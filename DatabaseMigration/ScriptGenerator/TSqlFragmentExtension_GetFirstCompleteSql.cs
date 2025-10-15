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
        //确保索引在合理范围内
        if (index < 0 || index >= fragment.ScriptTokenStream.Count)
        {
            return new List<TSqlParserToken>();
        }
        var sqlTokens = new List<TSqlParserToken>();
        for (; index < fragment.ScriptTokenStream.Count; index++)
        {
            var item = fragment.ScriptTokenStream[index];
            #region 处理注释
            //如果当前语句是块注释，并且前面没有任何Token，则将整个块注释作为一个完整的SQL语句返回
            if ((item.TokenType == TSqlTokenType.MultilineComment || item.TokenType == TSqlTokenType.SingleLineComment)
                && sqlTokens.Count(w => w.TokenType != TSqlTokenType.WhiteSpace) == 0)
            {
                sqlTokens.Add(item);
                index++;
                break;
            } 
            #endregion
            #region 处理if语句及语句块
            //如果当前语句是if，并且前面没有任何Token，则将整个if块作为一个完整的SQL语句返回
            if (item.TokenType == TSqlTokenType.If
                && sqlTokens.Count(w => w.TokenType != TSqlTokenType.WhiteSpace) == 0)
            {
                var ifTokens = fragment.GetIfCompleteSql(ref index);
                sqlTokens.AddRange(ifTokens);
                break;
            } 
            #endregion
            #region 处理create 语句
            //如果当前语句是create，并且前面没有任何Token，则将整个create块作为一个完整的SQL语句返回
            if (item.TokenType == TSqlTokenType.Create
                && sqlTokens.Count(w => w.TokenType != TSqlTokenType.WhiteSpace) == 0)
            {
                var createTokens = fragment.GetCompleteCreateSql(ref index);
                sqlTokens.AddRange(createTokens);
                break;
            } 
            #endregion
            #region 处理union /union all
            //如果当前语句是union，并且前面已经有Token，则认为前面的Token已经构成一个完整的SQL语句，返回前面的语句，否则返回union自身
            if (item.TokenType == TSqlTokenType.Union)
            {
                if (sqlTokens.Count(w => w.TokenType != TSqlTokenType.WhiteSpace) > 0)
                {
                    //前面已经有Token，认为前面的Token已经构成一个完整的SQL语句，返回前面的语句
                    break;
                }
                else
                {
                    //前面没有任何Token，认为union自身作为一个完整的SQL语句返回
                    sqlTokens.Add(item);
                    index++;
                    //需要判断union all这种情况，如果是的话，则返回union all
                    var nextTokens = fragment.ScriptTokenStream.Skip(index).Take(5).ToList();
                    var nextTokenType = nextTokens.GetFirstNotWhiteSpaceTokenType();
                    if (nextTokenType == TSqlTokenType.All)
                    {
                        for (var j = 0; j < nextTokens.Count; j++)
                        {
                            sqlTokens.Add(nextTokens[j]);
                            if (nextTokens[j].TokenType == TSqlTokenType.All)
                            {
                                index += j + 1;
                                break;
                            }
                        }
                    }
                    break;
                }
            }
            #endregion
            #region 处理select语句
            //如果当前语句是select，并且之前的语句并非某些特殊类型的语句，则认为之前的语句已经完整，直接返回
            if (item.TokenType == TSqlTokenType.Select
                && sqlTokens.Count(w => w.TokenType != TSqlTokenType.WhiteSpace) > 0)
            {
                break;
            }
            #endregion
            #region 处理行尾的换行符，根据换行符后的内容来决定当前语句是否完整
            //如果当前是\r\n这种换行符，则检查下一个token，并且跳过注释，如果下一个token是union,则认为当前语句已经完整，直接返回
            if (item.TokenType == TSqlTokenType.WhiteSpace && item.Text.Equals("\r\n"))
            {
                var nextTokens = fragment.ScriptTokenStream.Skip(index + 1).Take(10).ToList();
                var nextTokenType = nextTokens.GetFirstNotWhiteSpaceTokenType(skipComment: true);
                if (nextTokenType == TSqlTokenType.Union)
                {
                    sqlTokens.Add(item);
                    index++;
                    break;
                }
            } 
            #endregion
            //其他情况下，则直接认为当前项是语句的一部分
            sqlTokens.Add(item);
            //如果是分号，则表示一个完整的SQL语句结束
            if (item.TokenType == TSqlTokenType.Semicolon)
            {
                index++;
                break;
            }
        }

        return sqlTokens;
    }
}
