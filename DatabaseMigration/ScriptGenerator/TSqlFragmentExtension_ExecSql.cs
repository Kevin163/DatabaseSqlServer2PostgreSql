using DatabaseMigration.Migration;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigration.ScriptGenerator;

/// <summary>
/// TSqlFragment扩展方法，用于处理exec sql语句
/// </summary>
public static class TSqlFragmentExtension_ExecSql
{
    /// <summary>
    /// 是否是exec('sql string')这种形式的SQL语句，并且提取出sql string内容
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="sql"></param>
    /// <returns></returns>
    public static bool IsExecSqlStringInParenthesis(this IList<TSqlParserToken> tokens, out string sql)
    {
        sql = string.Empty;
        //需要检查的所有TokenType列表，其中的表示要取其中的值进行返回
        var tokenTypes = new List<TSqlTokenTypeItem>
        {
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Exec,TSqlTokenType.Execute }),
            new TSqlTokenTypeItem(TSqlTokenType.LeftParenthesis),
            new TSqlTokenTypeItem(TSqlTokenType.AsciiStringLiteral, action:TSqlTokenTypeAction.OutValue),
            new TSqlTokenTypeItem(TSqlTokenType.RightParenthesis),
        };
        var matchResult = tokens.IsMatchTokenTypesSequence(tokenTypes);
        if (matchResult.IsMatch && matchResult.OutValues.Count == 1)
        {
            sql = matchResult.OutValues[0].Replace("''", "'").TrimQuotes();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 是否是exec(@sql)这种形式的SQL语句，并且提取出@sql变量名称内容
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="sql"></param>
    /// <returns></returns>
    public static bool IsExecSqlVariableInParenthesis(this IList<TSqlParserToken> tokens, out string sql)
    {
        sql = string.Empty;
        //需要检查的所有TokenType列表，其中的表示要取其中的值进行返回
        var tokenTypes = new List<TSqlTokenTypeItem>
        {
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Exec,TSqlTokenType.Execute }),
            new TSqlTokenTypeItem(TSqlTokenType.LeftParenthesis),
            new TSqlTokenTypeItem(TSqlTokenType.Variable, action:TSqlTokenTypeAction.OutValue),
            new TSqlTokenTypeItem(TSqlTokenType.RightParenthesis),
        };
        var matchResult = tokens.IsMatchTokenTypesSequence(tokenTypes);
        if (matchResult.IsMatch && matchResult.OutValues.Count == 1)
        {
            sql = matchResult.OutValues[0];
            return true;
        }
        return false;
    }

}
