using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigration.ScriptGenerator;

/// <summary>
/// TSqlFragment的扩展方法，主要用于处理convert函数
/// </summary>
public static class TSqlFragmentExtension_ConvertFunction
{
    /// <summary>
    /// 处理类似convert(varchar(30) , 'gs') 的语句，转换为 CAST('gs' AS varchar(30))
    /// </summary>
    /// <param name="fragment"></param>
    /// <param name="i"></param>
    /// <returns></returns>
    public static string GetConvertSql(this TSqlFragment fragment, ref int i,PostgreSqlScriptGenerator postgreSqlScriptGenerator)
    {
        return fragment.ScriptTokenStream.GetConvertSql(ref i, postgreSqlScriptGenerator);
    }
}
