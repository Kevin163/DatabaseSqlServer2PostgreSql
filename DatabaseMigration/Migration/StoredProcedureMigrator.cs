using Microsoft.Data.SqlClient;
using Npgsql;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace DatabaseMigration.Migration
{
    /// <summary>
    /// 存储过程迁移器：将 SQL Server 的存储过程列表迁移为 PostgreSQL 的存储过程
    /// 当前实现：
    /// - 列举 SQL Server 中的存储过程
    /// - 抓取原始定义（保留原始换行）并进行基础清理（移除 dbo. 前缀、去掉 []）
    /// - 然后逐条语句进行转换
    /// - 如果遇到不能转换的语句，则将原始 T-SQL 作为注释放入存储过程体中，并且同时在日志中记录
    /// </summary>
    public class StoredProcedureMigrator
    {
        private readonly FileLoggerService _logger;

        public StoredProcedureMigrator(FileLoggerService logger)
        {
            _logger = logger;
        }
        #region 迁移存储过程入口，负责获取所有存储过程名称并逐个迁移
        /// <summary>
        /// 迁移存储过程入口，负责获取所有存储过程名称并逐个迁移
        /// </summary>
        /// <param name="sourceConnection"></param>
        /// <param name="targetConnection"></param>
        public void Migrate(SqlConnection sourceConnection, NpgsqlConnection targetConnection)
        {
            _logger.Log("开始迁移存储过程...");
            try
            {
                if (sourceConnection.State == ConnectionState.Closed) sourceConnection.Open();
                if (targetConnection.State == ConnectionState.Closed) targetConnection.Open();

                // 列举存储过程
                var items = GetProcedureNames(sourceConnection);

                _logger.Log($"发现 {items.Count} 个存储过程需要迁移。");

                foreach (var (schema, name) in items)
                {
                    string procName = name.ToLowerInvariant();
                    string? converted = null; // 捕获转换后的 SQL，用于失败时输出

                    try
                    {
                        string tsql = MigrationUtils.GetObjectDefinition(sourceConnection, procName);
                        if (string.IsNullOrWhiteSpace(tsql))
                        {
                            _logger.LogError($"存储过程 {procName} 无法获取定义，跳过。");
                            continue;
                        }

                        converted = ConvertProcedureToPostgres(sourceConnection, tsql, procName);
                        if (!string.IsNullOrWhiteSpace(converted))
                        {
                            using var npgCmd = new NpgsqlCommand(converted, targetConnection);
                            npgCmd.ExecuteNonQuery();
                            _logger.Log($"存储过程 {procName} -> \"{procName}\" 迁移成功");
                        }
                    }
                    catch (PostgresException pex)
                    {
                        _logger.LogError($"存储过程 {procName} 迁移失败（目标库错误）: {pex.SqlState} {pex.MessageText}");
                        if (!string.IsNullOrWhiteSpace(converted))
                        {
                            _logger.LogError($"转换后的存储过程定义：\n{converted}");
                        }
                        //出错后，先退出，先解决这一个出错原因后再继续
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"存储过程 {procName} 迁移失败: {ex.Message}");
                        if (!string.IsNullOrWhiteSpace(converted))
                        {
                            _logger.LogError($"转换后的存储过程定义：\n{converted}");
                        }
                        //出错后，先退出，先解决这一个出错原因后再继续
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"迁移存储过程时发生错误: {ex}");
            }

            _logger.Log("存储过程迁移完成。");
        }
        #endregion
        #region 获取源数据库中的存储过程名称列表（schema, name）
        /// <summary>
        /// 获取源数据库中的存储过程名称列表（schema, name）
        /// </summary>
        /// <param name="sourceConnection"></param>
        /// <returns></returns>
        private static List<(string schema, string name)> GetProcedureNames(SqlConnection sourceConnection)
        {
            const string listSql = @"
SELECT s.name AS schema_name, p.name AS proc_name
FROM sys.procedures p
JOIN sys.schemas s ON s.schema_id = p.schema_id
ORDER BY s.name, p.name;";
            using var cmd = new SqlCommand(listSql, sourceConnection);
            using var reader = cmd.ExecuteReader();
            var items = new List<(string schema, string name)>();
            while (reader.Read())
            {
                items.Add((reader.GetString(0), reader.GetString(1)));
            }
            reader.Close();
            return items;
        }
        #endregion
        #region 迁移单个存储过程
        /// <summary>
        /// 迁移单个存储过程
        /// </summary>
        /// <param name="sourceConn"></param>
        /// <param name="tsqlDefinition"></param>
        /// <param name="procName"></param>
        /// <returns></returns>
        private string ConvertProcedureToPostgres(SqlConnection sourceConn, string tsqlDefinition, string procName)
        {
            if (string.IsNullOrWhiteSpace(tsqlDefinition)) return string.Empty;

            // 基础清理：移除 dbo. 前缀（不在字符串/注释中）和去掉方括号
            string cleaned = MigrationUtils.RemoveSchemaPrefix(tsqlDefinition);
            cleaned = MigrationUtils.ReplaceBrackets(cleaned);

            var converted = new StringBuilder();
            var needConvert = new StringBuilder();

            while (!string.IsNullOrWhiteSpace(cleaned))
            {
                (var firstSql, var otherSql) = MigrationUtils.GetFirstCompleteSqlSentence(cleaned);
                (var convertedSql, var needConvertSql) = ConvertSingleSqlRoute(sourceConn, firstSql, procName);
                converted.AppendIfNotNullOrWhiteSpace(convertedSql);
                needConvert.AppendIfNotNullOrWhiteSpace(needConvertSql);
                cleaned = otherSql;
            }

            // 如果仍然有还需要转换的语句，生成可编译的占位存储过程，并把原始 T-SQL 作为注释输出
            if (needConvert.Length > 0)
            {
                _logger.LogError($"存储过程{procName}已转换语句：{converted.ToString()};--存在未转换的语句：{needConvert.ToString()}");
                throw new Exception($"存储过程{procName}存在未转换的语句，请查看日志。");
            }

            var sb = new StringBuilder();
            sb.AppendLine(converted.ToString());
            sb.AppendLine(needConvert.ToString());
            // 结束存储过程定义
            sb.AppendLine("END;");
            sb.AppendLine("$$;");
            return sb.ToString();
        }
        #endregion
        #region 转换单条 SQL 语句的路由，根据语句类型进行不同的转换处理
        /// <summary>
        /// 转换单条 SQL 语句的路由，根据语句类型进行不同的转换处理
        /// </summary>
        /// <param name="sourceConn"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        private static (string convertedSql, string needConvertSql) ConvertSingleSqlRoute(SqlConnection sourceConn, string sql, string procName)
        {
            if (MigrationUtils.IsStartWithCreateProcedure(sql))
            {
                // CREATE PROCEDURE 语句，重新生成
                return ConvertCreateProcedureSql(sourceConn, sql, procName);
            }
            if (MigrationUtils.IsStartWithBlockComment(sql))
            {
                // 块注释，直接保留
                return (sql, string.Empty);
            }
            if (MigrationUtils.IsStartWithIf(sql))
            {
                // IF 语句, 需要转换if语句本身的语法，并且转换if语句中的sql语句
                return ConvertIfSql(sourceConn, sql);
            }
            // 其它语句，默认为不需要额外处理，直接返回原始语句，由数据库执行时进行判断
            return (sql, "");
        }


        #endregion
        #region 转换create procedure语句及参数
        /// <summary>
        /// 转换create procedure语句及参数
        /// 不直接根据语句来进行转换，而是通过存储过程的名称和参数来重新生成删除和创建语句
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="procName"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static (string convertedSql, string needConvertSql) ConvertCreateProcedureSql(SqlConnection sourceConn, string sql, string procName)
        {

            var parameters = GetProcedureParameters(sourceConn, procName);

            // 生成参数定义与签名
            var paramDefs = new List<string>(parameters.Count);
            var dropTypes = new List<string>(parameters.Count);
            foreach (var p in parameters)
            {
                string quotedName = p.Name;
                string mode = p.IsOutput ? "OUT " : string.Empty; // 默认 IN 省略
                paramDefs.Add($"{mode}{quotedName} {p.PgType}");
                dropTypes.Add(p.PgType);
            }
            string defList = string.Join(", ", paramDefs);

            string dropSig = string.Join(", ", dropTypes);
            var sb = new StringBuilder();
            // 生成删除与创建语句
            sb.AppendLine(parameters.Count > 0
             ? $"DROP PROCEDURE IF EXISTS \"{procName}\"({dropSig});"
             : $"DROP PROCEDURE IF EXISTS \"{procName}\"();");

            sb.AppendLine(parameters.Count > 0
                ? $"CREATE OR REPLACE PROCEDURE \"{procName}\"({defList}) "
                : $"CREATE OR REPLACE PROCEDURE \"{procName}\"() ");
            sb.AppendLine("LANGUAGE plpgsql");
            sb.AppendLine("AS $$");
            sb.AppendLine("BEGIN");
            return (sb.ToString(), string.Empty);
        }
        /// <summary>
        /// 获取存储过程参数列表
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="procName"></param>
        /// <returns></returns>
        private static List<ProcParam> GetProcedureParameters(SqlConnection conn, string procName)
        {
            const string sql = @"
SELECT
    PARAMETER_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    NUMERIC_PRECISION,
    NUMERIC_SCALE,
    PARAMETER_MODE
FROM INFORMATION_SCHEMA.PARAMETERS
WHERE SPECIFIC_NAME = @proc
ORDER BY ORDINAL_POSITION;";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@proc", procName);
            using var reader = cmd.ExecuteReader();

            var metaTable = new DataTable();
            metaTable.Columns.Add("CHARACTER_MAXIMUM_LENGTH", typeof(object));
            metaTable.Columns.Add("NUMERIC_PRECISION", typeof(object));
            metaTable.Columns.Add("NUMERIC_SCALE", typeof(object));

            var list = new List<ProcParam>();
            while (reader.Read())
            {
                string rawName = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                string dataType = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                object charLen = reader.IsDBNull(2) ? DBNull.Value : reader.GetValue(2);
                object precision = reader.IsDBNull(3) ? DBNull.Value : reader.GetValue(3);
                object scale = reader.IsDBNull(4) ? DBNull.Value : reader.GetValue(4);
                string mode = reader.IsDBNull(5) ? "IN" : reader.GetString(5);

                // 填入元数据行
                var row = metaTable.NewRow();
                row["CHARACTER_MAXIMUM_LENGTH"] = charLen ?? DBNull.Value;
                row["NUMERIC_PRECISION"] = precision ?? DBNull.Value;
                row["NUMERIC_SCALE"] = scale ?? DBNull.Value;
                metaTable.Rows.Add(row);

                string paramName = (rawName ?? string.Empty).TrimStart('@');
                string pgType = MigrationUtils.ConvertToPostgresType(dataType, row);
                bool isOutput = mode.IndexOf("OUT", StringComparison.OrdinalIgnoreCase) >= 0 && !mode.Equals("IN", StringComparison.OrdinalIgnoreCase);

                list.Add(new ProcParam
                {
                    Name = paramName.ToLowerInvariant(),
                    PgType = pgType,
                    IsOutput = isOutput
                });
            }
            return list;
        }
        #endregion
        #region 转换if语句及内部sql
        /// <summary>
        /// 转换if语句及内部sql
        /// </summary>
        /// <param name="sourceConn"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static (string convertedSql, string needConvertSql) ConvertIfSql(SqlConnection sourceConn, string sql)
        {
            var converted = new StringBuilder();
            var needConvert = new StringBuilder();
            // 解析出 if 条件和内部语句
            (var ifConditionSql, var otherSql) = MigrationUtils.GetIfConditionSql(sql);
            if (string.IsNullOrWhiteSpace(ifConditionSql))
            {
                return ("", sql);
            }
            //转换 if 条件
            (var convertedIfConditionSql, var needConvertIfConditionSql,var endIfConditionSql) = ConvertIfConditionSql(ifConditionSql);
            converted.AppendIfNotNullOrWhiteSpace(convertedIfConditionSql);
            needConvert.AppendIfNotNullOrWhiteSpace(needConvertIfConditionSql);
            //转换 if 内部语句
            var innerSql = IfConditionUtils.GetSqlsInBeginAndEnd(otherSql);
            while (!string.IsNullOrWhiteSpace(innerSql))
            {
                (var firstInnerSql,var otherInnerSql) = MigrationUtils.GetFirstCompleteSqlSentence(innerSql);
                (var convertedInnerSql, var needConvertInnerSql) = ConvertSingleSqlRoute(sourceConn, firstInnerSql, "");
                converted.AppendIfNotNullOrWhiteSpace(convertedInnerSql);
                needConvert.AppendIfNotNullOrWhiteSpace(needConvertInnerSql);
                innerSql = otherInnerSql;
            }
            //添加 if 条件语句的结束部分
            converted.AppendIfNotNullOrWhiteSpace(endIfConditionSql);

            return (converted.ToString(), needConvert.ToString());
        }
        /// <summary>
        /// 转换if 条件
        /// </summary>
        /// <param name="ifConditionSql"></param>
        /// <returns></returns>
        private static (string convertedSql, string needConvertSql,string endSql) ConvertIfConditionSql(string ifConditionSql)
        {
            string tableName, columnName, indexName;
            // 处理 IF NOT EXISTS(SELECT * FROM syscolumns WHERE ID = OBJECT_ID('HotelPos') AND name = 'Id') 类型的语句
            if (IfConditionUtils.TryParseNotExistsSysColumnsCondition(ifConditionSql, out tableName, out columnName))
            {
                return ($"IF NOT EXISTS ( SELECT 1 FROM information_schema.columns WHERE table_name = '{tableName}' AND column_name = '{columnName}') THEN \n",
                    "",
                    "END IF;\n");
            }
            // 处理 if not exists(select * from INFORMATION_SCHEMA.columns where table_name='posSmMappingHid' and column_name = 'memberVersion') 
            if(IfConditionUtils.TryParseNotExistsInformationSchemaColumnsCondition(ifConditionSql,out tableName,out columnName))
            {
                return ($"IF NOT EXISTS ( SELECT 1 FROM information_schema.columns WHERE table_name = '{tableName}' AND column_name = '{columnName}') THEN \n",
                    "",
                    "END IF;\n");
            }
            //处理 IF OBJECT_ID('HuiYiMapping') IS NULL 这类语句
            if (IfConditionUtils.TryParseIsObjectIdNullCondition(ifConditionSql,out tableName))
            {
                return ($"IF to_regclass('{tableName}') IS NULL THEN \n",
                    "",
                    "END IF;\n");
            }
            // 处理 IF NOT EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'dbo.commonInvoiceInfo') AND type IN ('U'))
            if(IfConditionUtils.TryParseNotExistsSelectFromAllObjectsCondition(ifConditionSql,out tableName))
            {
                return ($"IF to_regclass('{tableName}') IS NULL THEN \n",
                   "",
                   "END IF;\n");
            }
            //处理IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'TryHotelIdForGroup')  这类语句
            if (IfConditionUtils.TryParseSelectFromTableWhenWhereOneEqualCondition(ifConditionSql,out tableName, out WhereConditionItem whereItem))
            {
                return ($"IF NOT EXISTS ( SELECT 1 FROM {tableName} WHERE {whereItem.ColumnName} = '{whereItem.Value}') THEN \n",
                    "",
                    "END IF;\n");
            }
            //处理 if not exists(select id from sysobjects where name = 'ImeiMappingHid') 这类语句
            if(IfConditionUtils.TryParseNotExistsSelectFromSysObjectsCondition(ifConditionSql,out string objectName))
            {
                return ($"IF to_regclass('{objectName}') IS NULL THEN \n",
                   "",
                   "END IF;\n");
            }
            // 处理 IF NOT EXISTS( SELECT * from sysobjects where name =( SELECT TOP 1 name FROM sys.indexes  WHERE is_primary_key = 1   AND object_id  = Object_Id('posSmMappingHid') AND name='PK_posSm_20190808912' ) )  这类语句
            if(IfConditionUtils.TryParseNotExistsSelectFromSysIndexCondition(ifConditionSql,out tableName,out indexName))
            {
                return ($"IF NOT EXISTS ( select * from pg_class where relname = '{indexName}' and relkind = 'i') THEN \n",
                   "",
                   "END IF;\n");
            }
            // 处理 IF EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('HotelUserWxInfo') AND name = 'NickName' AND length = 28)
            if(IfConditionUtils.TryParseExistsSysColumnsCondition(ifConditionSql,out tableName,out columnName,out int? length))
            {
                return ($"IF EXISTS ( SELECT 1 FROM information_schema.columns WHERE table_name = '{tableName}' AND column_name = '{columnName}' AND character_maximum_length = {length}) THEN \n",
                   "",
                   "END IF;\n");
            }
            // 处理 IF NOT EXISTS (SELECT * FROM AuthButtons WHERE AuthButtonId='SetHotelLevel' AND AuthButtonValue='524288' AND Seqid='101') 
            if(IfConditionUtils.TryParseNotExistsSelectFromAuthButtonsCondition(ifConditionSql,out var buttonId,out var buttonValue,out var seqid))
            {
                return ($"IF NOT EXISTS ( SELECT 1 FROM AuthButtons WHERE AuthButtonId = '{buttonId}' AND AuthButtonValue = '{buttonValue}' AND Seqid = '{seqid}') THEN \n",
                   "",
                   "END IF;\n");
            }
            /* 处理 if exists(select distinct * from (  
            select hotelCode as hid from dbo.posSmMappingHid
            union all
                                    select groupid from dbo.posSmMappingHid)a
                                    where ISNULL(a.hid, '') != '' and hid not in(select hid from dbo.hotelProducts where productCode = 'ipos'))  
            */
            if (IfConditionUtils.IsExistsPosSMMappingHidInHotelProductsWithSubqueryFormat(ifConditionSql))
            {
                return (@"IF EXISTS (
  SELECT 1 FROM (
    SELECT hotelCode AS hid FROM posSmMappingHid
    UNION ALL
    SELECT groupid AS hid FROM posSmMappingHid
  ) a
  WHERE COALESCE(a.hid, '') <> '' 
    AND a.hid NOT IN (
      SELECT hid FROM hotelProducts WHERE productCode = 'ipos'
    )
) THEN
",
                    "",
                    "END IF;\n");
            }

            return ("", $"{ifConditionSql} \n","");
        }
        #endregion

        /// <summary>
        /// 存储过程参数元数据
        /// </summary>
        private sealed class ProcParam
        {
            public string Name { get; set; } = string.Empty; // without '@'
            public string PgType { get; set; } = "text";
            public bool IsOutput { get; set; }
        }

    }
}
