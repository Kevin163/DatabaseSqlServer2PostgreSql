using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigration.ScriptGenerator;

/// <summary>
/// 获取TSqlFragment内部SQL语句的扩展方法，比如获取begin...end中的SQL语句列表,(...)中的SQL语句列表等
/// </summary>
public static class TSqlFragmentExtension_GetInnerSqls
{
    /// <summary>
    /// 获取内部的SQL语句列表，比如begin...end中的SQL语句列表,(...)中的SQL语句列表等
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static IList<TSqlParserToken> GetInnerSqls(this IList<TSqlParserToken> tokens, ref int index)
    {
        //支持的开始和结束Token类型
        var pairTokenTypes = new Dictionary<TSqlTokenType, TSqlTokenType>
        {
            { TSqlTokenType.Begin, TSqlTokenType.End },
            { TSqlTokenType.LeftParenthesis, TSqlTokenType.RightParenthesis },
        };
        //确定第一个开始Token的索引和对应的结束Token的索引
        if (index >= 0 && index < tokens.Count)
        {
            var startToken = tokens[index];
            if (pairTokenTypes.ContainsKey(startToken.TokenType))
            {
                var endTokenType = pairTokenTypes[startToken.TokenType];
                int count = tokens.Count;
                //反向查找对应的结束Token
                int endIndex = -1;
                for (var i = count - 1; i > index; i--)
                {
                    var item = tokens[i];
                    if (item.TokenType == endTokenType)
                    {
                        endIndex = i;
                        break;
                    }
                }
                //取出两个索引之间的所有Token作为结果返回
                if (endIndex > index)
                {
                    var innerTokens = tokens.Skip(index + 1).Take(endIndex - index - 1).ToList();
                    index = endIndex + 1; //更新index指向结束Token的下一个位置
                    return innerTokens;
                }
            }
        }
        //如果没有找到对应的开始和结束Token，则返回传入的所有token，并将index指向末尾
        index = tokens.Count;
        return tokens;
    }
}
