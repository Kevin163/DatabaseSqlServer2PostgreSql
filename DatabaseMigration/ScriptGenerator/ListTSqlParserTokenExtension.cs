using DatabaseMigration.Migration;
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
    /// 查找符合条件的第一个Token的索引，如果没有找到则返回-1
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public static int FindFirstIndex(this IList<TSqlParserToken> tokens,Func<TSqlParserToken,bool> predicate)
    {
        for(int i=0;i<tokens.Count;i++)
        {
            if (predicate(tokens[i]))
                return i;
        }
        return -1;
    }
    /// <summary>
    /// 查找符合条件的最后一个Token的索引，如果没有找到则返回-1
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public static int FindLastIndex(this IList<TSqlParserToken> tokens, Func<TSqlParserToken, bool> predicate)
    {
        for (int i = tokens.Count - 1; i >= 0; i--)
        {
            if (predicate(tokens[i]))
                return i;
        }
        return -1;
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
            return tokens[i].Text.ToPostgreSqlIdentifier();
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
        return curr.Text.ToPostgreSqlIdentifier();
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
    /// <summary>
    /// 处理类似DATEADD(DAY,-30,GETDATE())的语句，转换为 now() + interval '-30 day'
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="i"></param>
    /// <returns></returns>
    public static string GetDateAddSql(this IList<TSqlParserToken> tokens, ref int i)
    {
        // 确保指定索引的token是'DATEADD
        if (tokens == null || i < 0 || i >= tokens.Count) return string.Empty;
        var curr = tokens[i];

        if(!(curr.TokenType == TSqlTokenType.Identifier && curr.Text.Equals("DATEADD",StringComparison.OrdinalIgnoreCase)))
        {
            return string.Empty;
        }

        // 查找最近一个左括号的索引
        int openIdx = -1;
        for (int k = i + 1; k < tokens.Count; k++)
        {
            if (tokens[k].Text == "(") { openIdx = k; break; }
        }
        if (openIdx < 0) { i = i + 1; return "DATEADD"; }

        var partSb = new StringBuilder();
        var numberSb = new StringBuilder();
        var dateSb = new StringBuilder();

        int depth = 0; // depth inside nested parentheses AFTER the DATEADD(
        int phase = 0; // 0: parsing datepart, 1: parsing number, 2: parsing date expression
        int endIdx = openIdx;

        for (int k = openIdx + 1; k < tokens.Count; k++)
        {
            var t = tokens[k].Text;

            if (t == "(")
            {
                depth++;
                if (phase == 0) partSb.Append(t);
                else if (phase == 1) numberSb.Append(t);
                else dateSb.Append(t);
                continue;
            }
            if (t == ")")
            {
                if (depth == 0)
                {
                    endIdx = k;
                    break;
                }
                depth--;
                if (phase == 0) partSb.Append(t);
                else if (phase == 1) numberSb.Append(t);
                else dateSb.Append(t);
                continue;
            }

            if (t == "," && depth == 0)
            {
                if (phase == 0)
                {
                    phase = 1; // switch to parsing number
                    continue;
                }
                else if (phase == 1)
                {
                    phase = 2; // switch to parsing date expression
                    continue;
                }
            }

            if (phase == 0) partSb.Append(t);
            else if (phase == 1) numberSb.Append(t);
            else dateSb.Append(t);
        }

        // Move i to the closing parenthesis index so caller will continue after it
        i = endIdx;

        var partStr = partSb.ToString().Trim();
        var numberStr = numberSb.ToString().Trim();
        var dateStr = dateSb.ToString().Trim();

        // Normalize date expression
        string pgDateExpr = dateStr;
        if (string.IsNullOrEmpty(pgDateExpr)) pgDateExpr = "NOW()"; // fallback
        else
        {
            // map GETDATE() or GETDATE to NOW()
            if (pgDateExpr.IndexOf("GETDATE", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                pgDateExpr = "NOW()";
            }
            else if (pgDateExpr.IndexOf("GETUTCDATE", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                pgDateExpr = "TIMEZONE('UTC', NOW())"; // approximation
            }
        }

        // map datepart to postgres units
        string partKey = partStr;
        if ((partKey.StartsWith("'") && partKey.EndsWith("'")) || (partKey.StartsWith("\"") && partKey.EndsWith("\"")))
            partKey = partKey.Substring(1, partKey.Length - 2);
        partKey = partKey.Trim().ToLowerInvariant();

        string unit;
        bool multiplyQuarter = false;
        switch (partKey)
        {
            case "yy":
            case "yyyy":
            case "year":
                unit = "year"; break;
            case "qq":
            case "quarter":
                // quarter is 3 months
                unit = "month"; multiplyQuarter = true; break;
            case "mm":
            case "m":
            case "month":
                unit = "month"; break;
            case "day":
            case "dd":
            case "d":
                unit = "day"; break;
            case "hour":
            case "hh":
                unit = "hour"; break;
            case "minute":
            case "mi":
            case "n":
                unit = "minute"; break;
            case "second":
            case "ss":
            case "s":
                unit = "second"; break;
            case "ms":
            case "millisecond":
                unit = "millisecond"; break;
            default:
                // try to use the raw token as unit (safe fallback)
                unit = partKey; break;
        }

        // handle numeric sign
        var numTrim = numberStr;
        // remove surrounding parentheses
        if (numTrim.StartsWith("(") && numTrim.EndsWith(")")) numTrim = numTrim.Substring(1, numTrim.Length - 2).Trim();

        bool negative = false;
        string absNum = numTrim;
        if (absNum.StartsWith("-")) { negative = true; absNum = absNum.Substring(1).Trim(); }

        // if number is numeric, compute value; otherwise treat as expression
        bool isNumeric = double.TryParse(absNum, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double numericVal);

        string intervalExpr;
        if (isNumeric)
        {
            if (multiplyQuarter) numericVal = numericVal * 3.0;
            // format without scientific notation
            var valStr = numericVal.ToString(System.Globalization.CultureInfo.InvariantCulture);
            intervalExpr = $"INTERVAL '{valStr} {unit}'";
        }
        else
        {
            // expression interval: build ((expr)::text || ' unit')::interval
            // absNum may be an expression, preserve it
            var expr = absNum;
            if (multiplyQuarter)
            {
                // multiply expression by 3: ( (expr) * 3 )
                expr = $"(({expr}) * 3)";
            }
            intervalExpr = $"(({expr})::text || ' {unit}')::interval";
        }

        string result;
        if (negative)
        {
            result = $"{pgDateExpr} - {intervalExpr}";
        }
        else
        {
            result = $"{pgDateExpr} + {intervalExpr}";
        }

        return result;
    }
}
