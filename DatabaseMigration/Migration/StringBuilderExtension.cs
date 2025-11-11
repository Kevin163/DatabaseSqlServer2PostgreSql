using System.Text;

namespace DatabaseMigration.Migration
{
    /// <summary>
    /// 为 <see cref="StringBuilder"/> 提供便捷扩展方法。
    /// </summary>
    public static class StringBuilderExtension
    {
        /// <summary>
        /// 如果 <paramref name="value"/> 不是 <c>null</c>、空字符串或仅包含空白字符，则将该值作为一行追加到指定的 <see cref="StringBuilder"/> 实例中（追加换行符）。
        /// </summary>
        /// <param name="sb">要追加内容的 <see cref="StringBuilder"/> 实例。不能为 <c>null</c>。</param>
        /// <param name="value">要追加的字符串值；如果为 <c>null</c>、空或仅空白则不会追加。</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="sb"/> 为 <c>null</c> 时抛出。</exception>
        public static void AppendIfNotNullOrWhiteSpace(this StringBuilder sb, string value)
        {
            ArgumentNullException.ThrowIfNull(sb);
            if (!string.IsNullOrWhiteSpace(value))
            {
                sb.Append(value);
            }
        }
        /// <summary>
        /// 如果 <paramref name="sb"/> 非空且末尾不是分号，则在末尾追加分号。
        /// </summary>
        /// <param name="sb">要操作的 <see cref="StringBuilder"/> 实例。不能为 <c>null</c>。</param>
        public static void AppendSemicolonIfMissing(this StringBuilder sb)
        {
            ArgumentNullException.ThrowIfNull(sb);
            // 使用 sb[sb.Length - 1] 而不是索引运算符 ^1，以兼容 StringBuilder（不支持从末尾的 Index 运算符）。
            if (sb.Length > 0 && sb[sb.Length - 1] != ';')
            {
                sb.Append(';');
            }
        }
    }
}
