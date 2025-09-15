using Microsoft.Data.SqlClient;
using Npgsql;
using System;
using System.Data;
using System.Text;

namespace DatabaseMigration.Migration
{
    /// <summary>
    /// 负责将 SQL Server 中的表结构迁移到 PostgreSQL 的工具类。
    /// </summary>
    /// <remarks>
    /// 该类使用 <see cref="FileLoggerService"/> 记录迁移过程的日志，并根据 <see cref="MigrationMode"/>
    /// 决定是否复制表数据（Development 模式下复制，Production 模式下跳过数据复制）。
    /// 表名与列名在迁移时会统一转换为小写，以避免在 PostgreSQL 中必须使用双引号访问的问题。
    /// </remarks>
    public class TableMigrator
    {
        private readonly FileLoggerService _logger;
        private readonly MigrationMode _migrationMode;

        /// <summary>
        /// 使用指定的日志服务和迁移模式创建 <see cref="TableMigrator"/> 实例。
        /// </summary>
        /// <param name="logger">用于记录迁移过程的 <see cref="FileLoggerService"/> 实例，不能为 <c>null</c>。</param>
        /// <param name="migrationMode">指定迁移的模式，可影响是否复制表数据。</param>
        /// <exception cref="ArgumentNullException">如果 <paramref name="logger"/> 为 <c>null</c> 将抛出此异常。</exception>
        public TableMigrator(FileLoggerService logger, MigrationMode migrationMode)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _migrationMode = migrationMode;
        }

        /// <summary>
        /// 执行表迁移：读取源 SQL Server 的表定义并在目标 PostgreSQL 中创建对应的表结构。
        /// </summary>
        /// <param name="sourceConnection">已配置的 SQL Server 连接，用于读取表和列的元数据。方法内部会在必要时打开该连接，但不会负责关闭调用者可能期望保持的连接。</param>
        /// <param name="targetConnection">已配置的 PostgreSQL 连接，用于在目标数据库中执行建表语句。调用方需保证该连接处于可用状态。</param>
        /// <remarks>
        /// - 该方法会调用 <see cref="MigrationUtils.ConvertToPostgresType"/> 将 SQL Server 数据类型映射为 PostgreSQL 类型。
        /// - 迁移过程中，表名和列名会统一转换为小写并用双引号包裹以确保兼容性。
        /// - 在 <see cref="MigrationMode.Development"/> 模式下可以执行数据复制（复制源表前 10 行到目标表，前提是目标表为空，以便快速验证结构）；在 <see cref="MigrationMode.Production"/> 模式下会跳过数据复制以避免在生产环境中复制大量数据。
        /// - 方法会记录成功与失败的信息；当发生异常时会将异常与可能的建表语句写入日志，但不会重新抛出异常（当前实现捕获异常并记录）。
        /// </remarks>
        public void Migrate(SqlConnection sourceConnection, NpgsqlConnection targetConnection)
        {
            _logger.Log("开始迁移表...");
            DataTable tables = sourceConnection.GetSchema("Tables");
            // 只迁移类型为 BASE TABLE 的对象
            var realTables = tables.Select("TABLE_TYPE = 'BASE TABLE'");
            _logger.Log($"发现 {realTables.Length} 张表需要迁移。");

            foreach (DataRow table in realTables)
            {
                string originalTableName = (string)table["TABLE_NAME"];
                //由于postgresql的表名区分大小写，所有表名统一转为小写后进行创建，否则的话，在所有使用表的时候，都必须使用双引号括起来，但好多存储过程和视图中并没有使用双引号括起来
                string tableName = originalTableName.ToLower();
                _logger.Log($"正在迁移表: {tableName}");

                string createTableSql = null;
                try
                {
                    if (sourceConnection.State == ConnectionState.Closed)
                        sourceConnection.Open();

                    createTableSql = GenerateCreateTableSql(sourceConnection, tableName);
                    if (targetConnection.State == ConnectionState.Closed)
                        targetConnection.Open();

                    using (NpgsqlCommand command = new NpgsqlCommand(createTableSql, targetConnection))
                        command.ExecuteNonQuery();

                    _logger.Log($"表 '{tableName}' 的结构创建成功 (如果不存在)。");

                    if (_migrationMode == MigrationMode.Development)
                    {
                        CopyTop10TableDataWhenNeed(sourceConnection, targetConnection, table, originalTableName, tableName);
                    }
                    else
                    {
                        _logger.Log($"生产环境下，跳过表 '{tableName}' 的数据复制。");
                    }

                    _logger.Log($"表 {tableName} 迁移成功.");
                }
                catch (Exception ex)
                {
                    _logger.Log($"迁移表 {tableName} 失败: {ex}");
                    if (!string.IsNullOrEmpty(createTableSql))
                        _logger.Log($"出错的建表语句: {createTableSql}");
                }
            }
            _logger.Log("表迁移完成.");
        }
        /// <summary>
        /// 当目标表为空时，从源表复制前 10 行数据到目标表以验证结构。
        /// </summary>
        /// <param name="sourceConnection">源数据库的 SQL Server 连接。</param>
        /// <param name="targetConnection">目标数据库的 PostgreSQL 连接。</param>
        /// <param name="table">源表的元数据。</param>
        /// <param name="originalTableName">源表的原始名称。</param>
        /// <param name="tableName">目标表的名称。</param>
        private void CopyTop10TableDataWhenNeed(SqlConnection sourceConnection, NpgsqlConnection targetConnection, DataRow table, string originalTableName, string tableName)
        {
            try
            {
                // 检查目标表是否为空：使用 EXISTS 查询
                string existsSql = $"SELECT EXISTS (SELECT 1 FROM \"{tableName}\" LIMIT 1)";
                using (var existsCmd = new NpgsqlCommand(existsSql, targetConnection))
                {
                    object existsObj = existsCmd.ExecuteScalar();
                    bool hasAny = false;
                    if (existsObj is bool b) hasAny = b;
                    else if (existsObj != null && bool.TryParse(existsObj.ToString(), out var pb)) hasAny = pb;

                    if (!hasAny)
                    {
                        _logger.Log($"目标表 '{tableName}' 为空，开始复制源表前 10 行以验证结构。");

                        // 构造源表的限定名（尝试使用 TABLE_SCHEMA，如果存在）
                        string schema = table.Table.Columns.Contains("TABLE_SCHEMA") && table["TABLE_SCHEMA"] != DBNull.Value
                            ? (string)table["TABLE_SCHEMA"]
                            : "dbo";
                        string sourceQualified = $"[{schema}].[{originalTableName}]";

                        string selectTopSql = $"SELECT TOP 10 * FROM {sourceQualified}";
                        using (var srcCmd = new SqlCommand(selectTopSql, sourceConnection))
                        using (var reader = srcCmd.ExecuteReader())
                        using (var tx = targetConnection.BeginTransaction())
                        {
                            try
                            {
                                if (!reader.HasRows)
                                {
                                    _logger.Log($"源表 {sourceQualified} 无数据，跳过复制。");
                                }
                                else
                                {
                                    while (reader.Read())
                                    {
                                        int fieldCount = reader.FieldCount;
                                        var colNamesSb = new StringBuilder();
                                        var valsSb = new StringBuilder();
                                        using (var insertCmd = new NpgsqlCommand() { Connection = targetConnection, Transaction = tx })
                                        {
                                            for (int i = 0; i < fieldCount; i++)
                                            {
                                                string col = reader.GetName(i).ToLower().Replace("\"", "\"\"");
                                                if (i > 0)
                                                {
                                                    colNamesSb.Append(", ");
                                                    valsSb.Append(", ");
                                                }
                                                colNamesSb.Append($"\"{col}\"");
                                                string paramName = $"@p{i}";
                                                valsSb.Append(paramName);

                                                object value = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                                                insertCmd.Parameters.AddWithValue(paramName, value ?? DBNull.Value);
                                            }

                                            insertCmd.CommandText = $"INSERT INTO \"{tableName}\" ({colNamesSb}) VALUES ({valsSb})";
                                            insertCmd.ExecuteNonQuery();
                                        }
                                    }
                                }

                                tx.Commit();
                                _logger.Log($"已将源表 {sourceQualified} 的前 10 行复制到目标表 '{tableName}'。");
                            }
                            catch (Exception exCopy)
                            {
                                try { tx.Rollback(); } catch { }
                                _logger.Log($"复制表数据到 '{tableName}' 失败: {exCopy}");
                            }
                        }
                    }
                    else
                    {
                        _logger.Log($"目标表 '{tableName}' 已包含数据，跳过测试行复制。");
                    }
                }
            }
            catch (Exception exDev)
            {
                _logger.Log($"在 Development 模式下复制测试数据时发生错误: {exDev}");
            }
        }

