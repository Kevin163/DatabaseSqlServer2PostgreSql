using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace DatabaseMigration.Migration
{
    /// <summary>
    /// 提供从 SQL Server 到 PostgreSQL 的迁移转换工具方法集合。
    /// 包含数据类型映射、对象定义抓取（保留原始换行）以及视图定义从 T-SQL 到 PostgreSQL 的基础语法转换。
    /// </summary>
    public static class MigrationUtils
    {
        /// <summary>
        /// sp_helptext 每行返回的最大长度（含行尾），超过该长度的行会被截断。
        /// </summary>
        private const int RowLengthForSpHelpText = 255;
        /// <summary>
        /// 将 SQL Server 的列数据类型映射为 PostgreSQL 的兼容类型字符串。
        /// </summary>
        /// <param name="sqlServerType">SQL Server 的类型名称（来自 INFORMATION_SCHEMA.COLUMNS.DATA_TYPE）。</param>
        /// <param name="column">列的架构行（来自 GetSchema("Columns") 或 INFORMATION_SCHEMA），用于读取长度/精度/标度等附加信息。</param>
        /// <returns>PostgreSQL 的类型声明（可能包含长度/精度/标度）。</returns>
        /// <remarks>
        /// - varchar/nvarchar 当 CHARACTER_MAXIMUM_LENGTH 为 -1（即 SQL Server 的 MAX）时，将映射为 PostgreSQL 的 text。
        /// - datetime/datetime2/smalldatetime 映射为 timestamp；datetimeoffset 映射为 timestamptz。
        /// - binary/varbinary/image/timestamp 映射为 bytea。
        /// - tinyint 映射为 smallint。
        /// - 未识别类型将回退为 text。
        /// </remarks>
        public static string ConvertToPostgresType(string sqlServerType, DataRow column)
        {
            switch (sqlServerType.ToLower())
            {
                case "bigint": return "bigint";
                case "binary":
                case "varbinary":
                case "image":
                case "timestamp": return "bytea";
                case "bit": return "boolean";
                case "char":
                case "nchar":
                    if (column["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value)
                        return $"char({column["CHARACTER_MAXIMUM_LENGTH"]})";
                    return "char";
                case "varchar":
                case "nvarchar":
                    if (column["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value)
                    {
                        int maxLen = Convert.ToInt32(column["CHARACTER_MAXIMUM_LENGTH"]);
                        if (maxLen > 0)
                            return $"varchar({maxLen})";
                        else
                            return "text";
                    }
                    return "varchar";
                case "text":
                case "ntext":
                case "xml":
                case "sql_variant": return "text";
                case "date": return "date";
                case "datetime":
                case "datetime2":
                case "smalldatetime": return "timestamp";
                case "datetimeoffset": return "timestamptz";
                case "decimal":
                case "numeric":
                    if (column["NUMERIC_PRECISION"] != DBNull.Value && column["NUMERIC_SCALE"] != DBNull.Value)
                        return $"numeric({column["NUMERIC_PRECISION"]}, {column["NUMERIC_SCALE"]})";
                    return "numeric";
                case "float": return "double precision";
                case "int": return "integer";
                case "money":
                case "smallmoney": return "money";
                case "real": return "real";
                case "smallint":
                case "tinyint": return "smallint";
                case "time": return "time";
                case "uniqueidentifier": return "uuid";
                default: return "text";
            }
        }

        /// <summary>
        /// 获取指定对象（视图/存储过程/函数/触发器等）的 T-SQL 定义脚本（保留原始换行）。
        /// </summary>
        /// <param name="connection">已打开的 SQL Server 连接。</param>
        /// <param name="objectName">对象名称，可以是不带架构名的简单名（将尝试 dbo.{name} 兜底）或带架构的完全限定名（如 dbo.ViewName）。</param>
        /// <returns>
        /// 对象的定义脚本文本（包含原始换行与格式）；若对象不存在、无权限或不可脚本化（例如被加密）则返回空字符串。
        /// </returns>
        /// <remarks>
        /// 本方法仅使用 <c>sp_helptext</c> 获取定义，以确保尽最大可能保留原始换行和格式，便于后续“逐行”处理。
        /// </remarks>
        public static string GetObjectDefinition(SqlConnection connection, string objectName)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (string.IsNullOrWhiteSpace(objectName)) return string.Empty;

            // 先按传入名尝试
            var text = TrySpHelpText(connection, objectName);
            if (!string.IsNullOrEmpty(text)) return text;

            // 未带架构名则再尝试 dbo.{name}
            if (!objectName.Contains("."))
            {
                text = TrySpHelpText(connection, $"dbo.{objectName}");
                if (!string.IsNullOrEmpty(text)) return text;
            }

            return string.Empty;
        }
        /// <summary>
        /// 是否包含块注释的开始标记（/*）
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool HasBlockCommentStart(string s) => s.TrimStart().StartsWith("/*");
        /// <summary>
        /// 是否包含块注释的结束标记（*/）
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool HasBlockCommentEnd(string s) => s.TrimEnd().EndsWith("*/");
        /// <summary>
        /// 是否为可忽略的头部语句（SET 语句或 GO 分隔符）
        /// 有些 SQL Server 对象定义脚本会包含 SET 语句或 GO 分隔符，这些在 PostgreSQL 中没有意义，可以忽略。
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsIgnorableHeader(string s) => Regex.IsMatch(s, @"^\s*(SET\s+\w+|GO)\b", RegexOptions.IgnoreCase);
        /// <summary>
        /// 分析指定行的代码，将代码中的正常语句，注释以及注释的闭合状态进行返回
        /// </summary>
        /// <param name="s">要分析的代码</param>
        /// <returns>分析结果：code:正常语句代码，comment：行内的注释，正常保留，unclosed:注释是否未结束，false:已经结束，true:未结束，说明下一行的代码仍然是注释的一部分</returns>
        public static (string code, string comment, bool unclosed) SplitInlineBlockCommentStart(string s)
        {
            if (string.IsNullOrEmpty(s)) return (s, null, false);

            bool inSQ = false, inDQ = false;
            for (int i = 0; i < s.Length - 1; i++)
            {
                char c = s[i];
                if (c == '\'' && !inDQ) { inSQ = !inSQ; continue; }
                if (c == '"' && !inSQ) { inDQ = !inDQ; continue; }
                if (inSQ || inDQ) continue;

                if (s[i] == '/' && s[i + 1] == '*')
                {
                    int end = s.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    if (end >= 0)
                    {
                        string codeBefore = s.Substring(0, i);
                        string comment = s.Substring(i, end - i + 2);
                        string codeAfter = s.Substring(end + 2);
                        string code = codeBefore + codeAfter;
                        return (code, comment, false);
                    }
                    else
                    {
                        string codeBefore = s.Substring(0, i);
                        string comment = s.Substring(i);
                        return (codeBefore, comment, true);
                    }
                }
            }
            return (s, null, false);
        }
        /// <summary>
        /// sql server 对象定义中，标识符可能使用了中括号括起来，例如 [ColumnName]，而在 PostgreSQL 中，标识符使用双引号括起来，例如 "ColumnName"。
        /// 由于现在创建表的时候，明确已经将列名都转换为不区分大小写的小写形式，因此这里直接将中括号去掉即可，不需要转换为双引号。
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ReplaceBrackets(string s)
        {
            return Regex.Replace(s, @"\[(?<id>[^\[\]]+)\]", m => $"{m.Groups["id"].Value}");
        }
        /// <summary>
        /// 在非字符串/注释中移除指定 schema 前缀（例如dbo.），不会修改注释或字符串内的文本
        /// 例如 "SELECT dbo.Table.Column, 'dbo.test', /* dbo.abc */ FROM dbo.Table" -> "SELECT Table.Column, 'dbo.test', /* dbo.abc */ FROM Table"
        /// </summary>
        public static string RemoveSchemaPrefix(string sql, string schema)
        {
            if (string.IsNullOrEmpty(sql) || string.IsNullOrWhiteSpace(schema)) return sql;

            // 允许 schema 以点结尾（例如传入 "dbo."），这里统一去掉尾部的点再构造正则
            var sch = schema.Trim();
            if (sch.EndsWith(".")) sch = sch.Substring(0, sch.Length - 1);

            // 匹配最外层的 sch.标识符（避免位于字符串/括号内的粗略情况：排除前一字符为 ' " \\ / ]）
            // 例如：dbo.TableName、DBO.fn_xxx 等
            var pattern = $@"(?<!['""\\/\]])\b{Regex.Escape(sch)}\.(?=[A-Za-z_])";
            return Regex.Replace(sql, pattern, string.Empty, RegexOptions.IgnoreCase);
        }
        /// <summary>
        /// 执行sp_helptext来获取对象定义，同时处理一行被sp_helptext截断的情况
        /// </summary>
        /// <param name="connection">已打开的 SQL Server 连接。</param>
        /// <param name="name">对象名称</param>
        /// <returns>对象的定义脚本文本（包含原始换行与格式）；若对象不存在、无权限或不可脚本化（例如被加密）则返回空字符串。</returns>
        private static string TrySpHelpText(SqlConnection connection, string name)
        {
            var sb = new StringBuilder();
            try
            {
                var isSplitedLine = false; // 标志上一行是否被截断
                using var cmd = new SqlCommand("sp_helptext", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@objname", name);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var line = reader.GetString(0);
                    // 处理sp_helptext返回的行被截断的情况（超过255字符的行会被截断）
                    if (isSplitedLine)
                    {
                        //如果上一行被截断，则需要判断当前行是否需要拼接
                        //目前的规则是，如果当前行是以union开头，或是以--开头，则认为是不需要和上一行合并的
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var noStartSpace = line.TrimStart();
                            if (noStartSpace.StartsWith("union", StringComparison.OrdinalIgnoreCase) || noStartSpace.StartsWith("--"))
                            {
                                //不需要拼接的话，直接换行
                                sb.AppendLine();
                            }
                        }
                    }
                    // 如果当前行的长度等于255，则先拼接内容，但不换行，并且设置一个标志表示下一行是续行
                    if (!string.IsNullOrWhiteSpace(line) && line.Length == RowLengthForSpHelpText)
                    {
                        isSplitedLine = true;
                        sb.Append(line);
                    }
                    else
                    {
                        isSplitedLine = false;
                        sb.AppendLine(line);
                    }
                }
            }
            catch
            {
                // 忽略异常（对象不存在/无权限/加密等），返回空字符串
            }
            return sb.ToString();
        }
    }
}