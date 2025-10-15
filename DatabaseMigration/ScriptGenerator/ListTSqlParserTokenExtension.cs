using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Text;

namespace DatabaseMigration.ScriptGenerator;

/// <summary>
/// List&lt;TSqlParserToken&gt;的扩展方法
/// </summary>
public static class ListTSqlParserTokenExtension
{
    /// <summary>
    /// 获取第一个非空白的Token类型，如果全部是空白，则返回空白类型
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    public static TSqlTokenType GetFirstNotWhiteSpaceTokenType(this IList<TSqlParserToken> tokens,bool skipComment = false)
    {
        foreach (var tk in tokens)
        {
            if (tk.TokenType != TSqlTokenType.WhiteSpace)
            {
                if (skipComment && (tk.TokenType == TSqlTokenType.MultilineComment || tk.TokenType == TSqlTokenType.SingleLineComment))
                    continue;
                return tk.TokenType;
            }
        }
        return TSqlTokenType.WhiteSpace;
    }
    /// <summary>
    /// 获取标识列的列名
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="i"></param>
    /// <returns></returns>
    public static string GetIdentityName(this IList<TSqlParserToken> tokens, ref int i)
    {
        if (tokens == null || i < 0 || i >= tokens.Count) return string.Empty;
        int count = tokens.Count;
        int k = i;
        //确保指定索引的下一个是Dot类型的
        var curr = tokens[++k];
        if (curr.TokenType != TSqlTokenType.Dot)
        {
            return string.Empty;
        }
        //如果是的话，则继续向后查，直到找到第一个Identifier
        for (; k < tokens.Count; k++)
        {
            curr = tokens[k];
            if (curr.TokenType == TSqlTokenType.Identifier || curr.TokenType == TSqlTokenType.QuotedIdentifier)
            {
                break;
            }
        }
        i = k;
        return curr.Text.Trim('[', ']');
    }

    /// <summary>
    /// 处理类似convert(varchar(30) , 'gs') 的语句，转换为 CAST('gs' AS varchar(30))
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="i"></param>
    /// <returns></returns>
    public static string GetConvertSql(this IList<TSqlParserToken> tokens, ref int i)
    {
        if (tokens == null || i < 0 || i >= tokens.Count) return string.Empty;

        // 确保指定索引的token是'convert
        var curr = tokens[i];
        if (curr.TokenType != TSqlTokenType.Convert)
            return string.Empty;

        // 查找最近一个左括号的索引
        int openIdx = -1;
        for (int k = i + 1; k < tokens.Count; k++)
        {
            if (tokens[k].Text == "(") { openIdx = k; break; }
            // skip possible whitespace/comments tokens
        }
        if (openIdx < 0) { i = i + 1; return "convert"; }

        // 解析括号内的内容，分离出type, expr, style三个部分
        var typeSb = new StringBuilder();
        var exprSb = new StringBuilder();

        int depth = 0; // depth inside nested parentheses AFTER the convert( ...
        int phase = 0; // 0: parsing type, 1: parsing expr, 2: skipping style
        int endIdx = openIdx; // will point to closing ')'

        for (int k = openIdx + 1; k < tokens.Count; k++)
        {
            var t = tokens[k].Text;

            // Handle parentheses to track nesting within arguments
            if (t == "(")
            {
                depth++;
                // include the parenthesis in current buffer
                if (phase == 0) typeSb.Append(t);
                else if (phase == 1) exprSb.Append(t);
                continue;
            }
            if (t == ")")
            {
                if (depth == 0)
                {
                    // This is the closing parenthesis of CONVERT(...)
                    endIdx = k;
                    break;
                }
                depth--;
                if (phase == 0) typeSb.Append(t);
                else if (phase == 1) exprSb.Append(t);
                continue;
            }

            // Comma separating arguments at top level (depth == 0)
            if (t == "," && depth == 0)
            {
                if (phase == 0)
                {
                    phase = 1; // switch to parsing expression
                    continue;
                }
                else if (phase == 1)
                {
                    // third parameter (style) starts; we'll skip or ignore it
                    phase = 2;
                    continue;
                }
            }

            // Append token text to appropriate buffer depending on phase
            if (phase == 0)
            {
                typeSb.Append(t);
            }
            else if (phase == 1)
            {
                exprSb.Append(t);
            }
            else
            {
                // phase == 2 -> skipping style; do nothing other than continue until closing )
            }
        }

        // Move i to the closing parenthesis index so caller will continue after it
        i = endIdx;

        var typeStr = typeSb.ToString().Trim();
        var exprStr = exprSb.ToString().Trim();

        // Post-process some common patterns: remove outer parentheses from expression
        if (exprStr.StartsWith("(") && exprStr.EndsWith(")"))
        {
            // only trim if parentheses match
            int cnt = 0; bool ok = true;
            for (int p = 0; p < exprStr.Length; p++)
            {
                if (exprStr[p] == '(') cnt++; else if (exprStr[p] == ')') cnt--; if (cnt == 0 && p < exprStr.Length - 1) { ok = false; break; }
            }
            if (ok) exprStr = exprStr.Substring(1, exprStr.Length - 2).Trim();
        }

        // Construct CAST(expression AS type)
        var result = new StringBuilder();
        result.Append("CAST(");
        result.Append(exprStr.Length > 0 ? exprStr : string.Empty);
        result.Append(" AS ");
        result.Append(typeStr.Length > 0 ? typeStr : string.Empty);
        result.Append(")");

        return result.ToString();
    }
}
