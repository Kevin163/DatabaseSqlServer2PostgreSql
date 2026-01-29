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
            //var deleteAndCreateSql = ConvertCreateProcedureSql(sourceConn, procName.ToPostgreSqlIdentifier());

            //return $"{deleteAndCreateSql}{Environment.NewLine}{convertedSql}{Environment.NewLine}End;{Environment.NewLine}$$;";
            return convertedSql;
        }
    }
    #endregion
}
