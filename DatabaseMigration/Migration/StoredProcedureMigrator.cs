using DatabaseMigration.ScriptGenerator;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Npgsql;
using System.Data;
using System.IO;
using System.Text;

namespace DatabaseMigration.Migration;

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
        if (string.IsNullOrEmpty(tsqlDefinition))
            return string.Empty;

        var parser = new TSql170Parser(true);
        IList<ParseError> errors;
        using (var rdr = new StringReader(tsqlDefinition))
        {
            var fragment = parser.Parse(rdr, out errors);
            if (errors.Count > 0)
            {
                var errorMsgs = string.Join("; ", errors.Select(e => $"{Environment.NewLine}Line {e.Line}, Col {e.Column}: {e.Message}"));
                throw new Exception($"解析存储过程脚本错误：{errorMsgs}");
            }
            var convertedSql = new PostgreSqlProcedureScriptGenerator().GenerateSqlScript(fragment);
            var deleteAndCreateSql = ConvertCreateProcedureSql(sourceConn, procName.ToPostgreSqlIdentifier());

            return $"{deleteAndCreateSql}{Environment.NewLine}{convertedSql}{Environment.NewLine}End;{Environment.NewLine}$$;";
        }
    }
    #endregion
    #region 转换create procedure语句及参数
    /// <summary>
    /// 转换create procedure语句及参数
    /// 不直接根据语句来进行转换，而是通过存储过程的名称和参数来重新生成删除和创建语句
    /// </summary>
    /// <param name="procName"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private static string ConvertCreateProcedureSql(SqlConnection sourceConn, string procName)
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
        //// 生成删除与创建语句
        //sb.AppendLine(parameters.Count > 0
        // ? $"DROP PROCEDURE IF EXISTS \"{procName}\"({dropSig});"
        // : $"DROP PROCEDURE IF EXISTS \"{procName}\"();");

        sb.AppendLine(parameters.Count > 0
            ? $"CREATE OR REPLACE PROCEDURE \"{procName}\"({defList}) "
            : $"CREATE OR REPLACE PROCEDURE \"{procName}\"() ");
        sb.AppendLine("LANGUAGE plpgsql");
        sb.AppendLine("AS $$");
        sb.AppendLine("BEGIN");
        return sb.ToString();
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
