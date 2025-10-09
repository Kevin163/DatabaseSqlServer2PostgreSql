using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace DatabaseMigration.Migration
{
    /// <summary>
    /// if 条件相关的工具类
    /// </summary>
    public static class IfConditionUtils
    {
        /// <summary>
        /// 尝试解析 IF NOT EXISTS(... FROM syscolumns ...) 这类语句。
        /// 如果解析成功（即该语句是以 IF NOT EXISTS(SELECT * FROM syscolumns 开头），
        /// 则尝试从其 WHERE 子句中提取 OBJECT_ID('Table') 和 name = 'Column' 并通过 out 返回。
        /// 返回值表示该语句是否为目标类型（不要求一定能提取到表名或列名）。
        /// </summary>
        /// <param name="ifConditionSql">待解析的 if 条件 SQL</param>
        /// <param name="tableName">输出的表名（若没能提取到则为空）</param>
        /// <param name="columnName">输出的列名（若没能提取到则为空）</param>
        /// <returns>当输入为 IF NOT EXISTS(SELECT * FROM syscolumns ... ) 形式时返回 true，否则 false。</returns>
        public static bool TryParseNotExistsSysColumnsCondition(string ifConditionSql, out string tableName, out string columnName)
        {
            tableName = string.Empty;
            columnName = string.Empty;
            if (string.IsNullOrWhiteSpace(ifConditionSql)) return false;

            // 先判断是否符合 IF NOT EXISTS(SELECT * FROM syscolumns ... 的总体形式
            var headerPattern = @"^\s*IF\s+NOT\s+EXISTS\s*\(\s*SELECT\s+\*\s+FROM\s+syscolumns\b";
            if (!Regex.IsMatch(ifConditionSql, headerPattern, RegexOptions.IgnoreCase))
                return false;

            // 解析 OBJECT_ID('...') 作为表名（若包含 schema 则取最后一部分）
            var tableMatch = Regex.Match(ifConditionSql, @"OBJECT_ID\s*\(\s*'(?<table>[^']+)'\s*\)", RegexOptions.IgnoreCase);
            if (tableMatch.Success)
            {
                var tbl = tableMatch.Groups["table"].Value ?? string.Empty;
                if (tbl.Contains('.'))
                {
                    var parts = tbl.Split('.');
                    tableName = parts.Length > 0 ? parts[^1] : tbl;
                }
                else
                {
                    tableName = tbl;
                }
            }

            // 解析 name = 'Column'（允许 N'...'）
            var colMatch = Regex.Match(ifConditionSql, @"\bname\s*=\s*N?'(?<col>[^']+)'(?=\s|\)|;|$)", RegexOptions.IgnoreCase);
            if (colMatch.Success)
            {
                columnName = colMatch.Groups["col"].Value ?? string.Empty;
            }

            return true;
        }

        /// <summary>
        /// 判断if语句是否是IF OBJECT_ID('HuiYiMapping') IS NULL 这类语句
        /// </summary>
        /// <param name="ifConditionSql"></param>
        /// <returns></returns>
        public static bool TryParseIsObjectIdNullCondition(string ifConditionSql,out string tableName)
        {
            tableName = string.Empty;
            if (string.IsNullOrWhiteSpace(ifConditionSql)) return false;
            var pattern = @"^\s*IF\s+OBJECT_ID\s*\(\s*'(?<table>[^']+)'\s*\)\s+IS\s+NULL\b";
            var match = Regex.Match(ifConditionSql, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                tableName = match.Groups["table"].Value ?? string.Empty;
                return true;
            }
            return false;
        }
        /// <summary>
        /// 解析IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'TryHotelIdForGroup')  这类语句
        /// 并且提取表名和 where 条件（仅支持单个等值条件）
        /// </summary>
        /// <param name="ifConditionSql"></param>
        /// <param name="tableName"></param>
        /// <param name="whereConditionItems"></param>
        /// <returns></returns>
        public static bool TryParseSelectFromTableWhenWhereOneEqualCondition(string ifConditionSql, out string tableName, out WhereConditionItem? whereConditionItems)
        {
            tableName = string.Empty;
            whereConditionItems = null;

            if (string.IsNullOrWhiteSpace(ifConditionSql)) return false;

            // 支持的形式（忽略大小写和空白）：
            // IF NOT EXISTS(SELECT * FROM <table> WHERE <column> = 'value')
            // 表名/列名可能包含方 brackets 或引号，value 可能以 N'...' 的形式或使用双引号
            var pattern = @"^\s*IF\s+NOT\s+EXISTS\s*\(\s*SELECT\s+.+?\s+FROM\s+(?<table>[\[\]""'\w\.]+)\s+WHERE\s+(?<column>[\[\]""'\w\.]+)\s*=\s*(?:N?)(?<value>'[^']*'|""[^""]*""|\S+)\s*\)\s*;?\s*$";

            var match = Regex.Match(ifConditionSql.Trim(), pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!match.Success) return false;

            // 解析表名（去掉方 brackets/引号，如果包含 schema 则取最后一段）
            var rawTable = match.Groups["table"].Value ?? string.Empty;
            rawTable = rawTable.Trim().TrimEnd(';');
            var cleanedTable = rawTable.Replace("[", "").Replace("]", "").Replace("\"", "").Replace("'", "").Trim();
            if (cleanedTable.Contains('.'))
            {
                var parts = cleanedTable.Split('.');
                tableName = parts.Last();
            }
            else
            {
                tableName = cleanedTable;
            }

            // 解析列名和 value
            var rawCol = match.Groups["column"].Value ?? string.Empty;
            var col = rawCol.Replace("[", "").Replace("]", "").Replace("\"", "").Replace("'", "").Trim();
            var rawVal = match.Groups["value"].Value ?? string.Empty;
            var val = rawVal.Trim();

            // 去掉 N 前缀和引号
            if ((val.StartsWith("N'", System.StringComparison.OrdinalIgnoreCase) || val.StartsWith("n'", System.StringComparison.OrdinalIgnoreCase)))
            {
                val = val.Substring(1);
            }
            if ((val.StartsWith("'") && val.EndsWith("'")) || (val.StartsWith("\"") && val.EndsWith("\"")))
            {
                if (val.Length >= 2)
                    val = val.Substring(1, val.Length - 2);
            }
            val = val.Trim();

            whereConditionItems = new WhereConditionItem
            {
                ColumnName = col,
                Operator = WhereConditionOperator.Equal,
                Value = val
            };

            return true;
        }
        /// <summary>
        /// 解析if not exists(select id from sysobjects where name = 'ImeiMappingHid')  这类语句
        /// 并且提取对象名
        /// </summary>
        /// <param name="ifConditionSql"></param>
        /// <param name="objectName"></param>
        /// <returns></returns>
        public static bool TryParseNotExistsSelectFromSysObjectsCondition(string ifConditionSql,out string objectName)
        {
            objectName = string.Empty;
            if (string.IsNullOrWhiteSpace(ifConditionSql)) return false;
            // 支持的形式（忽略大小写和空白）：
            // IF NOT EXISTS(SELECT * FROM sysobjects WHERE name = 'ObjectName')
            // if not exists(select id from sysobjects where name = 'ImeiMappingHid')
            var pattern = @"^\s*IF\s+NOT\s+EXISTS\s*\(\s*SELECT\s+.+?\s+FROM\s+sysobjects\s+WHERE\s+name\s*=\s*(?:N)?(?<obj>'[^']*'|""[^""]*""|\S+)\s*\)\s*;?\s*$";
            var match = Regex.Match(ifConditionSql.Trim(), pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!match.Success) return false;
            var rawObj = match.Groups["obj"].Value ?? string.Empty;
            var obj = rawObj.Trim();
            // 去掉 N 前缀和引号
            if ((obj.StartsWith("N'", System.StringComparison.OrdinalIgnoreCase) || obj.StartsWith("n'", System.StringComparison.OrdinalIgnoreCase)))
            {
                obj = obj.Substring(1);
            }
            if ((obj.StartsWith("'") && obj.EndsWith("'")) || (obj.StartsWith("\"") && obj.EndsWith("\"")))
            {
                if (obj.Length >= 2)
                    obj = obj.Substring(1, obj.Length - 2);
            }
            obj = obj.Trim();
            objectName = obj;
            return true;
        }
        /// <summary>
        /// 解析 IF NOT EXISTS( SELECT * from sysobjects where name =( SELECT TOP 1 name FROM sys.indexes  WHERE is_primary_key = 1   AND object_id  = Object_Id('posSmMappingHid') AND name='PK_posSm_20190808912' ) )   这类语句
        /// 并且提取表名和索引名
        /// </summary>
        /// <param name="ifConditionSql"></param>
        /// <param name="tableName"></param>
        /// <param name="indexName"></param>
        /// <returns></returns>
        public static bool TryParseNotExistsSelectFromSysIndexCondition(string ifConditionSql, out string tableName, out string indexName)
        {
            tableName = string.Empty;
            indexName = string.Empty;
            if (string.IsNullOrWhiteSpace(ifConditionSql)) return false;
            // 支持的形式（忽略大小写和空白）：
            // IF NOT EXISTS( SELECT * from sysobjects where name =( SELECT TOP 1 name FROM sys.indexes  WHERE is_primary_key = 1   AND object_id  = Object_Id('posSmMappingHid') AND name='PK_posSm_20190808912' ) )  

            var outerPattern = @"^\s*IF\s+NOT\s+EXISTS\s*\(\s*SELECT\s+.+?\s+FROM\s+sysobjects\s+WHERE\s+name\s*=\s*\(\s*SELECT\s+TOP\s+\d+\s+name\s+FROM\s+sys\.indexes\s+WHERE\s+(?<inner>.*?)\)\s*\)\s*;?\s*$";
            var match = Regex.Match(ifConditionSql.Trim(), outerPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!match.Success) return false;

            var inner = match.Groups["inner"].Value ?? string.Empty;

            // 从 inner 中提取 object_id 的 Object_Id('table')
            var tblMatch = Regex.Match(inner, @"object_id\s*=\s*OBJECT_ID\s*\(\s*'(?<table>[^']+)'\s*\)", RegexOptions.IgnoreCase);
            if (tblMatch.Success)
            {
                var tbl = tblMatch.Groups["table"].Value ?? string.Empty;
                // 去掉 schema
                if (tbl.Contains('.'))
                {
                    var parts = tbl.Split('.');
                    tableName = parts.Length > 0 ? parts[^1] : tbl;
                }
                else
                {
                    tableName = tbl;
                }
            }

            // 从 inner 中提取 name = 'indexName'
            var idxMatch = Regex.Match(inner, @"\bname\s*=\s*(?:N)?'(?<index>[^']+)'", RegexOptions.IgnoreCase);
            if (idxMatch.Success)
            {
                indexName = idxMatch.Groups["index"].Value ?? string.Empty;
            }

            // 如果至少匹配到了内层，则认为是目标类型（即使未能提取到具体名称）
            return match.Success;
        }

        /// <summary>
        /// 获取指定sql中的真实语句块
        /// 如果sql是以begin end语句块，则返回语句块内的语句（即去掉begin end)
        /// 否则返回sql本身
        /// 示例一：
        /// BEGIN
        /// ALTER TABLE hotel ADD customerStatus VARCHAR(2) NOT NULL DEFAULT '0'
        /// END
        /// 将返回ALTER TABLE hotel ADD customerStatus VARCHAR(2) NOT NULL DEFAULT '0'
        /// 救命二：
        /// ALTER TABLE hotel ADD customerStatus VARCHAR(2) NOT NULL DEFAULT '0'
        /// 将返回
        /// ALTER TABLE hotel ADD customerStatus VARCHAR(2) NOT NULL DEFAULT '0'
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static string GetSqlsInBeginAndEnd(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return sql;

            // 按行处理，忽略前后空白/注释，只在最外层为 BEGIN...END 时剥离
            var lines = sql.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            // 寻找第一个非空行，判断是否是begin，如果是则设置标记
            int firstIdx = -1;

            for (int i = 0; i < lines.Length; i++)
            {
                string code = lines[i];
                if (!string.IsNullOrWhiteSpace(code))
                {
                    if (MigrationUtils.IsStartWithBegin(code))
                    {
                        //第一个非空行是以begin开头的，则设置标志，表示需要去掉begin end
                        firstIdx = i + 1;
                        break;
                    }
                    else
                    {
                        //第一个非空行不是begin开头的，则说明就是普通的语句，直接返回整个sql本身
                        return sql;
                    }
                }
            }

            int lastIdx = -1;
            if (firstIdx > -1)
            {
                //寻找最后一个end进行移除，所以倒序查找
                for (int i = lines.Length - 1; i > firstIdx; i--)
                {
                    var code = lines[i];
                    if (MigrationUtils.IsEndWithEnd(code))
                    {
                        lastIdx = i;
                        break;
                    }
                }
                //返回begin 和 end之间的语句
                var contentSql = new StringBuilder();
                for (int i = firstIdx; i < lastIdx; i++)
                {
                    contentSql.AppendLine(lines[i]);
                }
                //移除最后一行的多余换行符
                if (contentSql.Length > 0)
                {
                    contentSql.Length -= Environment.NewLine.Length;
                }
                return contentSql.ToString();
            }

            // 其他情况下，则返回sql 本身
            return sql;
        }
    }
}
