using DatabaseMigration.Migration;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Text;

namespace DatabaseMigration.ScriptGenerator;

/// <summary>
/// postgreSQL脚本生成器
/// </summary>
public abstract class PostgreSqlScriptGenerator
{
    /// <summary>
    /// 由子类实现，创建SQL脚本生成访问器
    /// </summary>
    /// <param name="fragment"></param>
    /// <returns></returns>
    public virtual string GenerateSqlScript(TSqlFragment fragment)
    {
        return ConvertSingleOrMultipleSqlOrSqlBatch(fragment.ScriptTokenStream);
    }
    #region 转换单个或多个语句入口
    protected virtual string ConvertSingleOrMultipleSqlOrSqlBatch(IList<TSqlParserToken> sqlTokens)
    {
        var sb = new StringBuilder();
        var len = sqlTokens.Count;
        for (var i = 0; i < len;)
        {
            var singleSqlTokens = sqlTokens.GetFirstCompleteSqlTokens(ref i);
            sb.Append(ConvertSingleSqlOrSqlBatch(singleSqlTokens));
        }
        return sb.ToString();
    }
    #endregion
    #region 转换单个语句或语句块入口
    /// <summary>
    /// 转换单个语句或语句块，默认实现会根据语句的类型调用不同的方法进行处理，子类可以重写此方法以实现更复杂的转换逻辑
    /// </summary>
    /// <param name="sqlTokens"></param>
    /// <returns></returns>
    protected virtual string ConvertSingleSqlOrSqlBatch(IList<TSqlParserToken> sqlTokens)
    {
        var sqlTokenType = sqlTokens.GetFirstNotWhiteSpaceTokenType();
        //处理create...语句
        if (sqlTokenType == TSqlTokenType.Create)
        {
            return ConvertSingleCreateSqlAndSqlBatch(sqlTokens);
        }
        //处理if语句或语句块
        if (sqlTokenType == TSqlTokenType.If)
        {
            return ConvertIfBlockSql(sqlTokens);
        }
        //处理alter语句
        if (sqlTokenType == TSqlTokenType.Alter)
        {
            return ConvertAlterSql(sqlTokens);
        }
        //处理delete语句
        if (sqlTokenType == TSqlTokenType.Delete)
        {
            return ConvertSingleDeleteSql(sqlTokens);
        }
        //处理begin范围语句
        if (sqlTokenType == TSqlTokenType.Begin || sqlTokenType == TSqlTokenType.End)
        {
            return ConvertBeginEndBlockSql(sqlTokens);
        }
        //处理()范围语句
        if (sqlTokenType == TSqlTokenType.LeftParenthesis)
        {
            return ConvertParenthesesBlockSql(sqlTokens);
        }
        //处理select语句
        if (sqlTokenType == TSqlTokenType.Select)
        {
            return ConvertSelectSql(sqlTokens);
        }
        //处理insert语句
        if (sqlTokenType == TSqlTokenType.Insert)
        {
            return ConvertInsertSql(sqlTokens);
        }
        //非特殊语句，则直接添加
        return string.Concat(sqlTokens.Select(w => w.Text));
    }
    #endregion

