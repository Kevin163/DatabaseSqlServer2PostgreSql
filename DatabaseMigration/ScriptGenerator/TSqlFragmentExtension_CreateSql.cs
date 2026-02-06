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
    public static List<TSqlParserToken> GetCompleteCreateSql(this IList<TSqlParserToken> tokens, ref int index)
    {
        //确保索引在合理范围内
        if (tokens == null || index < 0 || index >= tokens.Count)
        {
            return new List<TSqlParserToken>();
        }
        //需要确保是从create开始的
        var first = tokens[index];
        if (first.TokenType != TSqlTokenType.Create)
        {
            return new List<TSqlParserToken>();
        }
        //向后扫描10个Token，以便根据要创建的对象类型，调用相应的方法来获取完整的Create语句
        for (int k = index + 1; k < tokens.Count && k < index + 10; k++)
        {
            var tk = tokens[k];
            if (tk.TokenType == TSqlTokenType.View)
            {
                return tokens.GetCompleteCreateViewSql(ref index);
            }
            if(tk.TokenType == TSqlTokenType.Procedure || tk.TokenType == TSqlTokenType.Proc)
            {
                return tokens.GetCompleteCreateProcedureSql(ref index);
            }
            if (tk.TokenType == TSqlTokenType.Table)
            {
                return tokens.GetCompleteCreateTableSql(ref index);
            }
            // stop scanning if we encounter another object type keyword that isn't view
            if (tk.TokenType == TSqlTokenType.Function || tk.TokenType == TSqlTokenType.Index)
            {
                break;
            }
        }
        //其他情况下，则认为整个语句就是一个完成的create语句
        index = tokens.Count;
        return tokens.ToList();
    }

    /// <summary>
    /// 获取从指定索引开始的完整的Create Table语句的所有Token
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static List<TSqlParserToken> GetCompleteCreateTableSql(this IList<TSqlParserToken> tokens, ref int index)
    {
        //确保索引在合理范围内
        if (tokens == null || index < 0 || index >= tokens.Count)
        {
            return new List<TSqlParserToken>();
        }
        //需要确保是从create开始的
        var first = tokens[index];
        if (first.TokenType != TSqlTokenType.Create)
        {
            return new List<TSqlParserToken>();
        }

        var result = new List<TSqlParserToken>();
        var parenthesisCount = 0;
        var foundTable = false;
        var foundLeftParenthesis = false;

        for (int i = index; i < tokens.Count; i++)
        {
            var token = tokens[i];
            result.Add(token);

            // 跳过注释和空白
            if (token.TokenType == TSqlTokenType.MultilineComment ||
                token.TokenType == TSqlTokenType.SingleLineComment ||
                token.TokenType == TSqlTokenType.WhiteSpace)
            {
                continue;
            }

            // 标记找到了 TABLE 关键字
            if (!foundTable && token.TokenType == TSqlTokenType.Table)
            {
                foundTable = true;
                continue;
            }

            // 如果已经找到了 TABLE，开始查找左括号
            if (foundTable && !foundLeftParenthesis)
            {
                if (token.TokenType == TSqlTokenType.LeftParenthesis)
                {
                    foundLeftParenthesis = true;
                    parenthesisCount = 1;
                }
                continue;
            }

            // 如果已经找到了左括号，开始追踪括号计数
            if (foundLeftParenthesis)
            {
                if (token.TokenType == TSqlTokenType.LeftParenthesis)
                {
                    parenthesisCount++;
                }
                else if (token.TokenType == TSqlTokenType.RightParenthesis)
                {
                    parenthesisCount--;
                    // 当括号计数回到0时，说明找到了表定义的结束
                    if (parenthesisCount == 0)
                    {
                        // 继续向后查找，可能还有 ON [PRIMARY], ON [PRIMARY] WITH (...) 等子句
                        var foundConstraint = false;
                        for (int j = i + 1; j < tokens.Count; j++)
                        {
                            var nextToken = tokens[j];
                            result.Add(nextToken);

                            // 如果遇到分号，语句结束
                            if (nextToken.TokenType == TSqlTokenType.Semicolon)
                            {
                                index = j + 1;
                                return result;
                            }

                            // 如果遇到 ON 关键字（用于 ON [PRIMARY] 子句）
                            if (nextToken.TokenType == TSqlTokenType.On)
                            {
                                foundConstraint = true;
                                continue;
                            }

                            // 如果找到了 ON，则继续添加直到遇到分号或下一个SQL语句
                            if (foundConstraint)
                            {
                                // 遇到左括号，可能进入了 WITH (...) 子句
                                if (nextToken.TokenType == TSqlTokenType.LeftParenthesis)
                                {
                                    var withParenthesisCount = 1;
                                    for (int k = j + 1; k < tokens.Count; k++)
                                    {
                                        var withToken = tokens[k];
                                        result.Add(withToken);

                                        if (withToken.TokenType == TSqlTokenType.LeftParenthesis)
                                        {
                                            withParenthesisCount++;
                                        }
                                        else if (withToken.TokenType == TSqlTokenType.RightParenthesis)
                                        {
                                            withParenthesisCount--;
                                            if (withParenthesisCount == 0)
                                            {
                                                j = k;
                                                break;
                                            }
                                        }
                                    }
                                    continue;
                                }

                                // 如果遇到分号，结束
                                if (nextToken.TokenType == TSqlTokenType.Semicolon)
                                {
                                    index = j + 1;
                                    return result;
                                }

                                // 如果遇到下一个 SQL 语句的关键字（非空白、非注释、非 ON 相关的标识符）
                                if (nextToken.TokenType != TSqlTokenType.WhiteSpace &&
                                    nextToken.TokenType != TSqlTokenType.MultilineComment &&
                                    nextToken.TokenType != TSqlTokenType.SingleLineComment &&
                                    nextToken.TokenType != TSqlTokenType.AsciiStringOrQuotedIdentifier &&
                                    nextToken.TokenType != TSqlTokenType.QuotedIdentifier &&
                                    nextToken.TokenType != TSqlTokenType.LeftParenthesis &&
                                    nextToken.TokenType != TSqlTokenType.RightParenthesis &&
                                    nextToken.TokenType != TSqlTokenType.Comma &&
                                    nextToken.TokenType != TSqlTokenType.With &&
                                    nextToken.TokenType != TSqlTokenType.EndOfFile)
                                {
                                    index = j;
                                    return result;
                                }
                            }
                            else
                            {
                                // 没有找到 ON 的情况下，如果遇到下一个语句的关键字，则停止
                                if (nextToken.TokenType != TSqlTokenType.WhiteSpace &&
                                    nextToken.TokenType != TSqlTokenType.MultilineComment &&
                                    nextToken.TokenType != TSqlTokenType.SingleLineComment &&
                                    nextToken.TokenType != TSqlTokenType.On &&
                                    nextToken.TokenType != TSqlTokenType.AsciiStringOrQuotedIdentifier &&
                                    nextToken.TokenType != TSqlTokenType.QuotedIdentifier &&
                                    nextToken.TokenType != TSqlTokenType.With &&
                                    nextToken.TokenType != TSqlTokenType.EndOfFile)
                                {
                                    index = j;
                                    return result;
                                }
                            }
                        }
                        // 到了末尾
                        index = tokens.Count;
                        return result;
                    }
                }
            }
        }

        // 如果没有正常结束，返回已收集的结果
        index = tokens.Count;
        return result;
    }
}
