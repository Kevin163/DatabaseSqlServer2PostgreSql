using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.IO;

namespace DatabaseMigration.Migration;

/// <summary>
/// 提供与 <see cref="string"/> 相关的扩展方法集合。
/// </summary>
/// <remarks>
/// 该静态类用于放置应用中通用的字符串辅助/扩展方法，比如换行符规范化等。
/// 这些方法均为无状态的纯函数，适合在多线程环境中安全调用。
/// </remarks>
public static class StringExtension
{
    /// <summary>
    /// 将输入字符串中的所有换行表示规范化为单一的行结束符（LF，'\n'）。
    /// </summary>
    /// <param name="input">
    /// 要规范化的输入字符串。若为 <c>null</c>，方法将返回 <c>null</c>。
    /// </param>
    /// <returns>
    /// 如果 <paramref name="input"/> 为 <c>null</c>，则返回 <c>null</c>；否则返回一个新字符串，
    /// 其中所有 CRLF（"\r\n"）和孤立的 CR（"\r"） 都被替换为单个 LF（"\n"）。
    /// </returns>
    /// <remarks>
    /// - 此方法不会修改原始字符串（字符串为不可变类型）；会返回一个新的字符串或 <c>null</c>。
    /// - 替换顺序为先把 CRLF 转为 LF，再把孤立的 CR 转为 LF，以避免重复替换。
    /// - 该实现为线性时间复杂度（相对于输入长度），在典型文本处理中性能良好。
    /// - 该方法仅规范化换行表示，不会对其他字符或空白做任何额外处理。
    /// </remarks>
    /// <example>
    /// 示例：
    /// <code>
    /// string s1 = "line1\r\nline2\r\n";
    /// string s2 = s1.NormalizeLineEndings(); // s2 -> "line1\nline2\n"
    ///
    /// string s3 = "line1\rline2\n";
    /// string s4 = s3.NormalizeLineEndings(); // s4 -> "line1\nline2\n"
    ///
    /// string s5 = null;
    /// string s6 = s5.NormalizeLineEndings(); // s6 -> null
    /// </code>
    /// </example>
    /// <seealso cref="Environment.NewLine"/>
    public static string NormalizeLineEndings(this string input)
    {
        if (input == null) return null;
        return input.Replace("\r\n", "\n").Replace("\r", "\n");
    }
    /// <summary>
    /// 将输入字符串转换为PostgreSQL的标识符格式。
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string ToPostgreSqlIdentifier(this string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var name = input.ToLower()
            .Replace("dbo.", "")
            .TrimQuotes();
        return name;
    }
    /// <summary>
    /// 去除字符串两端的各种引号字符，包括方括号、中英文单引号和双引号。
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string TrimQuotes(this string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var name = input.Trim('[', ']', '\'', '"');
        if (name.StartsWith("n'",StringComparison.OrdinalIgnoreCase))
        {
            name = name[2..];
        }
        return name;
    }
    /// <summary>
    /// 将 SQL 字符串解析为 TSqlFragment 对象。
    /// </summary>
    /// <param name="sql"></param>
    /// <returns></returns>
    /// <exception cref="System.Exception"></exception>
    public static TSqlFragment ParseToFragment(this string sql)
    {
        var parser = new TSql170Parser(true);
        using var rdr = new StringReader(sql);
        var frag = parser.Parse(rdr, out var errors);
        if (errors != null && errors.Count > 0)
        {
            throw new System.Exception("Parse errors: " + string.Join(";", errors));
        }
        return frag;
    }
}
