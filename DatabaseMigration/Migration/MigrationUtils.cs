using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        /// 从指定的 SQL 脚本中提取第一个完整的 SQL 语句
        /// 以便整个语句可以作为一个单元进行转换,如一个select语句，或一个if语句块
        /// 循环处理每一行，先判断每一行是否是一些特殊情况，也是则进行对应的特殊情况处理，否则直接将该行加入firstSql
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static (string firstSql,string otherSql) GetFirstCompleteSqlSentence(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return (sql, string.Empty);
            //先将\r\n统一为\n，方便后续处理
            sql = sql.Replace("\r\n", "\n");
            var lines = sql.Split('\n', StringSplitOptions.None);
            var firstSql = new StringBuilder();
            var otherSql = new StringBuilder();

            var isInBlockComment = false; // 标志当前是否在块注释内，此语句包含从/*开始，到*/结束的所有行
            var isInIfBlock = false; // 标志当前是否在 if 块内，此语句包含从if开始，下面的一个begin end块或者是一个完整的其他语句
            var isInBeginEndBlock = false; // 标志当前是否在 begin end 块内，此语句包含从begin开始，到end结束的所有行
            var isInCreateProcedureBlock = false; // 标志当前是否在 create procedure 块内,此语句包含从create procedure开始，到as结束的所有行

            int i = 0;
            for (; i < lines.Length; i++)
            {
                var line = lines[i];
                #region 处理create procedure 块，从create procedure开始，到as结束
                if (IsStartWithCreateProcedure(line))
                {
                    isInCreateProcedureBlock = true;
                    firstSql.AppendLine(line);
                    continue;
                }
                if(isInCreateProcedureBlock)
                {
                    firstSql.AppendLine(line);
                    if (IsEndWithAs(line))
                    {
                        isInCreateProcedureBlock = false;
                        // create procedure 块结束，认为当前完整语句已经结束，跳出循环
                        break;
                    }
                    continue;
                }
                #endregion
                #region 处理块注释的开始和结束
                // 处理块注释的开始
                if (IsStartWithBlockComment(line))
                {
                    //如果当前语句是块注释的开始，并且之前已经有firstSql了，则认为之前的语句已经完整，当前块注释算是其他语句，所以将i-1后直接退出循环
                    if (firstSql.Length > 0)
                    {
                        i--;
                        break;
                    }
                    isInBlockComment = true;
                    firstSql.AppendLine(line);
                    // 如果当前行同时包含块注释的开始和结束标记，则将该行加入 firstSql, 并且认为当前完整语句已经结束，跳出循环
                    if (IsEndWithBlockComment(line))
                    {
                        isInBlockComment = false;
                        break;
                    }
                    continue;
                }
                // 如果已经在块注释内，并且当前行不是块注释的结束标记，则将该行加入 firstSql，继续处理后续行
                if (isInBlockComment)
                {
                    firstSql.AppendLine(line);
                    // 如果当前行包含块注释的结束标记，则将该行加入 firstSql, 并且认为当前完整语句已经结束，跳出循环
                    if (IsEndWithBlockComment(line))
                    {
                        isInBlockComment = false;
                        break;
                    }
                    continue;
                }
                #endregion
                #region 处理if语句块，从if开始，到下面的一个begin end块，或者是一个完整的其他语句
                // 如果当前语句是if开头的语句块，则设置标志，并将该行加入 firstSql，继续处理后续行，直到遇到END标记
                if (IsStartWithIf(line))
                {
                    //如果当前语句是if开头，并且之前已经有firstSql了，则认为之前的语句已经完整，当前if语句算是其他语句，所以将i-1后直接退出循环
                    if (firstSql.Length > 0)
                    {
                        i--;
                        break;
                    }
                    isInIfBlock = true;
                    firstSql.AppendLine(line);
                    continue;
                }
                // 处理if语句后面的begin end块
                if (isInIfBlock)
                {
                    if (IsStartWithBegin(line))
                    {
                        isInBeginEndBlock = true;
                        firstSql.AppendLine(line);
                        continue;
                    }
                    // 如果已经在if 的begin end块内，并且当前行就是end标记，则将该行加入 firstSql，当前完整语句就是if块，后续的都算 otherSql
                    if (isInBeginEndBlock && IsEndWithEnd(line))
                    {
                        firstSql.AppendLine(line);
                        break;
                    }
                    // 如果已经在if 块内,则将该行加入 firstSql，继续处理后续行
                    firstSql.AppendLine(line);
                    continue;
                }
                #endregion
                #region 处理单行注释语句
                /*
                 单行注释有以下情况：
                1. 以 -- 开头的行注释
                2. 本行注释是后续新行的注释说明，则说明本行之前的语句已经是完事的语句了，可以直接返回，如：
                  delete AuthButtons  
  --DELETE AuthButtons   
  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,mask) values('0','1','捷信达捷云系统运营管理',1,'1001000000') 
                3. 本行注释是上一行和后面行的一部分，则需要包含在一起，并且继续读取后面的行，如：
                CREATE TABLE dbo.HotelRegion  
(  
HotelID VARCHAR(6) PRIMARY KEY NOT NULL,  
GroupID VARCHAR(6) NOT NULL,  
Name VARCHAR(100) NOT NULL,  
City VARCHAR(100),  
Content VARCHAR(100),  
Mobile VARCHAR(100) NOT NULL,  
[Address] VARCHAR(400),  
--DefalutLoginName VARCHAR(400),  
CreateTime DATETIME  
)  

                 */
                if (IsStartWithLineComment(line) && firstSql.Length > 0)
                {
                    //取出下一行判断是否是新行的开始，是则表示上一行是一个完整的语句
                    //特殊情况是后面没有其他行了，则也认为上一行是一个完整的语句
                    var nextLine = GetRecentNotEmptyAndNotCommentLine(lines, i);
                    if (IsNewSqlSentenceStart(nextLine) || string.IsNullOrWhiteSpace(nextLine))
                    {
                        i--;
                        break;
                    }
                }
                #endregion
                //如果当前没有在特殊状态下，并且当前行是一个新语句的开始，则认为之前的语句是已经完整的了，将当前行认为是其他语句，所以将i-1后直接退出循环
                // Treat a standalone line comment (starting with --) as the start of the next segment as well
                if (firstSql.Length > 0 && IsNewSqlSentenceStart(line))
                 {
                     i--;
                     break;
                 }
                // 其他情况下，则表示当前行是普通行算是firstSql的一部分，然后继续处理后续行
                firstSql.AppendLine(line);
            }
            //跳出循环，则说明已经找到一个完事的sql语句块，后续的都算otherSql
            //由于跳出循环时，后面的i++不会执行，所以本次循环的初始语句中需要补一次，否则语句会有重复
            for (i++; i < lines.Length -1; i++)
            {
                otherSql.AppendLine(lines[i]);
            }
            //最后一行单独处理，避免多出一个换行
            if (i == lines.Length -1)
            {
                otherSql.Append(lines[i]);
            }

            return (firstSql.ToString(), otherSql.ToString());
        }
        /// <summary>
        /// 获取指定索引之后最近的非空且非注释行
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="currentIndex"></param>
        /// <returns></returns>
        private static string GetRecentNotEmptyAndNotCommentLine(string[] lines, int currentIndex)
        {
            for (int i = currentIndex; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (IsStartWithLineComment(line)) continue;
                if (IsStartWithBlockComment(line) && !IsEndWithBlockComment(line)) continue;
                return line;
            }
            return string.Empty;
        }
        /// <summary>
        /// 从指定的if语句块中提取if条件语句
        /// 如
        /// IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('hotel') AND name = 'customerStatus')
        /// BEGIN
        /// ALTER TABLE hotel ADD customerStatus VARCHAR(2) NOT NULL DEFAULT '0'
        /// END
        /// 则提取后的
        /// 1. if条件语句为:IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('hotel') AND name = 'customerStatus')
        /// 2. 其他语句为: BEGIN ALTER TABLE hotel ADD customerStatus VARCHAR(2) NOT NULL DEFAULT '0' END
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static (string ifConditionSql,string other) GetIfConditionSql(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return (string.Empty, string.Empty);

            // Normalize newlines for easier processing
            var norm = sql.Replace("\r\n", "\n");

            // Find the first IF (at start of string or after leading whitespace)
            var m = Regex.Match(norm, @"^\s*if\b", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (!m.Success)
            {
                return (string.Empty, sql);
            }

            int startIndex = m.Index;

            // Look for the first '(' after the IF keyword to decide whether to parse a parenthesized condition
            int firstParen = -1;
            for (int i = m.Index + m.Length; i < norm.Length; i++)
            {
                char c = norm[i];
                if (c == '(')
                {
                    firstParen = i;
                    break;
                }
                if (c == '\n')
                {
                    // reached end of the first line without finding '(', stop searching
                    break;
                }
            }

            if (firstParen == -1)
            {
                // No opening parenthesis on the IF line: return the first line as the condition
                var lineEnd = norm.IndexOf('\n', m.Index);
                if (lineEnd < 0) lineEnd = norm.Length;
                var cond = norm.Substring(startIndex, lineEnd - startIndex).TrimEnd('\r', '\n');
                var other = norm.Substring(lineEnd < norm.Length ? lineEnd + 1 : lineEnd);
                return (cond, other);
            }

            // We have an opening parenthesis; find the matching closing parenthesis, taking quotes into account
            int depth = 0;
            bool inSingle = false;
            bool inDouble = false;
            int closeIndex = -1;
            for (int i = firstParen; i < norm.Length; i++)
            {
                char c = norm[i];
                if (c == '\'' && !inDouble)
                {
                    // toggle single quote unless escaped by another single (T-SQL uses '' to escape)
                    // handle doubled single quotes: skip the next quote if present
                    if (inSingle && i + 1 < norm.Length && norm[i + 1] == '\'')
                    {
                        i++; // skip escaped quote inside literal
                        continue;
                    }
                    inSingle = !inSingle;
                    continue;
                }
                if (c == '"' && !inSingle)
                {
                    inDouble = !inDouble;
                    continue;
                }

                if (inSingle || inDouble) continue;

                if (c == '(') depth++;
                else if (c == ')')
                {
                    depth--;
                    if (depth == 0)
                    {
                        closeIndex = i;
                        break;
                    }
                }
            }

            if (closeIndex == -1)
            {
                // parenthesis not balanced; fall back to returning first line
                var lineEnd = norm.IndexOf('\n', m.Index);
                if (lineEnd < 0) lineEnd = norm.Length;
                var cond = norm.Substring(startIndex, lineEnd - startIndex).TrimEnd('\r', '\n');
                var other = norm.Substring(lineEnd < norm.Length ? lineEnd + 1 : lineEnd);
                return (cond, other);
            }

            // Include any trailing spaces/tabs after the closing parenthesis but stop before newline
            int endIndex = closeIndex;

            // Determine the end of the condition: examine the remainder of the current line (from closeIndex+1 to the next newline)
            int nextNewline = norm.IndexOf('\n', closeIndex + 1);
            int segmentEnd = (nextNewline >= 0) ? nextNewline - 1 : norm.Length - 1;

            // Extract the segment between the closing parenthesis and the line end
            var segment = (segmentEnd >= closeIndex + 1) ? norm.Substring(closeIndex + 1, segmentEnd - (closeIndex + 1) + 1) : string.Empty;

            // If the segment (after trimming leading spaces) starts with a SQL keyword that should be considered part of 'other' (e.g., BEGIN),
            // then the condition ends at the last space before that keyword; otherwise include the entire segment as part of the condition.
            var segTrimStart = segment.TrimStart();
            if (!string.IsNullOrEmpty(segTrimStart) && Regex.IsMatch(segTrimStart, "^(BEGIN|CREATE|ALTER|DROP|IF)\\b", RegexOptions.IgnoreCase))
            {
                // end condition at the position before the first non-space character of the segment
                int firstNonSpace = closeIndex + 1 + (segment.Length - segment.TrimStart().Length);
                endIndex = firstNonSpace - 1;
            }
            else
            {
                // include the entire segment (this will capture cases like " IS NULL")
                endIndex = segmentEnd;
            }

            var condition = norm.Substring(startIndex, endIndex - startIndex + 1);

            // The 'other' begins after the newline following the condition (if any). If condition ends on its own line, skip that newline.
            int otherStart = endIndex + 1;
            if (otherStart < norm.Length && norm[otherStart] == '\n') otherStart++;
            var otherSql = otherStart < norm.Length ? norm.Substring(otherStart) : string.Empty;

            return (condition, otherSql);
        }
        /// <summary>
        /// 是否包含块注释的开始标记（/*）
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsStartWithBlockComment(string s) => s.TrimStart().StartsWith("/*");
        /// <summary>
        /// 是否包含块注释的结束标记（*/）
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsEndWithBlockComment(string s) => s.TrimEnd().EndsWith("*/");
        /// <summary>
        /// 是否是以单行注释开头的行（--）
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsStartWithLineComment(string s) => s.TrimStart().StartsWith("--");
        /// <summary>
        /// 是否包含 if 语句的开始标记（if）
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsStartWithIf(string s) => s.TrimStart().StartsWith("if", StringComparison.InvariantCultureIgnoreCase);
        /// <summary>
        /// 是否包含 delete 语句的开始标记（delete）
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsStartWithDelete(string s) => s.TrimStart().StartsWith("delete", StringComparison.InvariantCultureIgnoreCase);
        /// <summary>
        /// 是否包含begin end语句块的开始标记（begin）
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsStartWithBegin(string s) => s.TrimStart().StartsWith("begin", StringComparison.InvariantCultureIgnoreCase);
        /// <summary>
        /// 是否包含begin end语句块的结束标记（end）
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsEndWithEnd(string s) => s.TrimStart().StartsWith("end", StringComparison.InvariantCultureIgnoreCase);
        /// <summary>
        /// 判断指定行是否为新 SQL 语句的开始
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsNewSqlSentenceStart(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            var trimmed = s.TrimStart();
            // 粗略判断新语句的开始：以常见SQL关键字开头的行
            return Regex.IsMatch(trimmed, @"^(SELECT|INSERT|UPDATE|DELETE|CREATE|ALTER|DROP|IF|BEGIN|WITH|DECLARE|EXEC|EXECUTE)\b", RegexOptions.IgnoreCase);
        }
        /// <summary>
        /// 是否为 CREATE PROCEDURE 语句的开始行
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsStartWithCreateProcedure(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            return Regex.IsMatch(s, @"^\s*create\s+procedure\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
        /// <summary>
        /// 是否包含as这个单独语句
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsEndWithAs(string s) => s.TrimStart().StartsWith("as", StringComparison.InvariantCultureIgnoreCase);
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
        public static string RemoveSchemaPrefix(string sql, string schema="dbo.")
        {
            if (string.IsNullOrEmpty(sql) || string.IsNullOrWhiteSpace(schema)) return sql;

            // 允许 schema 以点结尾（例如传入 "dbo."），这里统一去掉尾部的点再构造正则
            var sch = schema.Trim();
            if (sch.EndsWith(".")) sch = sch.Substring(0, sch.Length - 1);

            // 提取裸名字（去掉方括号或双引号），用于构造多种匹配形式
            var bare = sch.Trim();
            if (bare.StartsWith("[") && bare.EndsWith("]")) bare = bare.Substring(1, bare.Length - 2);
            if (bare.StartsWith("\"") && bare.EndsWith("\"")) bare = bare.Substring(1, bare.Length - 2);

            // 构造可匹配的 schema 变体：bare, [bare], "bare"
            var plainEsc = Regex.Escape(bare);
            var bracketEsc = Regex.Escape("[" + bare + "]");
            var quoteEsc = Regex.Escape("\"" + bare + "\"");

            // 匹配最外层的 sch.标识符（避免位于字符串/注释内的粗略情况：排除前一字符为 ' " \ / ])
            // 支持 bare., [bare]. 和 "bare".
            var pattern = $@"(?<!['""\\/\]])(?:{bracketEsc}|{quoteEsc}|\b{plainEsc}\b)\.(?=[A-Za-z_])";
            return Regex.Replace(sql, pattern, string.Empty, RegexOptions.IgnoreCase);
        }
        /// <summary>
        /// 执行sp_helptext来获取对象定义，同时处理一行被sp_helptext截断的情况
        /// 由于sp_helptext返回的每行都会带有\r\n，为了方便，全部都替换为空后再进行拼接，由程序判断是否合并行和换行
        /// </summary>
        /// <param name="connection">已打开的 SQL Server 连接。</param>
        /// <param name="name">对象名称</param>
        /// <returns>对象的定义脚本文本（包含原始换行与格式）；若对象不存在、无权限或不可脚本化（例如被加密）则返回空字符串</returns>
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
                    line = line.TrimEnd('\r', '\n'); // 先将尾部的\r\n统一去掉，方便后续处理
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