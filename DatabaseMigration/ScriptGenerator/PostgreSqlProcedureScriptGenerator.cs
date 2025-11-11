using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigration.ScriptGenerator;

/// <summary>
/// PostgreSQL存储过程脚本生成器
/// </summary>
public class PostgreSqlProcedureScriptGenerator : PostgreSqlScriptGenerator
{
    /// <summary>
    /// 重写create procedure语句的转换，返回空字符串，因为在外层StoredProcedureMigrator中已经生成好了删除和创建语句，根据参数进行不同处理
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    protected override string ConvertSingleCreateAs(IList<TSqlParserToken> tokens)
    {
        return "";
    }
}
