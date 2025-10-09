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
            // 同时支持 if(not exists(select 1 from syscolumns where id=object_id('helpFiles') and name='language')) 这种形式
            var headerPattern = @"^\s*IF\s*(?:\(\s*)?NOT\s+EXISTS\s*\(\s*SELECT\s+(?:\*|1|.+?)\s+FROM\s+syscolumns\b";
            if (!Regex.IsMatch(ifConditionSql, headerPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline))
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
        /// 解析 if not exists(select * from INFORMATION_SCHEMA.columns where table_name='posSmMappingHid' and column_name = 'memberVersion') 这类语句
        /// 并且提取表名和列名
        /// </summary>
        /// <param name="ifConditionSql"></param>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static bool TryParseNotExistsInformationSchemaColumnsCondition(string ifConditionSql, out string tableName, out string columnName)
        {
            tableName = string.Empty;
            columnName = string.Empty;
            if (string.IsNullOrWhiteSpace(ifConditionSql)) return false;

            // 支持的形式（忽略大小写和空白）：
            // IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.columns WHERE table_name = 'T' AND column_name = 'C')
            var outerPattern = @"^\s*IF\s+NOT\s+EXISTS\s*\(\s*SELECT\s+.+?\s+FROM\s+INFORMATION_SCHEMA\.columns\s+WHERE\s+(?<inner>.*?)\)\s*;?\s*$";
            var match = Regex.Match(ifConditionSql.Trim(), outerPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!match.Success) return false;

            var inner = match.Groups["inner"].Value ?? string.Empty;

            // 从 inner 中提取 table_name 和 column_name（顺序可变，用独立正则）
            var tblMatch = Regex.Match(inner, @"\btable_name\s*=\s*(?:N)?(?<table>'[^']*'|""[^"" ]*""|\S+)", RegexOptions.IgnoreCase);
            if (tblMatch.Success)
            {
                var raw = tblMatch.Groups["table"].Value ?? string.Empty;
                var t = raw.Trim();
                if ((t.StartsWith("N'", System.StringComparison.OrdinalIgnoreCase) || t.StartsWith("n'", System.StringComparison.OrdinalIgnoreCase)))
                    t = t.Substring(1);
                if ((t.StartsWith("'") && t.EndsWith("'")) || (t.StartsWith("\"") && t.EndsWith("\"")))
                {
                    if (t.Length >= 2) t = t.Substring(1, t.Length - 2);
                }
                tableName = t.Trim();
            }

            var colMatch = Regex.Match(inner, @"\bcolumn_name\s*=\s*(?:N)?(?<col>'[^']*'|""[^"" ]*""|\S+)", RegexOptions.IgnoreCase);
            if (colMatch.Success)
            {
                var raw = colMatch.Groups["col"].Value ?? string.Empty;
                var c = raw.Trim();
                if ((c.StartsWith("N'", System.StringComparison.OrdinalIgnoreCase) || c.StartsWith("n'", System.StringComparison.OrdinalIgnoreCase)))
                    c = c.Substring(1);
                if ((c.StartsWith("'") && c.EndsWith("'")) || (c.StartsWith("\"") && c.EndsWith("\"")))
                {
                    if (c.Length >= 2) c = c.Substring(1, c.Length - 2);
                }
                columnName = c.Trim();
            }

            return true;
        }

        /// <summary>
        /// 判断if语句是否是IF OBJECT_ID('HuiYiMapping') IS NULL 这类语句
        /// 同时支持if(object_id('versionParas') is null) 这类语句
        /// 并且提取表名
        /// </summary>
        /// <param name="ifConditionSql"></param>
        /// <returns></returns>
        public static bool TryParseIsObjectIdNullCondition(string ifConditionSql,out string tableName)
        {
            tableName = string.Empty;
            if (string.IsNullOrWhiteSpace(ifConditionSql)) return false;
            //需要支持的格式一：IF OBJECT_ID('HuiYiMapping') IS NULL
            //需要支持的格式二：if(object_id('versionParas') is null)
            //同时支持带外层括号的情况：IF ( OBJECT_ID('X') IS NULL )
            var pattern = @"^\s*IF\s*(?:\(\s*)?OBJECT_ID\s*\(\s*'(?<table>[^']+)'\s*\)\s+IS\s+NULL\b(?:\s*\))?\s*;?\s*$";
            var match = Regex.Match(ifConditionSql.Trim(), pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                tableName = match.Groups["table"].Value ?? string.Empty;
                return true;
            }
            return false;
        }
        /// <summary>
        /// 解析IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'TryHotelIdForGroup')  这类语句
        /// IF(NOT EXISTS(SELECT 1 FROM sysPara WHERE code = 'ISPAWeiXinTemplateIDQuitSelect')) 
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
            // IF(NOT EXISTS(SELECT 1 FROM sysPara WHERE code = 'ISPAWeiXinTemplateIDQuitSelect')) 
            // 表名/列名可能包含方 brackets 或引号，value 可能以 N'...' 的形式或使用双引号
            var pattern = @"^\s*IF\s*(?:\(\s*)?NOT\s+EXISTS\s*\(\s*SELECT\s+.+?\s+FROM\s+(?<table>[\[\]""'\w\.]+)\s+WHERE\s+(?<column>[\[\]""'\w\.]+)\s*=\s*(?:N?)(?<value>'[^']*'|""[^"" ]*""|\S+)\s*\)\s*(?:\s*\))?\s*;?\s*$";

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
            var pattern = @"^\s*IF\s+NOT\s+EXISTS\s*\(\s*SELECT\s+.+?\s+FROM\s+sysobjects\s+WHERE\s+name\s*=\s*(?:N)?(?<obj>'[^']*'|""[^"" ]*""|\S+)\s*\)\s*;?\s*$";
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

        /// <summary>
        /// 解析 IF EXISTS(SELECT ... FROM syscolumns WHERE id=OBJECT_ID('Table') AND name = 'Column' AND length = 28)
        /// 并提取 tableName, columnName 和 length（若未提供 length 则返回 null）
        /// </summary>
        public static bool TryParseExistsSysColumnsCondition(string ifConditionSql, out string tableName, out string columnName, out int? length)
        {
            tableName = string.Empty;
            columnName = string.Empty;
            length = null;
            if (string.IsNullOrWhiteSpace(ifConditionSql)) return false;

            // 支持的形式（忽略大小写和空白），允许 IF 后带括号：
            // IF EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('Table') AND name = 'Col' AND length = 28)
            var outerPattern = @"^\s*IF\s*(?:\(\s*)?EXISTS\s*\(\s*SELECT\s+.+?\s+FROM\s+syscolumns\s+WHERE\s+(?<inner>.*?)\)\s*(?:\s*\))?\s*;?\s*$";
            var match = Regex.Match(ifConditionSql.Trim(), outerPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!match.Success) return false;

            var inner = match.Groups["inner"].Value ?? string.Empty;

            // 提取 id = OBJECT_ID('...')
            var tblMatch = Regex.Match(inner, @"\bid\s*=\s*OBJECT_ID\s*\(\s*'(?<table>[^']+)'\s*\)", RegexOptions.IgnoreCase);
            if (tblMatch.Success)
            {
                var tbl = tblMatch.Groups["table"].Value ?? string.Empty;
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

            // 提取 name = 'Column'（允许 N'...'）
            var nmMatch = Regex.Match(inner, @"\bname\s*=\s*N?'(?<col>[^']+)'", RegexOptions.IgnoreCase);
            if (nmMatch.Success)
            {
                columnName = nmMatch.Groups["col"].Value ?? string.Empty;
            }

            // 提取 length = 123
            var lenMatch = Regex.Match(inner, @"\blength\s*=\s*(?<len>\d+)", RegexOptions.IgnoreCase);
            if (lenMatch.Success)
            {
                if (int.TryParse(lenMatch.Groups["len"].Value, out var l))
                {
                    length = l;
                }
            }

            return true;
        }

        /// <summary>
        /// 解析 IF NOT EXISTS (SELECT * FROM AuthButtons WHERE AuthButtonId='SetHotelLevel' AND AuthButtonValue='524288' AND Seqid='101')
        /// 提取 AuthButtonId、AuthButtonValue、Seqid
        /// </summary>
        public static bool TryParseNotExistsSelectFromAuthButtonsCondition(string ifConditionSql, out string buttonId, out string buttonValue, out string seqid)
        {
            buttonId = string.Empty;
            buttonValue = string.Empty;
            seqid = string.Empty;
            if (string.IsNullOrWhiteSpace(ifConditionSql)) return false;

            // 外层匹配，定位到 FROM AuthButtons 并提取 WHERE 子句
            var outerPattern = @"^\s*IF\s*(?:\(\s*)?NOT\s+EXISTS\s*\(\s*SELECT\s+.+?\s+FROM\s+AuthButtons\s+WHERE\s+(?<inner>.*?)\)\s*(?:\s*\))?\s*;?\s*$";
            var match = Regex.Match(ifConditionSql.Trim(), outerPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!match.Success) return false;

            var inner = match.Groups["inner"].Value ?? string.Empty;

            // 从 inner 中分别解析三个条件（顺序可变）
            // 支持 'value' 或 "value" 或 unquoted token
            var idMatch = Regex.Match(inner, @"\bAuthButtonId\s*=\s*(?:N)?(?<id>'[^']*'|""[^"" ]*""|\S+)", RegexOptions.IgnoreCase);
            if (idMatch.Success)
            {
                var raw = idMatch.Groups["id"].Value ?? string.Empty;
                var v = raw.Trim();
                if ((v.StartsWith("N'", System.StringComparison.OrdinalIgnoreCase) || v.StartsWith("n'", System.StringComparison.OrdinalIgnoreCase))) v = v.Substring(1);
                if ((v.StartsWith("'") && v.EndsWith("'")) || (v.StartsWith("\"") && v.EndsWith("\"")))
                {
                    if (v.Length >= 2) v = v.Substring(1, v.Length - 2);
                }
                buttonId = v.Trim();
            }

            var valMatch = Regex.Match(inner, @"\bAuthButtonValue\s*=\s*(?:N)?(?<val>'[^']*'|""[^"" ]*""|\S+)", RegexOptions.IgnoreCase);
            if (valMatch.Success)
            {
                var raw = valMatch.Groups["val"].Value ?? string.Empty;
                var v = raw.Trim();
                if ((v.StartsWith("N'", System.StringComparison.OrdinalIgnoreCase) || v.StartsWith("n'", System.StringComparison.OrdinalIgnoreCase))) v = v.Substring(1);
                if ((v.StartsWith("'") && v.EndsWith("'")) || (v.StartsWith("\"") && v.EndsWith("\"")))
                {
                    if (v.Length >= 2) v = v.Substring(1, v.Length - 2);
                }
                buttonValue = v.Trim();
            }

            var seqMatch = Regex.Match(inner, @"\bSeqid\s*=\s*(?:N)?(?<s>'[^']*'|""[^"" ]*""|\S+)", RegexOptions.IgnoreCase);
            if (seqMatch.Success)
            {
                var raw = seqMatch.Groups["s"].Value ?? string.Empty;
                var v = raw.Trim();
                if ((v.StartsWith("N'", System.StringComparison.OrdinalIgnoreCase) || v.StartsWith("n'", System.StringComparison.OrdinalIgnoreCase))) v = v.Substring(1);
                if ((v.StartsWith("'") && v.EndsWith("'")) || (v.StartsWith("\"") && v.EndsWith("\"")))
                {
                    if (v.Length >= 2) v = v.Substring(1, v.Length - 2);
                }
                seqid = v.Trim();
            }

            // 认为只要 outerPattern 匹配成功即可返回 true（即便某些值为空），保持与现有方法行为一致
            return match.Success;
        }

        /// <summary>
        /// 解析 IF NOT EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'dbo.commonInvoiceInfo') AND type IN ('U'))
        /// 提取 OBJECT_ID 中的表名（保留 schema，如 dbo.commonInvoiceInfo）
        /// </summary>
        public static bool TryParseNotExistsSelectFromAllObjectsCondition(string ifConditionSql, out string tableName)
        {
            tableName = string.Empty;
            if (string.IsNullOrWhiteSpace(ifConditionSql)) return false;

            // 外层匹配，定位到 FROM sys.all_objects 并提取 WHERE 子句
            var outerPattern = @"^\s*IF\s*(?:\(\s*)?NOT\s+EXISTS\s*\(\s*SELECT\s+.+?\s+FROM\s+sys\.all_objects\s+WHERE\s+(?<inner>.*?)\)\s*(?:\s*\))?\s*;?\s*$";
            var match = Regex.Match(ifConditionSql.Trim(), outerPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!match.Success) return false;

            var inner = match.Groups["inner"].Value ?? string.Empty;

            // 从 inner 中提取 object_id = OBJECT_ID(N'...') 或 OBJECT_ID('...')
            var objMatch = Regex.Match(inner, @"\bobject_id\s*=\s*OBJECT_ID\s*\(\s*(?:N)?'(?<obj>[^']+)'\s*\)", RegexOptions.IgnoreCase);
            if (objMatch.Success)
            {
                tableName = objMatch.Groups["obj"].Value ?? string.Empty;
                return true;
            }

            return true; // outer matched, but couldn't extract table name — still treat as matched
        }

        /// <summary>
        /// 判断是否为类似于以下形式的 IF EXISTS 子查询模式（带有内嵌的括号子查询并有别名和 WHERE 子句），
        /// 示例：
        /// if exists(select distinct * from (
        ///     select hotelCode as hid from posSmMappingHid
        ///     union all
        ///     select groupid from posSmMappingHid)a
        ///     where ISNULL(a.hid,'')!='' and hid not in(select hid from hotelProducts where productCode='ipos'))
        /// 该方法仅判断格式是否匹配，无需提取任何元素。
        /// </summary>
        /// <param name="ifConditionSql">待检测的 if 条件 SQL 字符串</param>
        /// <returns>若输入符合上述 EXISTS + 内嵌子查询（带别名及 WHERE）模式则返回 true，否则 false。</returns>
        public static bool IsExistsPosSMMappingHidInHotelProductsWithSubqueryFormat(string ifConditionSql)
        {
            if (string.IsNullOrWhiteSpace(ifConditionSql)) return false;

            // 允许多行匹配，使用 Singleline 使 '.' 能匹配换行。该正则尽量宽松以匹配多种换行/缩进变体：
            // - IF [ ( ] EXISTS ( SELECT DISTINCT * FROM ( <any content> ) <alias> WHERE ... )
            // 要点：匹配 "SELECT DISTINCT * FROM ( ... ) alias" 后跟 WHERE
            var pattern = @"^\s*IF\s*(?:\(\s*)?EXISTS\s*\(\s*SELECT\s+DISTINCT\s*\*\s+FROM\s*\(.*?\)\s*[A-Za-z_][A-Za-z0-9_]*\s+WHERE\b";

            if (!Regex.IsMatch(ifConditionSql, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline))
                return false;

            // 进一步要求必须包含 from posSmMappingHid（允许可选的 schema 前缀，如 dbo.）
            var fromPattern = @"\bFROM\s+(?:[A-Za-z_][A-Za-z0-9_]*\.)?posSmMappingHid\b";
            if (!Regex.IsMatch(ifConditionSql, fromPattern, RegexOptions.IgnoreCase))
                return false;

            // 进一步要求必须包含 hid not in(select hid from hotelProducts where productCode='ipos')
            // 允许可选 schema 前缀和空白变体，且 productCode 的引号为单引号
            var notInPattern = @"\bhid\s+not\s+in\s*\(\s*select\s+hid\s+from\s+(?:[A-Za-z_][A-Za-z0-9_]*\.)?hotelProducts\s+where\s+productCode\s*=\s*'ipos'\s*\)";
            if (!Regex.IsMatch(ifConditionSql, notInPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline))
                return false;

            return true;
        }

    }
}
