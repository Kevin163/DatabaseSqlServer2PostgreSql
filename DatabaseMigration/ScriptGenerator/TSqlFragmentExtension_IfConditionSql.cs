using DatabaseMigration.Migration;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Linq;
using System.Security.AccessControl;

namespace DatabaseMigration.ScriptGenerator;

/// <summary>
/// TSqlFragment的扩展方法，专门用于处理IF条件语句
/// 如：
/// if begin ... end
/// if ...
/// </summary>
public static class TSqlFragmentExtension_IfConditionSql
{
    /// <summary>
    /// TSqlFragment中获取从指定索引开始的第一个IF条件语句的所有Token
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static List<TSqlParserToken> GetIfCompleteSql(this IList<TSqlParserToken> tokens, ref int index)
    {
        //确保索引在合理范围内
        if (index < 0 || index >= tokens.Count)
        {
            return new List<TSqlParserToken>();
        }
        var sqlTokens = new List<TSqlParserToken>();
        int count = tokens.Count;
        int i = index;
        //确保指定索引的Token是IF
        var curr = tokens[i];
        if (curr.TokenType != TSqlTokenType.If)
        {
            return new List<TSqlParserToken>();
        }
        sqlTokens.Add(curr);
        i++;
        //继续向后查找，直到找到第一个BEGIN或者分号
        for (; i < count; i++)
        {
            curr = tokens[i];
            sqlTokens.Add(curr);
            if (curr.TokenType == TSqlTokenType.Begin)
            {
                //如果是BEGIN，则继续向后查找，直到找到对应的END
                int beginCount = 1;
                for (i++; i < count; i++)
                {
                    curr = tokens[i];
                    sqlTokens.Add(curr);
                    if (curr.TokenType == TSqlTokenType.Begin)
                    {
                        beginCount++;
                    }
                    else if (curr.TokenType == TSqlTokenType.End)
                    {
                        beginCount--;
                        if (beginCount == 0)
                        {
                            //找到了对应的END，结束查找
                            i++;
                            break;
                        }
                    }
                }
                break;
            }
            else if (curr.TokenType == TSqlTokenType.Semicolon)
            {
                //如果是分号，则表示IF语句结束
                i++;
                break;
            }
        }
        index = i;
        return sqlTokens;
    }
    /// <summary>
    /// TSqlFragment中获取从指定索引开始的第一个IF条件语句的所有Token，只包含IF条件部分，不包含BEGIN...END块
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static List<TSqlParserToken> GetIfConditionOnly(this IList<TSqlParserToken> tokens, ref int index)
    {
        //确保索引在合理范围内
        if (index < 0 || index >= tokens.Count)
        {
            return new List<TSqlParserToken>();
        }
        var sqlTokens = new List<TSqlParserToken>();
        int count = tokens.Count;
        int i = index;
        //确保指定索引的Token是IF
        var curr = tokens[i];
        if (curr.TokenType != TSqlTokenType.If)
        {
            return new List<TSqlParserToken>();
        }
        sqlTokens.Add(curr);
        i++;
        //继续向后查找，直到找到第一个BEGIN或者分号
        for (; i < count; i++)
        {
            curr = tokens[i];
            if (curr.TokenType == TSqlTokenType.Begin || curr.TokenType == TSqlTokenType.Semicolon)
            {
                //如果是BEGIN或者分号，则表示IF条件语句结束
                break;
            }
            sqlTokens.Add(curr);
        }
        index = i;
        return sqlTokens;
    }
    /// <summary>
    /// TSqlFragment的Token序列是否是IF NOT EXISTS (SELECT ... FROM syscolumns WHERE id=OBJECT_ID('tableName') AND name='columnName'),并输出tableName和columnName
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="tableName"></param>
    /// <param name="columnName"></param>
    /// <returns></returns>
    public static bool IsIfNotExistsFromSyscolumnsWhereIdAndName(this IList<TSqlParserToken> tokens, out string tableName,out string columnName)
    {
        tableName = string.Empty;
        columnName = string.Empty;
        //需要检查的所有TokenType列表，其中的表示要取其中的值进行返回
        var tokenTypes = new List<TSqlTokenTypeItem>
        {
            new TSqlTokenTypeItem(TSqlTokenType.If),
            new TSqlTokenTypeItem(TSqlTokenType.Not),
            new TSqlTokenTypeItem(TSqlTokenType.Exists),
            new TSqlTokenTypeItem(TSqlTokenType.LeftParenthesis),
            new TSqlTokenTypeItem(TSqlTokenType.Select),
            new TSqlTokenTypeItem(TSqlTokenType.From),
            new TSqlTokenTypeItem(TSqlTokenType.Identifier,value:"syscolumns"),
            new TSqlTokenTypeItem(TSqlTokenType.Where),
            new TSqlTokenTypeItem(TSqlTokenType.Identifier,value:"id"),
            new TSqlTokenTypeItem(TSqlTokenType.EqualsSign),
            new TSqlTokenTypeItem(TSqlTokenType.Identifier,value:"OBJECT_ID"),
            new TSqlTokenTypeItem(TSqlTokenType.LeftParenthesis),
            new TSqlTokenTypeItem(TSqlTokenType.AsciiStringLiteral, action:TSqlTokenTypeAction.OutValue), 
            new TSqlTokenTypeItem(TSqlTokenType.RightParenthesis),
            new TSqlTokenTypeItem(TSqlTokenType.And),
            new TSqlTokenTypeItem(TSqlTokenType.Identifier,value:"name"),
            new TSqlTokenTypeItem(TSqlTokenType.EqualsSign),
            new TSqlTokenTypeItem(TSqlTokenType.AsciiStringLiteral, action:TSqlTokenTypeAction.OutValue),
            new TSqlTokenTypeItem(TSqlTokenType.RightParenthesis),
        };
        var isMatch = tokens.IsMatchTokenTypesSequence(tokenTypes, out var outValues);
        if (isMatch && outValues.Count == 2)
        {
            tableName = outValues[0].ToPostgreSqlIdentifier();
            columnName = outValues[1].ToPostgreSqlIdentifier();
            return true;
        }
        return false;
    }
    /// <summary>
    /// TSqlFragment的Token序列是否是IF OBJECT_ID('HuiYiMapping') IS NULL，并输出objectName
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="objectName"></param>
    /// <returns></returns>
    public static bool IsIfObjectIdIsNull(this IList<TSqlParserToken> tokens, out string objectName)
    {
        objectName = string.Empty;
        //需要检查的所有TokenType列表，其中的表示要取其中的值进行返回
        var tokenTypes = new List<TSqlTokenTypeItem>
        {
            new TSqlTokenTypeItem(TSqlTokenType.If),
            new TSqlTokenTypeItem(TSqlTokenType.Identifier,value:"OBJECT_ID"),
            new TSqlTokenTypeItem(TSqlTokenType.LeftParenthesis),
            new TSqlTokenTypeItem(TSqlTokenType.AsciiStringLiteral, action:TSqlTokenTypeAction.OutValue),
            new TSqlTokenTypeItem(TSqlTokenType.RightParenthesis),
            new TSqlTokenTypeItem(TSqlTokenType.Is),
            new TSqlTokenTypeItem(TSqlTokenType.Null),
        };
        var isMatch = tokens.IsMatchTokenTypesSequence(tokenTypes, out var outValues);
        if (isMatch && outValues.Count == 1)
        {
            objectName = outValues[0].ToPostgreSqlIdentifier();
            return true;
        }
        return false;
    }

