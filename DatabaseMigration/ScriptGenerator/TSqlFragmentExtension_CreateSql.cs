using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigration.ScriptGenerator;

/// <summary>
/// TSqlFragment的扩展方法，专门用于处理Create语句
/// </summary>
public static class TSqlFragmentExtension_CreateSql
{
    /// <summary>
    /// 获取从指定索引开始的第一个完整的Create语句的所有Token
    /// </summary>
    /// <param name="fragment"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static List<TSqlParserToken> GetCompleteCreateSql(this TSqlFragment fragment, ref int index)
    {
        //确保索引在合理范围内
        if (fragment == null || fragment.ScriptTokenStream == null || index < 0 || index >= fragment.ScriptTokenStream.Count)
        {
            return new List<TSqlParserToken>();
        }
        //需要确保是从create开始的
        var first = fragment.ScriptTokenStream[index];
        if (first.TokenType != TSqlTokenType.Create)
        {
            return new List<TSqlParserToken>();
        }
        //向后扫描10个Token，以便根据要创建的对象类型，调用相应的方法来获取完整的Create语句
        for (int k = index + 1; k < fragment.ScriptTokenStream.Count && k < index + 10; k++)
        {
            var tk = fragment.ScriptTokenStream[k];
            if (tk.TokenType == TSqlTokenType.View)
            {
                return fragment.GetCompleteCreateViewSql(ref index);
            }
            // stop scanning if we encounter another object type keyword that isn't view
            if (tk.TokenType == TSqlTokenType.Table || tk.TokenType == TSqlTokenType.Procedure || tk.TokenType == TSqlTokenType.Function || tk.TokenType == TSqlTokenType.Index)
            {
                break;
            }
        }
        //如果没有找到，则认为是异常情况，直接返回空列表
        return new List<TSqlParserToken>();
    }
}