        /// <summary>
        /// 构建在 PostgreSQL 中创建指定表的 CREATE TABLE 语句。
        /// </summary>
        /// <param name="sourceConnection">用于从 SQL Server 查询列元数据的连接。</param>
        /// <param name="tableName">要生成建表语句的表名（此参数在方法内已假定为小写）。</param>
        /// <returns>返回可在 PostgreSQL 上执行的 CREATE TABLE SQL 字符串，包含列定义与 NOT NULL 约束。若表无列则生成空结构表语句。</returns>
        /// <remarks>
        /// - 方法会调用 <see cref="MigrationUtils.ConvertToPostgresType"/> 将 SQL Server 的数据类型与列元数据转换为 PostgreSQL 可识别的类型字符串。
        /// - 列名称在生成时会转换为小写并使用双引号包裹以避免大小写敏感问题。
        /// - 生成的 SQL 末尾会添加分号结尾。
        /// </remarks>
        /// <exception cref="InvalidOperationException">当查询列元数据失败或返回不一致信息时，可能抛出此类异常。</exception>
        private string GenerateCreateTableSql(SqlConnection sourceConnection, string tableName)
        {
            StringBuilder createTableSql = new StringBuilder($"CREATE TABLE IF NOT EXISTS \"{tableName}\" (\n");
            DataTable columns = sourceConnection.GetSchema("Columns", new string[] { null, null, tableName });

            foreach (DataRow column in columns.Rows)
            {
                string columnName = (string)column["COLUMN_NAME"];
                //列名也需要转为小写，否则在使用时也需要双引号括起来
                columnName = columnName.ToLower();
                string dataType = (string)column["DATA_TYPE"];
                bool isNullable = (string)column["IS_NULLABLE"] == "YES";
                string postgresType = MigrationUtils.ConvertToPostgresType(dataType, column);

                createTableSql.Append($"    \"{columnName}\" {postgresType}");
                if (!isNullable)
                    createTableSql.Append(" NOT NULL");
                createTableSql.Append(",\n");
            }

            if (columns.Rows.Count > 0)
                createTableSql.Length -= 2;
            createTableSql.Append("\n);");
            return createTableSql.ToString();
        }
    }
}