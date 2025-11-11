using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigration.ScriptGenerator;

/// <summary>
/// TSqlFragment扩展方法，获取从指定索引开始的第一个完整的SQL语句
/// </summary>
public static class TSqlFragmentExtension_GetFirstCompleteSql
{
    /// <summary>
    /// 从指定的TSqlFragment中获取从指定索引开始的第一个完整的SQL语句的所有Token
    /// </summary>
    /// <param name="fragment"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static List<TSqlParserToken> GetFirstCompleteSqlTokens(this TSqlFragment fragment, ref int index)
    {
        return fragment.ScriptTokenStream.GetFirstCompleteSqlTokens(ref index);
    }
    /// <summary>
    /// 从指定的Token列表中获取从指定索引开始的第一个完整的SQL语句的所有Token
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static List<TSqlParserToken> GetFirstCompleteSqlTokens(this IList<TSqlParserToken> tokens, ref int index)
    {
        //确保索引在合理范围内
        if (index < 0 || index >= tokens.Count)
        {
            return new List<TSqlParserToken>();
        }
        #region 判断是否是union,union all这样的特殊token，是则直接返回
        //判断是否是union,union all这样的特殊token，是则直接返回
        var firstToken = tokens[index];
        if (firstToken.TokenType == TSqlTokenType.Union)
        {
            //判断后面是否紧跟着all
            var nextTokens = tokens.Skip(index + 1).Take(10).ToList();
            var nextTokenType = nextTokens.GetFirstNotWhiteSpaceTokenType();
            if (nextTokenType == TSqlTokenType.All)
            {
                var tokenAllIndex = nextTokens.FindIndex(t => t.TokenType == TSqlTokenType.All);
                index += tokenAllIndex + 2;
                return new List<TSqlParserToken> { firstToken }.Concat(nextTokens.Take(tokenAllIndex + 1)).ToList();
            }
            //否则只返回union
            index++;
            return new List<TSqlParserToken> { firstToken };
        } 
        #endregion
        //优先取出指定index开始后的所有token，然后转换成SQL语句，并再次进行解析，以便直接获取第一个完整的SQL语句
        var tokensToEnd = tokens.Skip(index).ToList();
        var sqlToEnd = string.Concat(tokensToEnd.Select(w => w.Text));
        if (string.IsNullOrWhiteSpace(sqlToEnd))
        {
            index = tokens.Count;
            return new List<TSqlParserToken>();
        }
        var parser = new TSql170Parser(true);
        var newFragment = parser.Parse(new System.IO.StringReader(sqlToEnd), out var errors) as TSqlScript;
        #region 处理整个语句是create...as ...类型的语句，则只返回create ... as
        //如果只有一条批量语句，并且开始位置的第一个是create，则表示可能是整个create语句，需要先取出create ...as这样的做为第一个完整的语句进行返回
        if (newFragment.Batches.Count == 1 && newFragment.ScriptTokenStream.Count > 0 && newFragment.ScriptTokenStream[0].TokenType == TSqlTokenType.Create)
        {
            var createIndex = 0;
            var createTokens = newFragment.ScriptTokenStream.GetCompleteCreateSql(ref createIndex);
            index += createIndex;
            return createTokens;
        }
        #endregion
        #region 处理整个语句是select ... union select ...的语句，则只返回第一个select...
        //如果只有一个批量语句，并且开始位置的第一个是select语句，并且整个语句里面包含union，则说明是将select union...全部当成一个语句了，需要先只取出union前面的select语句
        if(newFragment.Batches.Count == 1 && newFragment.ScriptTokenStream.Count > 0 && newFragment.ScriptTokenStream[0].TokenType == TSqlTokenType.Select && sqlToEnd.Contains("union", StringComparison.OrdinalIgnoreCase))
        {
            var unionIndex = newFragment.ScriptTokenStream.FindFirstIndex(t => t.TokenType == TSqlTokenType.Union);
            var firstSelectTokens = newFragment.ScriptTokenStream.Take(unionIndex).ToList();
            index += firstSelectTokens.Count;
            return firstSelectTokens;
        }
        #endregion
        //取出第一个语句的起始和结束位置
        var firstStatement = newFragment.Batches[0].Statements[0];
        var startIndex = firstStatement.FirstTokenIndex;
        var endIndex = firstStatement.LastTokenIndex;
        //这里给出的语句都是有真实意义的语句，如果如果startIndex>0，则表示前面有内容被跳过了，需要先返回这些内容，一般都是空白或注释，直接做为完整的语句进行返回即可
        if (startIndex > 0)
        {
            index += startIndex;
            return tokensToEnd.Take(startIndex).ToList();
        }
        //取出第一个语句的所有Token
        var firstSqlTokens = tokensToEnd.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();
        //更新传入的index参数，指向下一个语句的起始位置
        index += firstSqlTokens.Count;
        return firstSqlTokens;
    }
}
