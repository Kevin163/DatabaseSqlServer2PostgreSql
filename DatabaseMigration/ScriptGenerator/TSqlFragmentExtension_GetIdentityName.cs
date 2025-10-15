using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigration.ScriptGenerator;

/// <summary>
/// TSqlFragment的扩展方法，专门用于处理获取标识列名
/// </summary>
public static class TSqlFragmentExtension_GetIdentityName
{
    /// <summary>
    /// 获取标识列的列名
    /// </summary>
    /// <param name="fragment"></param>
    /// <param name="i"></param>
    /// <returns></returns>
    public static string GetIdentityName(this TSqlFragment fragment, ref int i)
    {
        return fragment.ScriptTokenStream.GetIdentityName(ref i);
    }
}