    /// <summary>
    ///解析 IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'TryHotelIdForGroup')这类语句的Token序列
    /// 并输出表名和where条件的value（仅支持单一等值条件）
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsIfNotExistsSelectFromSysParaWhereCodeEqualCondition(this IList<TSqlParserToken> tokens, out string codeValue)
    {
        codeValue = string.Empty;
        //需要检查的所有TokenType列表，其中的表示要取其中的值进行返回
        var tokenTypes = new List<TSqlTokenTypeItem>
        {
            new TSqlTokenTypeItem(TSqlTokenType.If),
            new TSqlTokenTypeItem(TSqlTokenType.Not),
            new TSqlTokenTypeItem(TSqlTokenType.Exists),
            new TSqlTokenTypeItem(TSqlTokenType.LeftParenthesis),
            new TSqlTokenTypeItem(TSqlTokenType.Select),
            new TSqlTokenTypeItem(TSqlTokenType.From),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier,TSqlTokenType.QuotedIdentifier },value:"sysPara"),
            new TSqlTokenTypeItem(TSqlTokenType.Where),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier,TSqlTokenType.QuotedIdentifier },value:"code"),
            new TSqlTokenTypeItem(TSqlTokenType.EqualsSign),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.AsciiStringLiteral,TSqlTokenType.UnicodeStringLiteral,TSqlTokenType.AsciiStringOrQuotedIdentifier }, action: TSqlTokenTypeAction.OutValue),
            new TSqlTokenTypeItem(TSqlTokenType.RightParenthesis),
        };
        var isMatch = tokens.IsMatchTokenTypesSequence(tokenTypes, out var outValues);
        if (isMatch && outValues.Count == 1)
        {
            codeValue = outValues[0];
            return true;
        }
        return false;
    }

    /// <summary>
    /// 解析 IF NOT EXISTS(SELECT * FROM sysobjects WHERE name = 'ImeiMappingHid') 这类语句的Token序列
    /// 并输出where条件的name值（仅支持单一等值条件）
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="nameValue"></param>
    /// <returns></returns>
    public static bool IsIfNotExistsSelectFromSysobjectsWhereNameEqualCondition(this IList<TSqlParserToken> tokens, out string nameValue)
    {
        nameValue = string.Empty;
        var tokenTypes = new List<TSqlTokenTypeItem>
        {
            new TSqlTokenTypeItem(TSqlTokenType.If),
            new TSqlTokenTypeItem(TSqlTokenType.Not),
            new TSqlTokenTypeItem(TSqlTokenType.Exists),
            new TSqlTokenTypeItem(TSqlTokenType.LeftParenthesis),
            new TSqlTokenTypeItem(TSqlTokenType.Select),
            new TSqlTokenTypeItem(TSqlTokenType.From),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier}, value: "sysobjects"),
            new TSqlTokenTypeItem(TSqlTokenType.Where),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier}, value: "name"),
            new TSqlTokenTypeItem(TSqlTokenType.EqualsSign),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.AsciiStringLiteral, TSqlTokenType.UnicodeStringLiteral, TSqlTokenType.AsciiStringOrQuotedIdentifier}, action: TSqlTokenTypeAction.OutValue),
            new TSqlTokenTypeItem(TSqlTokenType.RightParenthesis),
        };
        var isMatch = tokens.IsMatchTokenTypesSequence(tokenTypes, out var outValues);
        if (isMatch && outValues.Count == 1)
        {
            nameValue = outValues[0].ToPostgreSqlIdentifier();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 解析 IF NOT EXISTS( SELECT * FROM sysobjects WHERE name = ( SELECT TOP1 name FROM sys.indexes WHERE is_primary_key =1 AND object_id = OBJECT_ID('table') AND name='indexName' ) )
    /// 并输出tableName和indexName（仅支持此固定嵌套结构）
    /// </summary>
    public static bool IsIfNotExistsSysobjectsNameEqualNestedIndexSelect(this IList<TSqlParserToken> tokens, out string tableName, out string indexName)
    {
        tableName = string.Empty;
        indexName = string.Empty;
        var tokenTypes = new List<TSqlTokenTypeItem>
        {
            new TSqlTokenTypeItem(TSqlTokenType.If),
            new TSqlTokenTypeItem(TSqlTokenType.Not),
            new TSqlTokenTypeItem(TSqlTokenType.Exists),
            new TSqlTokenTypeItem(TSqlTokenType.LeftParenthesis),
            new TSqlTokenTypeItem(TSqlTokenType.Select),
            new TSqlTokenTypeItem(TSqlTokenType.From),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier}, value: "sysobjects"),
            new TSqlTokenTypeItem(TSqlTokenType.Where),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier}, value: "name"),
            new TSqlTokenTypeItem(TSqlTokenType.EqualsSign),
            new TSqlTokenTypeItem(TSqlTokenType.LeftParenthesis),
            new TSqlTokenTypeItem(TSqlTokenType.Select),
            // inner select may contain TOP or TOP1 tokens; skip to the name column
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier}, value: "name"),
            new TSqlTokenTypeItem(TSqlTokenType.From),
            // match indexes table (either 'sys.indexes' or tokens separated by dot; match on 'indexes')
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier}, value: "indexes"),
            new TSqlTokenTypeItem(TSqlTokenType.Where),
            // allow is_primary_key = 1 (we don't capture this value)
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier}, value: "is_primary_key"),
            new TSqlTokenTypeItem(TSqlTokenType.EqualsSign),
            new TSqlTokenTypeItem(TSqlTokenType.Integer),
            new TSqlTokenTypeItem(TSqlTokenType.And),
            // object_id = OBJECT_ID('table')
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier}, value: "object_id"),
            new TSqlTokenTypeItem(TSqlTokenType.EqualsSign),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier}, value: "OBJECT_ID"),
            new TSqlTokenTypeItem(TSqlTokenType.LeftParenthesis),
            new TSqlTokenTypeItem(TSqlTokenType.AsciiStringLiteral, action: TSqlTokenTypeAction.OutValue),
            new TSqlTokenTypeItem(TSqlTokenType.RightParenthesis),
            new TSqlTokenTypeItem(TSqlTokenType.And),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier}, value: "name"),
            new TSqlTokenTypeItem(TSqlTokenType.EqualsSign),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.AsciiStringLiteral, TSqlTokenType.UnicodeStringLiteral, TSqlTokenType.AsciiStringOrQuotedIdentifier}, action: TSqlTokenTypeAction.OutValue),
            new TSqlTokenTypeItem(TSqlTokenType.RightParenthesis),
            new TSqlTokenTypeItem(TSqlTokenType.RightParenthesis),
        };

        var isMatch = tokens.IsMatchTokenTypesSequence(tokenTypes, out var outValues);
        // Expect two out values: first is table name (OBJECT_ID arg), second is index name
        if (isMatch && outValues.Count == 2)
        {
            tableName = outValues[0].ToPostgreSqlIdentifier();
            indexName = outValues[1].ToPostgreSqlIdentifier();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 判断tokens序列是否与tokenTypes序列匹配，并输出OutValue的值
    /// </summary>
    public static bool IsMatchTokenTypesSequence(this IList<TSqlParserToken> tokens, List<TSqlTokenTypeItem> tokenTypes, out List<string> outValues)
    {
        outValues = new List<string>();
        if (tokens == null || tokenTypes == null) return false;
        int i = 0, j = 0;
        while (i < tokens.Count && j < tokenTypes.Count)
        {
            var token = tokens[i];
            var typeItem = tokenTypes[j];
            if (!typeItem.TokenTypes.Contains(token.TokenType))
            {
                //如果类型不匹配，则检查下一个Token
                i++;
                continue;
            }
            if (typeItem.Action == TSqlTokenTypeAction.Check && !string.IsNullOrEmpty(typeItem.CheckValue) && !string.Equals(token.Text.ToPostgreSqlIdentifier(), typeItem.CheckValue, StringComparison.OrdinalIgnoreCase))
            {
                //如果是检查值不匹配，则检查下一个Token
                i++;
                continue;
            }
            if (typeItem.Action == TSqlTokenTypeAction.OutValue)
            {
                outValues.Add(token.Text);
            }
            i++;
            j++;
        }
        return j == tokenTypes.Count;
    }

    /// <summary>
    /// 解析 IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.columns WHERE table_name='T' AND column_name='C') 这类语句的Token序列
    /// 并输出 tableName 和 columnName（保留原始大小写，不做小写化）
    /// 支持 table_name 和 column_name 顺序互换的变体
    /// </summary>
    public static bool IsIfNotExistsSelectFromInformationSchemaColumns(this IList<TSqlParserToken> tokens, out string tableName, out string columnName)
    {
        tableName = string.Empty;
        columnName = string.Empty;
        if (tokens == null) return false;

        var headerTypes = new List<TSqlTokenTypeItem>
        {
            new TSqlTokenTypeItem(TSqlTokenType.If),
            new TSqlTokenTypeItem(TSqlTokenType.Not),
            new TSqlTokenTypeItem(TSqlTokenType.Exists),
            new TSqlTokenTypeItem(TSqlTokenType.LeftParenthesis),
            new TSqlTokenTypeItem(TSqlTokenType.Select),
            new TSqlTokenTypeItem(TSqlTokenType.From),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier}, value: "INFORMATION_SCHEMA"),
            new TSqlTokenTypeItem(TSqlTokenType.Dot),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier}, value: "columns"),
            new TSqlTokenTypeItem(TSqlTokenType.Where),
        };

        if (!tokens.IsMatchTokenTypesSequence(headerTypes, out var _)) return false;

        // find WHERE after columns
        int whereIndex = -1;
        for (int i = headerTypes.Count; i < tokens.Count; i++)
        {
            if (tokens[i].TokenType == TSqlTokenType.Where)
            {
                whereIndex = i;
                break;
            }
        }

        whereIndex++; // move to first token after WHERE
        var whereColumnAndValues = GetWhereColumnNameAndColumnValues(tokens, ref whereIndex);

        if (whereColumnAndValues.TryGetValue("table_name", out var rawTable) && whereColumnAndValues.TryGetValue("column_name", out var rawColumn))
        {
            tableName = rawTable.ToPostgreSqlIdentifier();
            columnName = rawColumn.ToPostgreSqlIdentifier();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 解析 IF EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('Table') AND name = 'Col' AND length = 28)
    /// 并输出 tableName 与 columnName，仅当 length 等于 28 时返回 true
    /// </summary>
    public static bool IsIfExistsFromSyscolumnsWhereIdNameAndLength(this IList<TSqlParserToken> tokens, out string tableName, out string columnName, out int length)
    {
        tableName = string.Empty;
        columnName = string.Empty;
        length = 0;
        if (tokens == null) return false;

        var tokenTypes = new List<TSqlTokenTypeItem>
        {
            new TSqlTokenTypeItem(TSqlTokenType.If),
            new TSqlTokenTypeItem(TSqlTokenType.Exists),
            new TSqlTokenTypeItem(TSqlTokenType.LeftParenthesis),
            new TSqlTokenTypeItem(TSqlTokenType.Select),
            new TSqlTokenTypeItem(TSqlTokenType.From),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier}, value: "syscolumns"),
            new TSqlTokenTypeItem(TSqlTokenType.Where),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier}, value: "id"),
            new TSqlTokenTypeItem(TSqlTokenType.EqualsSign),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier}, value: "OBJECT_ID"),
            new TSqlTokenTypeItem(TSqlTokenType.LeftParenthesis),
            new TSqlTokenTypeItem(TSqlTokenType.AsciiStringLiteral, action: TSqlTokenTypeAction.OutValue), // table
            new TSqlTokenTypeItem(TSqlTokenType.RightParenthesis),
            new TSqlTokenTypeItem(TSqlTokenType.And),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier}, value: "name"),
            new TSqlTokenTypeItem(TSqlTokenType.EqualsSign),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.AsciiStringLiteral,TSqlTokenType.UnicodeStringLiteral }, action: TSqlTokenTypeAction.OutValue), // column
            new TSqlTokenTypeItem(TSqlTokenType.And),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier}, value: "length"),
            new TSqlTokenTypeItem(TSqlTokenType.EqualsSign),
            new TSqlTokenTypeItem(TSqlTokenType.Integer, action: TSqlTokenTypeAction.OutValue), // length value
            new TSqlTokenTypeItem(TSqlTokenType.RightParenthesis),
        };

        var isMatch = tokens.IsMatchTokenTypesSequence(tokenTypes, out var outValues);
        if (!isMatch || outValues.Count != 3) return false;

        tableName = outValues[0].ToPostgreSqlIdentifier();
        columnName = outValues[1].ToPostgreSqlIdentifier();
        length = Convert.ToInt32(outValues[2]);

        return true;
    }

    /// <summary>
    /// 解析 IF NOT EXISTS (SELECT * FROM AuthButtons WHERE AuthButtonId='SetHotelLevel' AND AuthButtonValue='524288' AND Seqid='101')
    /// 并提取出 buttonId、buttonValue 与 seqid（支持顺序变体、N 前缀与双引号等）
    /// </summary>
    public static bool IsIfNotExistsSelectFromAuthButtons(this IList<TSqlParserToken> tokens, out string buttonId, out string buttonValue, out string seqid)
    {
        buttonId = string.Empty;
        buttonValue = string.Empty;
        seqid = string.Empty;
        if (tokens == null) return false;

        // Check outer header: IF NOT EXISTS ( SELECT ... FROM AuthButtons WHERE
        var headerTypes = new List<TSqlTokenTypeItem>
        {
            new TSqlTokenTypeItem(TSqlTokenType.If),
            new TSqlTokenTypeItem(TSqlTokenType.Not),
            new TSqlTokenTypeItem(TSqlTokenType.Exists),
            new TSqlTokenTypeItem(TSqlTokenType.LeftParenthesis),
            new TSqlTokenTypeItem(TSqlTokenType.Select),
            new TSqlTokenTypeItem(TSqlTokenType.From),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier}, value: "AuthButtons"),
            new TSqlTokenTypeItem(TSqlTokenType.Where),
        };
        if (!tokens.IsMatchTokenTypesSequence(headerTypes, out var _)) return false;

        var whereIndex = -1;
        for(var i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].TokenType == TSqlTokenType.Where)
            {
                whereIndex = i; break;
            }
        }
        whereIndex++;
        //提取 WHERE 之后的所有条件部分
        var whereColumnAndValues = GetWhereColumnNameAndColumnValues(tokens, ref whereIndex);
        if(whereColumnAndValues.TryGetValue("authbuttonid", out var rawButtonId))
        {
            buttonId = rawButtonId;
        }
        if (whereColumnAndValues.TryGetValue("authbuttonvalue", out var rawButtonValue))
        {
            buttonValue = rawButtonValue;
        }
        if (whereColumnAndValues.TryGetValue("seqid", out var rawSeqid))
        {
            seqid = rawSeqid;
        }

        // 确保都已提取到值
        if (!string.IsNullOrEmpty(buttonId) && !string.IsNullOrEmpty(buttonValue) && !string.IsNullOrEmpty(seqid))
        {
            return true;
        }
        return false;
    }
    /// <summary>
    /// 解析 IF NOT EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'dbo.commonInvoiceInfo') AND type IN ('U'))
    /// 并且提取出对象名称
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="objectName"></param>
    /// <returns></returns>
    public static bool IsIfNotExistsSelectFromSysAllObjectsWhereObjectIdAndTypeIn(this IList<TSqlParserToken> tokens, out string objectName)
    {
        objectName = string.Empty;
        if (tokens == null) return false;
        var tokenTypes = new List<TSqlTokenTypeItem>
        {
            new TSqlTokenTypeItem(TSqlTokenType.If),
            new TSqlTokenTypeItem(TSqlTokenType.Not),
            new TSqlTokenTypeItem(TSqlTokenType.Exists),
            new TSqlTokenTypeItem(TSqlTokenType.LeftParenthesis),
            new TSqlTokenTypeItem(TSqlTokenType.Select),
            new TSqlTokenTypeItem(TSqlTokenType.From),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier}, value: "sys"),
            new TSqlTokenTypeItem(TSqlTokenType.Dot),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier}, value: "all_objects"),
            new TSqlTokenTypeItem(TSqlTokenType.Where),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier}, value: "object_id"),
            new TSqlTokenTypeItem(TSqlTokenType.EqualsSign),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier}, value: "OBJECT_ID"),
            new TSqlTokenTypeItem(TSqlTokenType.LeftParenthesis),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.AsciiStringLiteral,TSqlTokenType.UnicodeStringLiteral,TSqlTokenType.AsciiStringOrQuotedIdentifier }, action: TSqlTokenTypeAction.OutValue), // object name
            new TSqlTokenTypeItem(TSqlTokenType.RightParenthesis),
            new TSqlTokenTypeItem(TSqlTokenType.And),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier}, value: "type"),
            new TSqlTokenTypeItem(TSqlTokenType.In),
            // skipping the IN list details
            new TSqlTokenTypeItem(TSqlTokenType.RightParenthesis),
        };
        var isMatch = tokens.IsMatchTokenTypesSequence(tokenTypes, out var outValues);
        if (isMatch && outValues.Count == 1)
        {
            objectName = outValues[0].ToPostgreSqlIdentifier();
            return true;
        }
        return false;
    }
    /// <summary>
    /// 解析 if exists(select distinct * from ( select hotelCode as hid from posSmMappingHid union all select groupid from posSmMappingHid) a where ISNULL(a.hid,'')!='' and hid not in(select hid from hotelProducts where productCode='ipos')) 这类语句的Token序列
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    public static bool IsIfExistsSelectDistinctFromUnionAllSubquery(this IList<TSqlParserToken> tokens)
    {
        if (tokens == null) return false;
        var tokenTypes = new List<TSqlTokenTypeItem>
        {
            new TSqlTokenTypeItem(TSqlTokenType.If),
            new TSqlTokenTypeItem(TSqlTokenType.Exists),
            new TSqlTokenTypeItem(TSqlTokenType.LeftParenthesis),
            new TSqlTokenTypeItem(TSqlTokenType.Select),
            new TSqlTokenTypeItem(TSqlTokenType.Distinct),
            new TSqlTokenTypeItem(TSqlTokenType.From),
            new TSqlTokenTypeItem(TSqlTokenType.LeftParenthesis),
            new TSqlTokenTypeItem(TSqlTokenType.Select),
            new TSqlTokenTypeItem(TSqlTokenType.From),
            new TSqlTokenTypeItem(TSqlTokenType.Identifier,value:"posSmMappingHid"), 
            new TSqlTokenTypeItem(TSqlTokenType.Union),
            new TSqlTokenTypeItem(TSqlTokenType.All),
            new TSqlTokenTypeItem(TSqlTokenType.Select),
            new TSqlTokenTypeItem(TSqlTokenType.From),
            new TSqlTokenTypeItem(TSqlTokenType.Identifier,value:"posSmMappingHid"),
            new TSqlTokenTypeItem(TSqlTokenType.RightParenthesis),
            new TSqlTokenTypeItem(TSqlTokenType.Where),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier,TSqlTokenType.QuotedIdentifier },value:"hid"),
            new TSqlTokenTypeItem(TSqlTokenType.Not),
            new TSqlTokenTypeItem(TSqlTokenType.In),
            new TSqlTokenTypeItem(TSqlTokenType.LeftParenthesis),
            new TSqlTokenTypeItem(TSqlTokenType.Select),
            new TSqlTokenTypeItem(TSqlTokenType.Identifier,value:"hid"),
            new TSqlTokenTypeItem(TSqlTokenType.From),
            new TSqlTokenTypeItem(TSqlTokenType.Identifier,value:"hotelProducts"),
            new TSqlTokenTypeItem(TSqlTokenType.Where),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier,TSqlTokenType.QuotedIdentifier },value:"productCode"),
            new TSqlTokenTypeItem(TSqlTokenType.EqualsSign),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.AsciiStringLiteral,TSqlTokenType.UnicodeStringLiteral,TSqlTokenType.AsciiStringOrQuotedIdentifier },value:"ipos"),
            new TSqlTokenTypeItem(TSqlTokenType.RightParenthesis),
            new TSqlTokenTypeItem(TSqlTokenType.RightParenthesis),
        };
        var isMatch = tokens.IsMatchTokenTypesSequence(tokenTypes, out var _);
        return isMatch;
    }
    /// <summary>
    /// 解析 IF EXISTS(SELECT * FROM Table WHERE Column1='Value1' AND Column2='Value2') 这类语句的Token序列
    /// 提取出 WHERE 条件中的列名与对应的值，返回字典形式，如 { "Column1": "Value1", "Column2": "Value2" }
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static Dictionary<string,string> GetWhereColumnNameAndColumnValues(IList<TSqlParserToken> tokens,ref int index)
    {
        var result = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        if (tokens == null) return result;
        if (index < 0 || index >= tokens.Count) return result;

        int i = index;
        var tokenItems = new List<TSqlTokenTypeItem>
        {
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier,TSqlTokenType.QuotedIdentifier,TSqlTokenType.AsciiStringOrQuotedIdentifier }),
            new TSqlTokenTypeItem(TSqlTokenType.EqualsSign),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.AsciiStringLiteral,TSqlTokenType.UnicodeStringLiteral,TSqlTokenType.AsciiStringOrQuotedIdentifier })
        };
        var tokenItemIndex = 0;
        var key = "";
        var value = "";
        while (i < tokens.Count)
        {
            var curr = tokens[i];
            // stop conditions
            if (curr.TokenType == TSqlTokenType.RightParenthesis || curr.TokenType == TSqlTokenType.Semicolon)
            {
                i++;
                break;
            }
            //如果是AND，则重置状态，继续下一个
            if (curr.TokenType == TSqlTokenType.And)
            {
                tokenItemIndex = 0;
                i++;
                continue;
            }
            //如果不是预期的Token类型，则跳过
            if (!tokenItems[tokenItemIndex].TokenTypes.Contains(curr.TokenType))
            {
                i++;
                continue;
            }
            if(tokenItemIndex == 0)
            {
                //当前满足条件的Token是列名
                key = curr.Text.ToPostgreSqlIdentifier();
            }
            else if(tokenItemIndex == 2)
            {
                //当前满足条件的Token是列值
                value = curr.Text.TrimQuotes();
                //键和值都已获取，存入结果字典
                result.Add(key, value);
                key = "";
                value = "";
                //每找到一个完整的条件，则将索引指向下一个Token
                index = i + 1;
            }
            tokenItemIndex = (tokenItemIndex + 1) % tokenItems.Count;
            i++;
        }
        return result;
    }
}