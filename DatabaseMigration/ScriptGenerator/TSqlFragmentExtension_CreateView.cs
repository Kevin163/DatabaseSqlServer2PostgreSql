using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigration.ScriptGenerator;
/// <summary>
/// TSqlFragment的扩展方法，专门用于处理Create View语句
/// </summary>
public static class TSqlFragmentExtension_CreateView
{
    /// <summary>
    /// 获取完整的Create View语句的所有Token，到as结束，如 create view xx as
    /// </summary>
    /// <param name="fragment"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static List<TSqlParserToken> GetCompleteCreateViewSql(this TSqlFragment fragment, ref int index)
    {
        var result = new List<TSqlParserToken>();
        if (fragment == null || fragment.ScriptTokenStream == null || index < 0 || index >= fragment.ScriptTokenStream.Count)
        {
            return result;
        }

        var tokens = fragment.ScriptTokenStream;
        int len = tokens.Count;
        int i = index;

        for (; i < len; i++)
        {
            var tk = tokens[i];
            result.Add(tk);

            // Stop once we see AS token (the AS that follows view name)
            if (tk.TokenType == TSqlTokenType.As)
            {
                i++; // move to next token after AS
                break;
            }
        }

        // update the caller's index to point to token after AS (or end)
        index = i;
        return result;
    }
}