    #region 转换单个Create语句
    /// <summary>
    /// 转换单个Create语句，默认实现是直接拼接Token文本，子类可以重写此方法以实现更复杂的转换逻辑
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    protected virtual string ConvertSingleCreateSqlAndSqlBatch(IList<TSqlParserToken> tokens)
    {
        //如果是create ... as语句，则分两部分进行处理
        //1 create... as 本身
        //2 as后面的所有语句
        var nextTokens = tokens.Skip(1).ToList();
        var nextTokenType = nextTokens.GetFirstNotWhiteSpaceTokenType();
        if(nextTokenType == TSqlTokenType.View || nextTokenType == TSqlTokenType.Proc || nextTokenType == TSqlTokenType.Procedure || nextTokenType == TSqlTokenType.Function)
        {
            var asIndex = 0;
            for(var i = 2; i < tokens.Count; i++)
            {
                if (tokens[i].TokenType == TSqlTokenType.As)
                {
                    asIndex = i;
                    break;
                }
            }
            var createAsTokens = tokens.Take(asIndex + 1).ToList();
            var afterAsTokens = tokens.Skip(asIndex + 1).ToList();
            var sb = new StringBuilder();
            sb.Append(ConvertSingleCreateAs(createAsTokens));
            sb.Append(ConvertSingleOrMultipleSqlOrSqlBatch(afterAsTokens));
            return sb.ToString();
        }
        //如果是其他情况，则直接拼接
        return string.Concat(tokens.Select(w => w.Text));
    }
    /// <summary>
    /// 转换单个Create ... As语句，默认实现是直接拼接Token文本，子类可以重写此方法以实现更复杂的转换逻辑
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    protected virtual string ConvertSingleCreateAs(IList<TSqlParserToken> tokens)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < tokens.Count; i++)
        {
            var item = tokens[i];
            //处理create,替换为create or replace
            if (item.TokenType == TSqlTokenType.Create)
            {
                sb.Append("CREATE OR REPLACE ");
                continue;
            }
            //处理dbo.name这样的标识符，去掉前面的dbo.
            if (item.TokenType == TSqlTokenType.AsciiStringOrQuotedIdentifier || item.TokenType == TSqlTokenType.QuotedIdentifier)
            {
                sb.Append(tokens.GetIdentityName(ref i));
                continue;
            }
            sb.Append(item.Text);
        }
        //由于create view是到as就结束了，所以在最后需要补上一个换行
        sb.AppendLine();
        return sb.ToString();
    }
    #endregion

    #region 转换单个Delete语句
    /// <summary>
    /// 转换单个Delete语句，默认实现会处理where条件中的dateadd函数调用，子类可以重写此方法以实现更复杂的转换逻辑
    /// postgresql中的delete语句，必须包含from，所以需要将delete table1 转换为delete from table1格式，否则会报错
    /// </summary>
    /// <param name="sqlTokens"></param>
    /// <returns></returns>
    protected virtual string ConvertSingleDeleteSql(IList<TSqlParserToken> sqlTokens)
    {
        var hasFrom = false;
        //从第一个delete开始，到下一个identity之前，检查是否有from关键字
        foreach(var token in sqlTokens)
        {
            if (token.TokenType == TSqlTokenType.From)
            {
                hasFrom = true;
                break;
            }
            if (token.TokenType == TSqlTokenType.AsciiStringOrQuotedIdentifier || token.TokenType == TSqlTokenType.QuotedIdentifier || token.TokenType == TSqlTokenType.Identifier)
            {
                break;
            }
        }
        //转换delete语句
        var sql = new StringBuilder();
        for (var i = 0; i < sqlTokens.Count; i++)
        {
            var item = sqlTokens[i];
            //处理delete,如果后面没有from，则添加from
            if (item.TokenType == TSqlTokenType.Delete && !hasFrom)
            {
                sql.Append("DELETE FROM");
                continue;
            }
            //处理dateadd函数调用
            if (item.TokenType == TSqlTokenType.Identifier && item.Text.Equals("dateadd", StringComparison.OrdinalIgnoreCase))
            {
                sql.Append(sqlTokens.GetDateAddSql(ref i));
                continue;
            }
            sql.Append(item.Text);
        }
        sql.AppendSemicolonIfMissing();
        return sql.ToString();
    }
    #endregion

    #region 转换Begin...End块
    /// <summary>
    /// 转换Begin...End块，默认实现是先拼接Begin,然后从tokens中取begin和end之间的语句，调用ConvertSingleSqlOrSqlBatch进行转换，最后拼接End
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    protected virtual string ConvertBeginEndBlockSql(IList<TSqlParserToken> tokens)
    {
        var sql = new StringBuilder();
        sql.Append("BEGIN");
        //tokens中第一个Token应该是Begin,最后一个Token应该是End,取出中间部分循环处理每条语句
        var sqlTokens = tokens.Skip(1).Take(tokens.Count - 2).ToList();
        var len = sqlTokens.Count;
        for (var i = 0; i < len;)
        {
            var sqlInBeginTokens = sqlTokens.GetFirstCompleteSqlTokens(ref i);
            sql.Append(ConvertSingleSqlOrSqlBatch(sqlInBeginTokens));
        }
        sql.Append($"{Environment.NewLine}END;{Environment.NewLine}");
        return sql.ToString();
    }
    #endregion

    #region 转换()块
    /// <summary>
    /// 转换()块，默认实现是先拼接(,然后从tokens中取(和)之间的语句，调用ConvertSingleSqlOrSqlBatch进行转换，最后拼接)
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    protected virtual string ConvertParenthesesBlockSql(IList<TSqlParserToken> tokens)
    {
        var sb = new StringBuilder();
        sb.Append("(");
        //tokens中第一个Token应该是(,最后一个Token应该是),取出中间部分循环处理每条语句
        var sqlTokens = tokens.Skip(1).Take(tokens.Count - 2).ToList();
        var len = sqlTokens.Count;
        for (var i = 0; i < len;)
        {
            var sqlInBeginTokens = sqlTokens.GetFirstCompleteSqlTokens(ref i);
            sb.Append(ConvertSingleSqlOrSqlBatch(sqlInBeginTokens));
        }
        sb.Append($"{Environment.NewLine}){Environment.NewLine}");
        return sb.ToString();
    }
    #endregion

    #region 转select语句
    protected virtual string ConvertSelectSql(IList<TSqlParserToken> tokens)
    {
        var sb = new StringBuilder();
        //完整的selct语句包含select,from,where等，而只有select部分中的name='value',name = a.columnName等需要处理,而from ,where等不需要处理
        var isInSelect = false;
        for (var i = 0; i < tokens.Count; i++)
        {
            var item = tokens[i];
            #region 处理是否在select语句中
            if (item.TokenType == TSqlTokenType.Select)
            {
                isInSelect = true;
            }
            else if (isInSelect && (item.TokenType == TSqlTokenType.From || item.TokenType == TSqlTokenType.Where || item.TokenType == TSqlTokenType.Group || item.TokenType == TSqlTokenType.Order || item.TokenType == TSqlTokenType.Option))
            {
                isInSelect = false;
            }
            #endregion
            #region 处理dbo.xxx,直接跳过dbo.,从xxx开始继续转换，因为postgresql不支持dbo架构
            //处理dbo.uf_getmask(...)函数，转换为uf_getmask(...)，但不能处理a.columnName这种，所以要求第一个的文本必须是dbo
            if ((item.TokenType == TSqlTokenType.Identifier || item.TokenType == TSqlTokenType.QuotedIdentifier)
                && item.Text.Equals("dbo", StringComparison.OrdinalIgnoreCase)
                 && tokens[i + 1].TokenType == TSqlTokenType.Dot)
            {
                //直接跳到.后面的下一个标识符
                i += 2;
                item = tokens[i];
            }
            #endregion
            #region 处理select 'Engineering' as 'Product'，即as别名也是字符串的情况
            //处理select 'Engineering' as 'Product'，即as别名也是字符串的情况
            if (item.TokenType == TSqlTokenType.As)
            {
                var nextTokens = tokens.Skip(i + 1).Take(5).ToList();
                var nextTokenType = nextTokens.GetFirstNotWhiteSpaceTokenType();
                if (nextTokenType == TSqlTokenType.AsciiStringLiteral)
                {
                    sb.Append(item.Text);
                    for (var j = 0; j < nextTokens.Count; j++)
                    {
                        if (nextTokens[j].TokenType == TSqlTokenType.AsciiStringLiteral)
                        {
                            item = nextTokens[j];
                            item.TokenType = TSqlTokenType.Identifier;
                            i = i + j + 1;
                            break;
                        }
                        sb.Append(nextTokens[j].Text);
                    }
                }
            }
            #endregion
            #region 处理所有Identifier,都更改为小写
            //处理所有Identifier,都更改为小写
            if (item.TokenType == TSqlTokenType.Identifier || item.TokenType == TSqlTokenType.QuotedIdentifier)
            {
                //判断是否是colName = 'value'这样的形式，如果是，则需要将等号改为as，并且将列名放到后面
                //同时需要妆容colName = a.columnName这样的形式
                var nextTokens = tokens.Skip(i + 1).ToList();
                var nextTokenType = nextTokens.GetFirstNotWhiteSpaceTokenType();
                if (nextTokenType == TSqlTokenType.EqualsSign && isInSelect)
                {
                    var valueTokenIndex = nextTokens.FindIndex(w => w.TokenType != TSqlTokenType.WhiteSpace && w.TokenType != TSqlTokenType.EqualsSign);
                    //检查valueTokenIndex下一项是否是.,如果是.，则表示是a.columnName这样的形式
                    if (valueTokenIndex + 1 < nextTokens.Count && nextTokens[valueTokenIndex + 1].TokenType == TSqlTokenType.Dot)
                    {
                        sb.Append($"{nextTokens[valueTokenIndex].Text}{nextTokens[valueTokenIndex + 1].Text}{nextTokens[valueTokenIndex + 2].Text} AS {item.Text.ToPostgreSqlIdentifier()}");
                        i += valueTokenIndex + 3;
                    }
                    //不是.，则表示是colName = 'value'这样的形式，直接取valueTokenIndex的值即可
                    else
                    {
                        sb.Append($"{nextTokens[valueTokenIndex].Text} AS {item.Text.ToPostgreSqlIdentifier()}");
                        i += valueTokenIndex + 1;
                    }
                }
                else
                {
                    sb.Append(item.Text.ToPostgreSqlIdentifier());
                }
                continue;
            }
            #endregion
            #region 处理类似convert(varchar(30) , 'gs') 的语句，转换为 CAST('gs' AS varchar(30))
            //处理类似convert(varchar(30) , 'gs') 的语句，转换为 CAST('gs' AS varchar(30))
            if (item.TokenType == TSqlTokenType.Convert)
            {
                sb.Append(tokens.GetConvertSql(ref i));
                continue;
            }
            #endregion

            //非特殊类型的，直接添加
            sb.Append(item.Text);
        }
        return sb.ToString();
    }
    #endregion

    #region 转换insert 语句
    /// <summary>
    /// 转换insert语句，默认实现是直接拼接Token文本，子类可以重写此方法以实现更复杂的转换逻辑
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    protected virtual string ConvertInsertSql(IList<TSqlParserToken> tokens)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < tokens.Count; i++)
        {
            var item = tokens[i];
            //处理dbo.name这样的标识符，去掉前面的dbo.
            if (item.TokenType == TSqlTokenType.AsciiStringOrQuotedIdentifier || item.TokenType == TSqlTokenType.QuotedIdentifier)
            {
                sb.Append(tokens.GetIdentityName(ref i));
                continue;
            }
            sb.Append(item.Text);
        }
        sb.AppendSemicolonIfMissing();
        return sb.ToString();
    }
    #endregion

    #region 处理if语句或语句块
    /// <summary>
    /// 转换if语句或语句块，默认实现是先取出if条件部分进行转换，然后取出if条件后的语句进行转换，最后添加end if;
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    protected virtual string ConvertIfBlockSql(IList<TSqlParserToken> tokens)
    {
        var sb = new StringBuilder();
        //其中取中的if条件语句进行转换
        var startIndex = 0;
        var ifConditionTokens = tokens.GetIfConditionOnly(ref startIndex);
        sb.AppendLine(ConvertIfConditionSqlOnly(ifConditionTokens));
        //取出if条件后的语句进行转换
        var sqlTokens = tokens.Skip(startIndex).ToList();
        //如果剩余语句块是begin ..end形式，则取出begin end之间的语句进行转换，因为if..begin..end已经在if条件部分处理过了
        var firstTokenType = sqlTokens.GetFirstNotWhiteSpaceTokenType();
        if (firstTokenType == TSqlTokenType.Begin)
        {
            var beginIndex = sqlTokens.FindIndex(t=>t.TokenType == TSqlTokenType.Begin);
            var innerTokens = sqlTokens.Skip(beginIndex+1).Take(sqlTokens.Count - beginIndex - 2).ToList();
            sb.AppendLine(ConvertSingleOrMultipleSqlOrSqlBatch(innerTokens));
        }
        else
        {
            sb.AppendLine(ConvertSingleSqlOrSqlBatch(sqlTokens));
        }
        //添加结束标志
        sb.AppendLine(" END IF;");
        return sb.ToString();
    }

    /// <summary>
    /// 转换if 条件
    /// </summary>
    /// <param name="ifConditionSql"></param>
    /// <returns></returns>
    protected virtual string ConvertIfConditionSqlOnly(IList<TSqlParserToken> tokens)
    {
        string tableName, columnName, indexName,codeValue;
        // 处理 IF NOT EXISTS(SELECT * FROM syscolumns WHERE ID = OBJECT_ID('HotelPos') AND name = 'Id') 类型的语句
        if(tokens.IsIfNotExistsFromSyscolumnsWhereIdAndName(out tableName,out columnName))
        {
            return $"IF NOT EXISTS ( SELECT 1 FROM information_schema.columns WHERE table_name = '{tableName}' AND column_name = '{columnName}') THEN ";
        }
        // 处理 if not exists(select * from INFORMATION_SCHEMA.columns where table_name='posSmMappingHid' and column_name = 'memberVersion') 
        if(tokens.IsIfNotExistsSelectFromInformationSchemaColumns(out tableName,out columnName))
        {
            return $"IF NOT EXISTS ( SELECT 1 FROM information_schema.columns WHERE table_name = '{tableName}' AND column_name = '{columnName}') THEN ";
        }
        //处理 IF OBJECT_ID('HuiYiMapping') IS NULL 这类语句
        if(tokens.IsIfObjectIdIsNull(out tableName))
        {
            return $"IF to_regclass('{tableName}') IS NULL THEN ";
        }
        // 处理 IF NOT EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'dbo.commonInvoiceInfo') AND type IN ('U'))
        if(tokens.IsIfNotExistsSelectFromSysAllObjectsWhereObjectIdAndTypeIn(out tableName))
        {
            return $"IF to_regclass('{tableName}') IS NULL THEN ";
        }
        //处理IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'TryHotelIdForGroup')  这类语句
        if(tokens.IsIfNotExistsSelectFromSysParaWhereCodeEqualCondition(out codeValue))
        {
            return $"IF NOT EXISTS ( SELECT 1 FROM syspara WHERE code = '{codeValue}') THEN ";
        }
        //处理 if not exists(select id from sysobjects where name = 'ImeiMappingHid') 这类语句
        if(tokens.IsIfNotExistsSelectFromSysobjectsWhereNameEqualCondition(out tableName))
        {
            return $"IF to_regclass('{tableName}') IS NULL THEN ";
        }
        // 处理 IF NOT EXISTS( SELECT * from sysobjects where name =( SELECT TOP 1 name FROM sys.indexes  WHERE is_primary_key = 1   AND object_id  = Object_Id('posSmMappingHid') AND name='PK_posSm_20190808912' ) )  这类语句
        if(tokens.IsIfNotExistsSysobjectsNameEqualNestedIndexSelect(out tableName,out indexName))
        {
            return $"IF NOT EXISTS ( select * from pg_class where relname = '{indexName}' and relkind = 'i') THEN ";
        }
        // 处理 IF EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('HotelUserWxInfo') AND name = 'NickName' AND length = 28)
        if(tokens.IsIfExistsFromSyscolumnsWhereIdNameAndLength(out tableName,out columnName,out int columnLength))
        {
            return $"IF EXISTS ( SELECT 1 FROM information_schema.columns WHERE table_name = '{tableName}' AND column_name = '{columnName}' AND character_maximum_length = {columnLength}) THEN ";
        }
        // 处理 IF NOT EXISTS (SELECT * FROM AuthButtons WHERE AuthButtonId='SetHotelLevel' AND AuthButtonValue='524288' AND Seqid='101') 
        if(tokens.IsIfNotExistsSelectFromAuthButtons(out string buttonId,out string buttonValue,out string sqlId))
        {
            return $"IF NOT EXISTS ( SELECT 1 FROM AuthButtons WHERE AuthButtonId = '{buttonId}' AND AuthButtonValue = '{buttonValue}' AND Seqid = '{sqlId}') THEN ";
        }
        /* 处理 if exists(select distinct * from (  
        select hotelCode as hid from dbo.posSmMappingHid
        union all
                                select groupid from dbo.posSmMappingHid)a
                                where ISNULL(a.hid, '') != '' and hid not in(select hid from dbo.hotelProducts where productCode = 'ipos'))  
        */
        if(tokens.IsIfExistsSelectDistinctFromUnionAllSubquery())
        {
            return @"IF EXISTS (
  SELECT 1 FROM (
    SELECT hotelCode AS hid FROM posSmMappingHid
    UNION ALL
    SELECT groupid AS hid FROM posSmMappingHid
  ) a
  WHERE COALESCE(a.hid, '') <> '' 
    AND a.hid NOT IN (
      SELECT hid FROM hotelProducts WHERE productCode = 'ipos'
    )
) THEN
";
        }

        //默认直接拼接
        return string.Concat(tokens.Select(w => w.Text));
    }
    #endregion

    #region 处理alter语句
    /// <summary>
    /// 处理alter语句，默认实现是直接拼接Token文本，子类可以重写此方法以实现更复杂的转换逻辑
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    protected virtual string ConvertAlterSql(IList<TSqlParserToken> tokens)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < tokens.Count; i++)
        {
            var item = tokens[i];
            //处理dbo.name这样的标识符，去掉前面的dbo.
            if (item.TokenType == TSqlTokenType.AsciiStringOrQuotedIdentifier || item.TokenType == TSqlTokenType.QuotedIdentifier)
            {
                sb.Append(tokens.GetIdentityName(ref i));
                continue;
            }
            //如果是标识符，则转换为小写
            if(item.TokenType == TSqlTokenType.Identifier)
            {
                sb.Append(item.Text.ToPostgreSqlIdentifier());
                continue;
            }
            sb.Append(item.Text);
        }
        sb.AppendSemicolonIfMissing();
        return sb.ToString();
    }
    #endregion
}
