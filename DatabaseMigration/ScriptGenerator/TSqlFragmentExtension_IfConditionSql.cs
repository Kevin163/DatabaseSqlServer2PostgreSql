using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigration.ScriptGenerator;

/// <summary>
/// TSqlFragment的扩展方法，专门用于处理IF条件语句
/// 如：
/// if begin ... end
/// if ...
/// </summary>
public static class TSqlFragmentExtension_IfConditionSql
{
    /// <summary>
    /// TSqlFragment中获取从指定索引开始的第一个IF条件语句的所有Token
    /// </summary>
    /// <param name="fragment"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static List<TSqlParserToken> GetIfCompleteSql(this TSqlFragment fragment, ref int index)
    {
        //确保索引在合理范围内
        if (index < 0 || index >= fragment.ScriptTokenStream.Count)
        {
            return new List<TSqlParserToken>();
        }
        var sqlTokens = new List<TSqlParserToken>();
        var tokens = fragment.ScriptTokenStream;
        int count = tokens.Count;
        int i = index;
        //确保指定索引的Token是IF
        var curr = tokens[i];
        if (curr.TokenType != TSqlTokenType.If)
        {
            return new List<TSqlParserToken>();
        }
        sqlTokens.Add(curr);
        i++;
        //继续向后查找，直到找到第一个BEGIN或者分号
        for (; i < count; i++)
        {
            curr = tokens[i];
            sqlTokens.Add(curr);
            if (curr.TokenType == TSqlTokenType.Begin)
            {
                //如果是BEGIN，则继续向后查找，直到找到对应的END
                int beginCount = 1;
                for (i++; i < count; i++)
                {
                    curr = tokens[i];
                    sqlTokens.Add(curr);
                    if (curr.TokenType == TSqlTokenType.Begin)
                    {
                        beginCount++;
                    }
                    else if (curr.TokenType == TSqlTokenType.End)
                    {
                        beginCount--;
                        if (beginCount == 0)
                        {
                            //找到了对应的END，结束查找
                            i++;
                            break;
                        }
                    }
                }
                break;
            }
            else if (curr.TokenType == TSqlTokenType.Semicolon)
            {
                //如果是分号，则表示IF语句结束
                i++;
                break;
            }
        }
        index = i;
        return sqlTokens;
    }
    /// <summary>
    /// TSqlFragment中获取从指定索引开始的第一个IF条件语句的所有Token，只包含IF条件部分，不包含BEGIN...END块
    /// </summary>
    /// <param name="fragment"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static List<TSqlParserToken> GetIfConditionOnly(this TSqlFragment fragment, ref int index)
    {
        //确保索引在合理范围内
        if (index < 0 || index >= fragment.ScriptTokenStream.Count)
        {
            return new List<TSqlParserToken>();
        }
        var sqlTokens = new List<TSqlParserToken>();
        var tokens = fragment.ScriptTokenStream;
        int count = tokens.Count;
        int i = index;
        //确保指定索引的Token是IF
        var curr = tokens[i];
        if (curr.TokenType != TSqlTokenType.If)
        {
            return new List<TSqlParserToken>();
        }
        sqlTokens.Add(curr);
        i++;
        //继续向后查找，直到找到第一个BEGIN或者分号
        for (; i < count; i++)
        {
            curr = tokens[i];
            if (curr.TokenType == TSqlTokenType.Begin || curr.TokenType == TSqlTokenType.Semicolon)
            {
                //如果是BEGIN或者分号，则表示IF条件语句结束
                break;
            }
            sqlTokens.Add(curr);
        }
        index = i;
        return sqlTokens;
    }
}
