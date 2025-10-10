using System.Text.RegularExpressions;

namespace DatabaseMigration.Migration
{
    /// <summary>
    /// delete 语句相关的工具类
    /// </summary>
    public static class DeleteSqlUtils
    {
        /// <summary>
        /// 尝试解析类似于：
        /// DELETE FROM sysLog WHERE cDate< DATEADD(DAY,-30,GETDATE())
        /// 并提取 tableName（不含 schema）、columnName 和 days（如 -30）。
        /// 返回 true 表示解析成功。
        /// </summary>
        public static bool TryParseDeleteOlderThanDateAdd(string sql, out string tableName, out string columnName, out int days)
        {
            tableName = string.Empty;
            columnName = string.Empty;
            days = 0;
            if (string.IsNullOrWhiteSpace(sql)) return false;

            // 允许换行与空白的变体，大小写不敏感。
            // 捕获 table、column 和 days（三个命名组）。
            var pattern = @"^\s*DELETE\s+FROM\s+(?<table>[\[\]""\w\.]+)\s+WHERE\s+(?<column>[\[\]""\w\.]+)\s*<\s*DATEADD\s*\(\s*DAY\s*,\s*(?<days>-?\d+)\s*,\s*GETDATE\s*\(\s*\)\s*\)\s*;?\s*$";

            var m = Regex.Match(sql.Trim(), pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!m.Success) return false;

            string rawTable = m.Groups["table"].Value ?? string.Empty;
            string rawColumn = m.Groups["column"].Value ?? string.Empty;
            string rawDays = m.Groups["days"].Value ?? string.Empty;

            if (string.IsNullOrEmpty(rawTable) || string.IsNullOrEmpty(rawColumn) || string.IsNullOrEmpty(rawDays))
                return false;

            // 清理方括号或双引号，并取最后一段作为表名（去掉 schema）
            string cleanTable = rawTable.Replace("[", "").Replace("]", "").Replace("\"", "").Trim();
            if (cleanTable.Contains('.'))
            {
                var parts = cleanTable.Split('.');
                cleanTable = parts.Length > 0 ? parts[^1] : cleanTable;
            }

            string cleanColumn = rawColumn.Replace("[", "").Replace("]", "").Replace("\"", "").Trim();

            if (!int.TryParse(rawDays, out var d)) return false;

            tableName = cleanTable;
            columnName = cleanColumn;
            days = d;
            return true;
        }

        /// <summary>
        /// 尝试解析类似于：
        /// DELETE authlist
        /// 或者带 FROM 的变体： DELETE FROM authlist;
        /// 并提取 tableName（不含 schema）。
        /// </summary>
        public static bool TryParseSimpleDelete(string sql, out string tableName)
        {
            tableName = string.Empty;
            if (string.IsNullOrWhiteSpace(sql)) return false;

            // 允许可选的 FROM 关键字和可选的 schema 前缀；匹配方括号或双引号包裹的标识符
            var pattern = @"^\s*DELETE\s+(?:FROM\s+)?(?<table>[\[\]""\w\.]+)\s*;?\s*$";
            var m = Regex.Match(sql.Trim(), pattern, RegexOptions.IgnoreCase);
            if (!m.Success) return false;

            var rawTable = m.Groups["table"].Value ?? string.Empty;
            if (string.IsNullOrEmpty(rawTable)) return false;

            var cleanTable = rawTable.Replace("[", "").Replace("]", "").Replace("\"", "").Trim();
            if (cleanTable.Contains('.'))
            {
                var parts = cleanTable.Split('.');
                cleanTable = parts.Length > 0 ? parts[^1] : cleanTable;
            }

            tableName = cleanTable;
            return true;
        }

        /// <summary>
        /// 迁移delete语句
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static (string convertedSql, string needConvertSql) ConvertDeleteSql(string sql)
        {
            string tableName, columnName;
            // 处理 DELETE FROM sysLog WHERE cDate< DATEADD(DAY,-30,GETDATE()) 这类语句
            if (TryParseDeleteOlderThanDateAdd(sql, out tableName, out columnName, out var days))
            {
                // days may be negative (e.g. -30) meaning "current_date - 30"
                if (days < 0)
                {
                    return ($"DELETE FROM {tableName} WHERE {columnName} < current_date - {System.Math.Abs(days)};\n", "");
                }
                else
                {
                    return ($"DELETE FROM {tableName} WHERE {columnName} < current_date + {days};\n", "");
                }
            }

            // 处理简单的 delete 语句，如: delete authlist 或 delete from authlist
            if (TryParseSimpleDelete(sql, out tableName))
            {
                return ($"DELETE FROM {tableName};\n", "");
            }

            //其他语句，则按待迁移进行返回
            return ("", sql);
        }
    }
}
