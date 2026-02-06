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
        return GenerateSqlScript(fragment.ScriptTokenStream);
    }
    #region 转换单个或多个语句入口
    public virtual string GenerateSqlScript(IList<TSqlParserToken> sqlTokens,bool needAddSqlContentBeforeEndFile = true)
    {
        var sb = new StringBuilder();
        sb.Append(ConvertAllSqlAndSqlBatch(sqlTokens));
        if (needAddSqlContentBeforeEndFile)
        {
            sb.Append(GetSqlContentBeforeEndFile());
        }
        return sb.ToString();
    }
    #endregion
    #region 转换单个语句或语句块入口
    protected virtual string ConvertAllSqlAndSqlBatch(IList<TSqlParserToken> sqlTokens)
    {
        var sb = new StringBuilder();
        var len = sqlTokens.Count;
        for (var i = 0; i < len;)
        {
            var singleSqlTokens = sqlTokens.GetFirstCompleteSqlTokens(ref i);
            sb.Append(ConvertSingleCompleteSqlAndSqlBatch(singleSqlTokens));
        }
        return sb.ToString();
    }
    /// <summary>
    /// 转换单个语句或语句块，默认实现会根据语句的类型调用不同的方法进行处理，子类可以重写此方法以实现更复杂的转换逻辑
    /// </summary>
    /// <param name="sqlTokens"></param>
    /// <returns></returns>
    protected virtual string ConvertSingleCompleteSqlAndSqlBatch(IList<TSqlParserToken> sqlTokens)
    {
        var sqlTokenType = sqlTokens.GetFirstNotWhiteSpaceTokenType();
        //处理create...语句
        if (sqlTokenType == TSqlTokenType.Create)
        {
            return ConvertCreateSqlAndSqlBatch(sqlTokens);
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
            return ConvertDeleteSql(sqlTokens);
        }
        //处理drop语句
        if (sqlTokenType == TSqlTokenType.Drop)
        {
            return ConvertDropSql(sqlTokens);
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
        //处理update语句
        if(sqlTokenType == TSqlTokenType.Update)
        {
            return ConvertUpdateSql(sqlTokens);
        }
        //处理执行动态语句exec
        if(sqlTokenType == TSqlTokenType.Exec || sqlTokenType == TSqlTokenType.Execute)
        {
            return ConvertExecuteSql(sqlTokens);
        }
        //处理declare语句
        if(sqlTokenType == TSqlTokenType.Declare)
        {
            return ConvertDeclareSql(sqlTokens);
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
    protected virtual string ConvertCreateSqlAndSqlBatch(IList<TSqlParserToken> tokens)
    {
        var nextTokens = tokens.Skip(1).ToList();
        var nextTokenType = nextTokens.GetFirstNotWhiteSpaceTokenType();
        //如果是create table语句，则进行特殊处理
        if (nextTokenType == TSqlTokenType.Table)
        {
            return ConvertCreateTableSql(tokens);
        }
        //如果是create index语句，则进行特殊处理（包括 NONCLUSTERED INDEX）
        if (nextTokenType == TSqlTokenType.Index)
        {
            return ConvertCreateIndexSql(tokens);
        }
        //检查是否是 CREATE NONCLUSTERED INDEX 的情况
        //检查第一个非空token的文本是否为 NONCLUSTERED
        var firstNonWhiteSpaceIndex = nextTokens.FindIndex(t => t.TokenType != TSqlTokenType.WhiteSpace);
        if (firstNonWhiteSpaceIndex >= 0 && nextTokens[firstNonWhiteSpaceIndex].Text.Trim().Equals("NONCLUSTERED", StringComparison.OrdinalIgnoreCase))
        {
            //跳过 NONCLUSTERED，检查下一个是否是 INDEX
            var afterNonClustered = nextTokens.Skip(firstNonWhiteSpaceIndex + 1).ToList();
            if (afterNonClustered.GetFirstNotWhiteSpaceTokenType() == TSqlTokenType.Index)
            {
                return ConvertCreateIndexSql(tokens);
            }
        }

        //如果是create ... as语句，则分两部分进行处理
        //1 create... as 本身
        //2 as后面的所有语句
        if (nextTokenType == TSqlTokenType.View || nextTokenType == TSqlTokenType.Proc || nextTokenType == TSqlTokenType.Procedure || nextTokenType == TSqlTokenType.Function)
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
            sb.Append(ConvertCreateAsOnly(createAsTokens));
            sb.Append(ConvertAllSqlAndSqlBatch(afterAsTokens));
            return sb.ToString();
        }
        //如果是其他情况，则直接拼接
        return string.Concat(tokens.Select(w => w.Text));
    }
    /// <summary>
    /// 转换单个Create Table语句，包含表名称和列名称以及列类型的转换
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    private string ConvertCreateTableSql(IList<TSqlParserToken> tokens)
    {
        //获取表名称
        var tableKeywordIdx = tokens.FindFirstIndex(t => t.TokenType == TSqlTokenType.Table);
        var leftParenIdx = tokens.FindFirstIndex(t => t.TokenType == TSqlTokenType.LeftParenthesis);
        var tableNameText = string.Concat(tokens.Skip(tableKeywordIdx + 1).Take(leftParenIdx - tableKeywordIdx - 1).Select(t => t.Text)).ToPostgreSqlIdentifier();
        //获取（）中间的列定义部分
        var lastRightParenIdx = tokens.FindLastIndex(t => t.TokenType == TSqlTokenType.RightParenthesis);
        var colTokens = tokens.Skip(leftParenIdx + 1).Take(lastRightParenIdx - leftParenIdx - 1).ToList();
        var columns = new List<string>();
        var curr = new List<TSqlParserToken>();
        int pdepth = 0;
        // iterate with index to allow lookahead for comments that follow a comma
        for (int idx = 0; idx < colTokens.Count; idx++)
        {
            var tk = colTokens[idx];
            if (tk.TokenType == TSqlTokenType.LeftParenthesis) { pdepth++; curr.Add(tk); continue; }
            if (tk.TokenType == TSqlTokenType.RightParenthesis) { pdepth--; curr.Add(tk); continue; }
            if (tk.TokenType == TSqlTokenType.Comma && pdepth == 0)
            {
                // lookahead: if comma is followed (optionally with whitespace) by a single-line or multi-line comment,
                // treat that comment as belonging to the previous column (attach to curr) instead of starting a new column
                int j = idx + 1;
                // skip white space tokens
                while (j < colTokens.Count && colTokens[j].TokenType == TSqlTokenType.WhiteSpace) j++;
                if (j < colTokens.Count && (colTokens[j].TokenType == TSqlTokenType.SingleLineComment || colTokens[j].TokenType == TSqlTokenType.MultilineComment))
                {
                    // attach the comment and any following whitespace to current column
                    curr.Add(colTokens[j]);
                    j++;
                    while (j < colTokens.Count && colTokens[j].TokenType == TSqlTokenType.WhiteSpace)
                    {
                        curr.Add(colTokens[j]);
                        j++;
                    }
                    // advance main index to the last consumed token
                    idx = j -1; // skip the comment tokens we just consumed
                    // finalize current column (do not include the comma itself)
                    if (curr.Count > 0)
                    {
                        columns.Add(ConvertCreateTableColumnTokensToDefinition(curr));
                        curr.Clear();
                    }
                    continue;
                }

                // normal comma: finish current column
                if (curr.Count > 0)
                {
                    columns.Add(ConvertCreateTableColumnTokensToDefinition(curr));
                    curr.Clear();
                }
                continue;
            }
            curr.Add(tk);
        }
        if (curr.Count > 0 && curr.Any(t => t.TokenType != TSqlTokenType.WhiteSpace))
        {
            columns.Add(ConvertCreateTableColumnTokensToDefinition(curr));
            curr.Clear();
        }

        //组装Create Table语句
        var sb = new StringBuilder();
        sb.AppendLine($"CREATE TABLE {tableNameText} (");
        for (int i = 0; i < columns.Count; i++)
        {
            var splitChar = i < columns.Count - 1 ? "," : "";

            var line = columns[i].Trim();
            //如果line包含--注释，则分隔符放在注释前面
            var commentIdx = line.IndexOf("--");
            if (commentIdx >= 0)
            {
                var codePart = line.Substring(0, commentIdx).TrimEnd();
                var commentPart = line.Substring(commentIdx).TrimEnd();
                line = $"{codePart}{splitChar} {commentPart}";
                sb.AppendLine($"        {line}");
                continue;
            }
            else
            {
                sb.AppendLine($"        {line}{splitChar}");
            }
        }
        sb.Append(");");
        return sb.ToString();
    }
    /// <summary>
    /// 处理列名称和列类型的转换
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    private string ConvertCreateTableColumnTokensToDefinition(IList<TSqlParserToken> tokens)
    {
        var sb = new StringBuilder();
        var hasColumnName = false;
        for (int i = 0; i < tokens.Count; i++)
        {
            var tk = tokens[i];
            if (tk.TokenType == TSqlTokenType.Identifier || tk.TokenType == TSqlTokenType.QuotedIdentifier || tk.TokenType == TSqlTokenType.AsciiStringOrQuotedIdentifier)
            {
                var tkText = tk.Text.ToPostgreSqlIdentifier();
                //列名和列类型都是相同的类型，第一个是列名，第二个是列类型
                if (!hasColumnName)
                {
                    hasColumnName = true;
                    sb.Append(tkText);
                    continue;
                }
                else
                {
                    //已经有列名的情况下，第二个标识符是列类型，需要进行类型转换
                    var dataType = MigrationUtils.ConvertToPostgresType(tkText, null);

                    //数据类型后面，还有三种情况，not null / identity(1,1) / primary key
                    //先处理特殊数据类型，int,bigint,需要判断后面是否有identity关键字，有的话，则需要转换为serial,bigserial
                    #region 先处理特殊数据类型，int,bigint,需要判断后面是否有identity关键字，有的话，则需要转换为serial,bigserial
                    if (tkText.Equals("int", StringComparison.OrdinalIgnoreCase))
                    {
                        var indexForIdentity = tokens.FindFirstIndex(t => t.TokenType == TSqlTokenType.Identity, i + 1);
                        if (indexForIdentity > 0)
                        {
                            dataType = "serial";
                        }
                    }
                    //处理特殊情况，如果是bigint IDENTITY(1,1)这种情况，则转换为bigserial
                    if (tkText.Equals("bigint", StringComparison.OrdinalIgnoreCase))
                    {
                        var indexForIdentity = tokens.FindFirstIndex(t => t.TokenType == TSqlTokenType.Identity, i + 1);
                        if (indexForIdentity > 0)
                        {
                            dataType = "bigserial";
                        }
                    }
                    #endregion
                    sb.Append(dataType);
                    #region 处理数据类型精度，如varchar(30),decimal(18,2)等
                    if (i + 1 < tokens.Count && tokens[i + 1].TokenType == TSqlTokenType.LeftParenthesis)
                    {
                        for (var j = i + 1; j < tokens.Count; j++)
                        {
                            sb.Append(tokens[j].Text);
                            //如果遇到右括号，则结束
                            if (tokens[j].TokenType == TSqlTokenType.RightParenthesis)
                            {
                                break;
                            }
                        }
                    }
                    #endregion
                    //再处理not null情况
                    #region 再处理not null情况
                    var indexForNot = tokens.FindFirstIndex(t => t.TokenType == TSqlTokenType.Not, i + 1);
                    if(indexForNot > 0)
                    {
                        var indexForNull = tokens.FindFirstIndex(t => t.TokenType == TSqlTokenType.Null, indexForNot + 1);
                        if(indexForNull > 0)
                        {
                            sb.Append(" NOT NULL");
                        }
                    }
                    else
                    {
                        sb.Append(" NULL");
                    }
                    #endregion
                    //再处理primary key情况,注意，如果是primary key (id)这种情况，是单独一行处理的，这里不处理
                    #region 再处理primary key情况
                    var indexForPrimary = tokens.FindFirstIndex(t => t.TokenType == TSqlTokenType.Primary, i + 1);
                    if (indexForPrimary > 0)
                    {
                        var indexForKey = tokens.FindFirstIndex(t => t.TokenType == TSqlTokenType.Key, indexForPrimary + 1);
                        if (indexForKey > 0)
                        {
                            var nextTokenType = tokens.GetFirstNotWhiteSpaceTokenTypeFromIndex(indexForKey + 1);
                            //如果下一个非空白TokenType是左括号或CLUSTERED，则表示是primary key (id)这种情况,交由外层进行处理
                            if (nextTokenType == TSqlTokenType.LeftParenthesis || nextTokenType == TSqlTokenType.Clustered)
                            {
                                //如果是在列的语句中包含primary key (id)这种情况的话，则说明是由于书写不规范，最后一列没有加,分隔，所以把后续的primary key认为是同一行语句了
                                //此时需要添加逗号和换行，然后跳过primary key部分
                                sb.AppendIfMissing(',');
                                sb.AppendLine();
                                //注意：返回的索引值需要回到primary关键字前面，因为外层循环会i++
                                i = indexForPrimary - 1;
                                continue;
                            }
                            sb.Append(" PRIMARY KEY");
                        }
                    }
                    #endregion
                    //最后处理列的单行注释说明，如--这是主键ID
                    var indexForSingleLineComment = tokens.FindFirstIndex(t => t.TokenType == TSqlTokenType.SingleLineComment, i + 1);
                    if (indexForSingleLineComment > 0)
                    {
                        sb.Append(tokens[indexForSingleLineComment].Text);
                    }
                    //本行已经处理完毕，跳出循环
                    break;
                }
            }
            //处理constraint pk_abc primary key这种情况，跳过constraint和名称，直接处理primary key
            else if (tk.TokenType == TSqlTokenType.Constraint)
            {
                //跳过constraint名称部分
                var j = i + 1;
                for (; j < tokens.Count; j++)
                {
                    if (tokens[j].TokenType == TSqlTokenType.Primary)
                    {
                        i = j - 1; //外层循环会i++，所以这里减1
                        break;
                    }
                }
                continue;
            }
            //处理primary key (id)情况，需要单独一行
            //同时处理id iniqueidentifier not null primary key情况，直接跟在原有列定义后面
            else if (tk.TokenType == TSqlTokenType.Primary)
            {
                var j = i;
                var hasKey = false;
                var hasLeftParenthesis = false;
                for (; j < tokens.Count && !hasLeftParenthesis; j++)
                {
                    if (tokens[j].TokenType == TSqlTokenType.Key)
                    {
                        hasKey = true;
                        //根据后面是否有(来区分不同的方式，进行不同的处理
                        var indexForLeftParenthesis = tokens.FindFirstIndex(w=>w.TokenType == TSqlTokenType.LeftParenthesis,j+1);
                        if(indexForLeftParenthesis > 0)
                        {
                            //处理primary key (id)情况
                            sb.Append(" PRIMARY KEY (");
                            //继续添加（）中的列名称
                            for (j = indexForLeftParenthesis; j < tokens.Count; j++)
                            {
                                if (tokens[j].TokenType == TSqlTokenType.RightParenthesis)
                                {
                                    sb.Append(")");
                                    break;
                                }
                                if (tokens[j].TokenType == TSqlTokenType.Identifier || tokens[j].TokenType == TSqlTokenType.QuotedIdentifier)
                                {
                                    sb.Append(tokens[j].Text.ToPostgreSqlIdentifier());
                                }
                                if (tokens[j].TokenType == TSqlTokenType.Comma)
                                {
                                    sb.Append(",");
                                }
                            }
                        }
                        else
                        {
                            //处理id iniqueidentifier not null primary key情况，直接跟在原有列定义后面
                            sb.Append("primary key");
                        }

                        continue;
                    }
                    //遇到行内注释则退出primary key处理
                    if (tokens[j].TokenType == TSqlTokenType.SingleLineComment)
                    {
                        sb.Append(tokens[j].Text);
                        break;
                    }
                }
                if (hasKey)
                {
                    i = j;
                    break;
                }
                continue;
            }
            sb.Append(tk.Text);
        }
        //语句末尾可能会有逗号，去掉，由外层根据列是否最后一列来决定是否添加逗号
        return sb.ToString().Trim().TrimEnd(',');
    }
    /// <summary>
    /// 转换单个Create Index语句
    /// 只需要处理identity名称和表名称的转换即可
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    private string ConvertCreateIndexSql(IList<TSqlParserToken> tokens)
    {
        var sb = new StringBuilder();
        var lastTokenWasWhiteSpace = false;
        foreach(var tk in tokens)
        {
            if(tk.TokenType == TSqlTokenType.Identifier)
            {
                sb.Append(tk.Text.ToPostgreSqlIdentifier());
                lastTokenWasWhiteSpace = false;
                continue;
            }
            //如果是换行符，则忽略
            if(tk.TokenType == TSqlTokenType.WhiteSpace && tk.Text == "\r\n")
            {
                continue;
            }
            //如果是 NONCLUSTERED 关键字，则忽略（PostgreSQL 不支持）
            if(!string.IsNullOrEmpty(tk.Text) && tk.Text.Trim().Equals("NONCLUSTERED", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            //跳过连续的空格
            if(tk.TokenType == TSqlTokenType.WhiteSpace && lastTokenWasWhiteSpace)
            {
                continue;
            }
            sb.Append(tk.Text);
            lastTokenWasWhiteSpace = tk.TokenType == TSqlTokenType.WhiteSpace;
        }
        sb.AppendIfMissing(';');
        return sb.ToString();
    }
    /// <summary>
    /// 转换单个Create ... As语句，默认实现是直接拼接Token文本，子类可以重写此方法以实现更复杂的转换逻辑
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    protected virtual string ConvertCreateAsOnly(IList<TSqlParserToken> tokens)
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
    protected virtual string ConvertDeleteSql(IList<TSqlParserToken> sqlTokens)
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
            //处理临时表（以 # 开头的标识符）
            if (item.TokenType == TSqlTokenType.Identifier && item.Text.StartsWith("#"))
            {
                sql.Append(item.Text.ToPostgreSqlIdentifier());
                continue;
            }
            //处理引号标识符（可能是 dbo.table 格式）
            if (item.TokenType == TSqlTokenType.AsciiStringOrQuotedIdentifier || item.TokenType == TSqlTokenType.QuotedIdentifier)
            {
                sql.Append(item.Text.ToPostgreSqlIdentifier());
                continue;
            }
            sql.Append(item.Text);
        }
        sql.AppendIfMissing(';');
        return sql.ToString();
    }
    #endregion

    #region 转换Drop语句
    /// <summary>
    /// 转换DROP语句，处理标识符转换（包括临时表）
    /// </summary>
    /// <param name="sqlTokens"></param>
    /// <returns></returns>
    protected virtual string ConvertDropSql(IList<TSqlParserToken> sqlTokens)
    {
        var sql = new StringBuilder();
        for (var i = 0; i < sqlTokens.Count; i++)
        {
            var item = sqlTokens[i];
            //处理临时表（以 # 开头的标识符）
            if (item.TokenType == TSqlTokenType.Identifier && item.Text.StartsWith("#"))
            {
                sql.Append(item.Text.ToPostgreSqlIdentifier());
                continue;
            }
            //处理引号标识符
            if (item.TokenType == TSqlTokenType.AsciiStringOrQuotedIdentifier || item.TokenType == TSqlTokenType.QuotedIdentifier)
            {
                sql.Append(item.Text.ToPostgreSqlIdentifier());
                continue;
            }
            sql.Append(item.Text);
        }
        sql.AppendIfMissing(';');
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
            sql.Append(ConvertSingleCompleteSqlAndSqlBatch(sqlInBeginTokens));
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
        var lastIndex = tokens.FindLastIndex(w => w.TokenType == TSqlTokenType.RightParenthesis);
        //tokens中第一个Token应该是(,最后一个Token应该是),取出中间部分循环处理每条语句
        var sqlTokens = tokens.Skip(1).Take(lastIndex - 1).ToList();
        var len = sqlTokens.Count;
        for (var i = 0; i < len;)
        {
            var sqlInBeginTokens = sqlTokens.GetFirstCompleteSqlTokens(ref i);
            sb.Append(ConvertSingleCompleteSqlAndSqlBatch(sqlInBeginTokens));
        }
        sb.Append($"{Environment.NewLine}){Environment.NewLine}");
        return sb.ToString();
    }
    #endregion

    #region 转select语句
    protected virtual string ConvertSelectSql(IList<TSqlParserToken> tokens)
    {
        var sb = new StringBuilder();

        // 检查是否有 INTO 子句（SELECT ... INTO table FROM ...）
        // 这种语法在 SQL Server 中创建表，在 PostgreSQL 中需要转换为 CREATE TEMP TABLE ... AS SELECT ...
        var intoIndex = -1;
        for (var i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].TokenType == TSqlTokenType.Into)
            {
                intoIndex = i;
                break;
            }
        }
        if (intoIndex > 0)
        {
            return ConvertSelectIntoSql(tokens, intoIndex);
        }

        // 检查是否有 TOP 子句（SELECT TOP n ...）
        // PostgreSQL 不支持 TOP，需要转换为 LIMIT
        var topValue = "";
        var skipIndices = new HashSet<int>(); // 记录需要跳过的 token 索引

        for (var i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].TokenType == TSqlTokenType.Top)
            {
                skipIndices.Add(i); // 跳过 TOP
                // 获取 TOP 后面的数值
                var nextIndex = i + 1;
                while (nextIndex < tokens.Count && tokens[nextIndex].TokenType == TSqlTokenType.WhiteSpace)
                {
                    skipIndices.Add(nextIndex); // 跳过空格
                    nextIndex++;
                }
                if (nextIndex < tokens.Count)
                {
                    if (tokens[nextIndex].TokenType == TSqlTokenType.Integer || tokens[nextIndex].TokenType == TSqlTokenType.Numeric)
                    {
                        topValue = tokens[nextIndex].Text;
                        skipIndices.Add(nextIndex); // 跳过数值
                    }
                    else if (tokens[nextIndex].TokenType == TSqlTokenType.LeftParenthesis)
                    {
                        // TOP (expression) 形式，跳到对应的右括号
                        var parenCount = 1;
                        var exprStart = nextIndex + 1;
                        skipIndices.Add(nextIndex); // 跳过左括号
                        nextIndex++;
                        while (nextIndex < tokens.Count && parenCount > 0)
                        {
                            skipIndices.Add(nextIndex);
                            if (tokens[nextIndex].TokenType == TSqlTokenType.LeftParenthesis) parenCount++;
                            else if (tokens[nextIndex].TokenType == TSqlTokenType.RightParenthesis) parenCount--;
                            nextIndex++;
                        }
                        // 提取表达式
                        var exprTokens = tokens.Skip(exprStart).Take(nextIndex - exprStart - 1).ToList();
                        var expr = string.Concat(exprTokens.Select(t => t.Text));
                        topValue = $"({expr})";
                    }
                }
                break;
            }
        }

        //完整的selct语句包含select,from,where等，而只有select部分中的name='value',name = a.columnName等需要处理,而from ,where等不需要处理
        var isInSelect = false;
        for (var i = 0; i < tokens.Count; i++)
        {
            // 跳过 TOP 关键字和其后的数值
            if (skipIndices.Contains(i))
            {
                continue;
            }

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

                // 判断是否是 ISNULL 函数，需要转换为 coalesce
                if (item.Text.Equals("isnull", StringComparison.OrdinalIgnoreCase) && nextTokenType == TSqlTokenType.LeftParenthesis)
                {
                    sb.Append("coalesce");
                    continue;
                }

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
                sb.Append(tokens.GetConvertSql(ref i, this));
                continue;
            }
            #endregion
            #region 处理+号的情况，只有前后是字符串时才转换为||，否则保留+
            if (item.TokenType == TSqlTokenType.Plus)
            {
                // 跳过前面的空白
                int prevIdx = i - 1;
                while (prevIdx >= 0 && tokens[prevIdx].TokenType == TSqlTokenType.WhiteSpace) prevIdx--;
                var prev = prevIdx >= 0 ? tokens[prevIdx] : null;

                // 跳过后面的空白
                int nextIdx = i + 1;
                while (nextIdx < tokens.Count && tokens[nextIdx].TokenType == TSqlTokenType.WhiteSpace) nextIdx++;
                var next = nextIdx < tokens.Count ? tokens[nextIdx] : null;

                bool prevIsString = prev != null && (prev.TokenType == TSqlTokenType.AsciiStringLiteral);
                bool nextIsString = next != null && (next.TokenType == TSqlTokenType.AsciiStringLiteral);

                if (prevIsString || nextIsString)
                    sb.Append("||");
                else
                    sb.Append("+");
                continue;
            }
            #endregion
            //非特殊类型的，直接添加
            // 避免连续多个空格
            if (item.TokenType == TSqlTokenType.WhiteSpace)
            {
                // 检查前一个添加的字符是否是空格
                if (sb.Length > 0 && sb[sb.Length - 1] == ' ')
                {
                    continue; // 跳过多余的空格
                }
                sb.Append(item.Text);
            }
            else
            {
                sb.Append(item.Text);
            }
        }

        // 如果有 TOP 子句，在末尾添加 LIMIT
        if (!string.IsNullOrEmpty(topValue))
        {
            sb.AppendIfMissing(';');
            var result = sb.ToString();
            // 在分号之前插入 LIMIT
            var lastSemicolonIndex = result.LastIndexOf(';');
            if (lastSemicolonIndex > 0)
            {
                result = result.Insert(lastSemicolonIndex, $" LIMIT {topValue}");
            }
            else
            {
                result = result.TrimEnd(';') + $" LIMIT {topValue};";
            }
            return result;
        }

        sb.AppendIfMissing(';');
        return sb.ToString();
    }

    /// <summary>
    /// 处理 SELECT ... INTO table 语句，转换为 CREATE TEMP TABLE ... AS SELECT ...
    /// </summary>
    /// <param name="tokens">原始 tokens</param>
    /// <param name="intoIndex">INTO 关键字的索引</param>
    /// <returns>转换后的 SQL</returns>
    protected virtual string ConvertSelectIntoSql(IList<TSqlParserToken> tokens, int intoIndex)
    {
        var sb = new StringBuilder();

        // 检查是否有 TOP 子句（SELECT TOP n ... INTO）
        var topValue = "";
        var skipIndices = new HashSet<int>();

        for (var i = 0; i < intoIndex; i++)
        {
            if (tokens[i].TokenType == TSqlTokenType.Top)
            {
                skipIndices.Add(i);
                var nextIndex = i + 1;
                while (nextIndex < intoIndex && tokens[nextIndex].TokenType == TSqlTokenType.WhiteSpace)
                {
                    skipIndices.Add(nextIndex);
                    nextIndex++;
                }
                if (nextIndex < intoIndex)
                {
                    if (tokens[nextIndex].TokenType == TSqlTokenType.Integer || tokens[nextIndex].TokenType == TSqlTokenType.Numeric)
                    {
                        topValue = tokens[nextIndex].Text;
                        skipIndices.Add(nextIndex);
                    }
                    else if (tokens[nextIndex].TokenType == TSqlTokenType.LeftParenthesis)
                    {
                        var parenCount = 1;
                        var exprStart = nextIndex + 1;
                        skipIndices.Add(nextIndex);
                        nextIndex++;
                        while (nextIndex < intoIndex && parenCount > 0)
                        {
                            skipIndices.Add(nextIndex);
                            if (tokens[nextIndex].TokenType == TSqlTokenType.LeftParenthesis) parenCount++;
                            else if (tokens[nextIndex].TokenType == TSqlTokenType.RightParenthesis) parenCount--;
                            nextIndex++;
                        }
                        var exprTokens = tokens.Skip(exprStart).Take(nextIndex - exprStart - 1).ToList();
                        var expr = string.Concat(exprTokens.Select(t => t.Text));
                        topValue = $"({expr})";
                    }
                }
                break;
            }
        }

        // 1. 首先查找表名
        var tableNameIndex = -1;
        for (var i = intoIndex + 1; i < tokens.Count; i++)
        {
            if (tokens[i].TokenType == TSqlTokenType.Identifier || tokens[i].TokenType == TSqlTokenType.QuotedIdentifier)
            {
                tableNameIndex = i;
                break;
            }
        }

        if (tableNameIndex < 0)
        {
            // 如果找不到表名，返回空字符串
            return string.Empty;
        }

        var tableName = tokens[tableNameIndex].Text.ToPostgreSqlIdentifier();

        // 2. 添加 DROP TABLE IF EXISTS（避免重复创建错误）
        sb.AppendLine($"DROP TABLE IF EXISTS {tableName};");

        // 3. 添加 CREATE TEMP TABLE
        sb.Append("CREATE TEMP TABLE ");
        sb.Append(tableName);

        // 4. 添加 AS
        sb.Append(" AS ");

        // 5. 添加 SELECT 部分（从开始到 INTO 之前）
        for (var i = 0; i < intoIndex; i++)
        {
            // 跳过 TOP 和相关 token
            if (skipIndices.Contains(i))
            {
                continue;
            }

            var item = tokens[i];

            #region 处理dbo.xxx,直接跳过dbo.,从xxx开始继续转换
            if ((item.TokenType == TSqlTokenType.Identifier || item.TokenType == TSqlTokenType.QuotedIdentifier)
                && item.Text.Equals("dbo", StringComparison.OrdinalIgnoreCase)
                 && tokens[i + 1].TokenType == TSqlTokenType.Dot)
            {
                i += 2;
                item = tokens[i];
            }
            #endregion

            #region 处理所有Identifier,都更改为小写
            if (item.TokenType == TSqlTokenType.Identifier || item.TokenType == TSqlTokenType.QuotedIdentifier)
            {
                var nextTokens = tokens.Skip(i + 1).Take(intoIndex - i - 1).ToList();
                var nextTokenType = nextTokens.GetFirstNotWhiteSpaceTokenType();
                if (nextTokenType == TSqlTokenType.EqualsSign)
                {
                    var valueTokenIndex = nextTokens.FindIndex(w => w.TokenType != TSqlTokenType.WhiteSpace && w.TokenType != TSqlTokenType.EqualsSign);
                    if (valueTokenIndex + 1 < nextTokens.Count && nextTokens[valueTokenIndex + 1].TokenType == TSqlTokenType.Dot)
                    {
                        sb.Append($"{nextTokens[valueTokenIndex].Text}{nextTokens[valueTokenIndex + 1].Text}{nextTokens[valueTokenIndex + 2].Text} AS {item.Text.ToPostgreSqlIdentifier()}");
                        i += valueTokenIndex + 3;
                        continue;
                    }
                    else
                    {
                        sb.Append($"{nextTokens[valueTokenIndex].Text} AS {item.Text.ToPostgreSqlIdentifier()}");
                        i += valueTokenIndex + 1;
                        continue;
                    }
                }
                else
                {
                    sb.Append(item.Text.ToPostgreSqlIdentifier());
                    continue;
                }
            }
            #endregion

            #region 处理convert函数
            if (item.TokenType == TSqlTokenType.Convert)
            {
                var tempIndex = i;
                sb.Append(tokens.GetConvertSql(ref tempIndex, this));
                i = tempIndex;
                continue;
            }
            #endregion

            // 避免连续多个空格
            if (item.TokenType == TSqlTokenType.WhiteSpace)
            {
                if (sb.Length > 0 && sb[sb.Length - 1] == ' ')
                {
                    continue;
                }
                sb.Append(item.Text);
            }
            else
            {
                sb.Append(item.Text);
            }
        }

        // 6. 添加 FROM 及之后的子句（跳过 INTO 和表名）
        var fromIndex = -1;
        for (var i = intoIndex; i < tokens.Count; i++)
        {
            if (tokens[i].TokenType == TSqlTokenType.From)
            {
                fromIndex = i;
                break;
            }
        }
        if (fromIndex > 0)
        {
            for (var i = fromIndex; i < tokens.Count; i++)
            {
                var item = tokens[i];

                #region 处理dbo.xxx,直接跳过dbo.
                if ((item.TokenType == TSqlTokenType.Identifier || item.TokenType == TSqlTokenType.QuotedIdentifier)
                    && item.Text.Equals("dbo", StringComparison.OrdinalIgnoreCase)
                     && i + 1 < tokens.Count && tokens[i + 1].TokenType == TSqlTokenType.Dot)
                {
                    i += 2;
                    if (i < tokens.Count)
                    {
                        item = tokens[i];
                        sb.Append(item.Text.ToPostgreSqlIdentifier());
                    }
                    continue;
                }
                #endregion

                #region 处理标识符
                if (item.TokenType == TSqlTokenType.Identifier || item.TokenType == TSqlTokenType.QuotedIdentifier)
                {
                    // 判断是否是 ISNULL 函数，需要转换为 coalesce
                    if (item.Text.Equals("isnull", StringComparison.OrdinalIgnoreCase))
                    {
                        var nextTokens = tokens.Skip(i + 1).ToList();
                        var nextTokenType = nextTokens.GetFirstNotWhiteSpaceTokenType();
                        if (nextTokenType == TSqlTokenType.LeftParenthesis)
                        {
                            sb.Append("coalesce");
                            continue;
                        }
                    }
                    sb.Append(item.Text.ToPostgreSqlIdentifier());
                    continue;
                }
                #endregion

                sb.Append(item.Text);
            }
        }

        // 添加 LIMIT（如果有 TOP 子句）
        if (!string.IsNullOrEmpty(topValue))
        {
            sb.Append($" LIMIT {topValue}");
        }

        sb.Append(";");
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

        // 检查是否有 TOP 子句（INSERT INTO ... SELECT TOP n ...）
        // PostgreSQL 不支持 TOP，需要转换为 LIMIT
        var topValue = "";
        var skipIndices = new HashSet<int>(); // 记录需要跳过的 token 索引

        for (var i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].TokenType == TSqlTokenType.Top)
            {
                skipIndices.Add(i); // 跳过 TOP
                // 获取 TOP 后面的数值
                var nextIndex = i + 1;
                while (nextIndex < tokens.Count && tokens[nextIndex].TokenType == TSqlTokenType.WhiteSpace)
                {
                    skipIndices.Add(nextIndex); // 跳过空格
                    nextIndex++;
                }
                if (nextIndex < tokens.Count)
                {
                    if (tokens[nextIndex].TokenType == TSqlTokenType.Integer || tokens[nextIndex].TokenType == TSqlTokenType.Numeric)
                    {
                        topValue = tokens[nextIndex].Text;
                        skipIndices.Add(nextIndex); // 跳过数值
                    }
                    else if (tokens[nextIndex].TokenType == TSqlTokenType.LeftParenthesis)
                    {
                        // TOP (expression) 形式，跳到对应的右括号
                        var parenCount = 1;
                        var exprStart = nextIndex + 1;
                        skipIndices.Add(nextIndex); // 跳过左括号
                        nextIndex++;
                        while (nextIndex < tokens.Count && parenCount > 0)
                        {
                            skipIndices.Add(nextIndex);
                            if (tokens[nextIndex].TokenType == TSqlTokenType.LeftParenthesis) parenCount++;
                            else if (tokens[nextIndex].TokenType == TSqlTokenType.RightParenthesis) parenCount--;
                            nextIndex++;
                        }
                        // 提取表达式
                        var exprTokens = tokens.Skip(exprStart).Take(nextIndex - exprStart - 1).ToList();
                        var expr = string.Concat(exprTokens.Select(t => t.Text));
                        topValue = $"({expr})";
                    }
                }
                break;
            }
        }

        for (var i = 0; i < tokens.Count; i++)
        {
            // 跳过 TOP 关键字和其后的数值
            if (skipIndices.Contains(i))
            {
                continue;
            }

            var item = tokens[i];
            //处理dbo.name这样的标识符，去掉前面的dbo.
            if (item.TokenType == TSqlTokenType.AsciiStringOrQuotedIdentifier || item.TokenType == TSqlTokenType.QuotedIdentifier)
            {
                sb.Append(tokens.GetIdentityName(ref i));
                continue;
            }
            //处理临时表（以 # 开头的标识符）
            if (item.TokenType == TSqlTokenType.Identifier && item.Text.StartsWith("#"))
            {
                sb.Append(item.Text.ToPostgreSqlIdentifier());
                continue;
            }
            sb.Append(item.Text);
        }

        // 如果有 TOP 子句，在末尾添加 LIMIT
        if (!string.IsNullOrEmpty(topValue))
        {
            sb.AppendIfMissing(';');
            var result = sb.ToString();
            // 在分号之前插入 LIMIT
            var lastSemicolonIndex = result.LastIndexOf(';');
            if (lastSemicolonIndex > 0)
            {
                result = result.Insert(lastSemicolonIndex, $" LIMIT {topValue}");
            }
            else
            {
                result = result.TrimEnd(';') + $" LIMIT {topValue};";
            }
            return result;
        }

        sb.AppendIfMissing(';');
        return sb.ToString();
    }
    #endregion

    #region 转换update语句
    protected virtual string ConvertUpdateSql(IList<TSqlParserToken> tokens)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < tokens.Count; i++)
        {
            var item = tokens[i];
            //处理dbo.name这样的标识符，去掉前面的dbo.
            if (item.TokenType == TSqlTokenType.Identifier || item.TokenType == TSqlTokenType.AsciiStringOrQuotedIdentifier || item.TokenType == TSqlTokenType.QuotedIdentifier)
            {
                sb.Append(tokens.GetIdentityName(ref i));
                continue;
            }
            sb.Append(item.Text);
        }
        sb.AppendIfMissing(';');
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
        sb.AppendLine(ConvertIfConditionSqlOnly(ifConditionTokens,out var needConvertIfContent));
        if (needConvertIfContent)
        {
            //取出if条件后的语句进行转换
            var sqlTokens = tokens.Skip(startIndex).ToList();
            //如果剩余语句块是begin ..end形式，则取出begin end之间的语句进行转换，因为if..begin..end已经在if条件部分处理过了
            var firstTokenType = sqlTokens.GetFirstNotWhiteSpaceTokenType();
            if (firstTokenType == TSqlTokenType.Begin)
            {
                var beginIndex = sqlTokens.FindIndex(t => t.TokenType == TSqlTokenType.Begin);
                var innerTokens = sqlTokens.Skip(beginIndex + 1).Take(sqlTokens.Count - beginIndex - 2).ToList();
                sb.AppendLine(ConvertAllSqlAndSqlBatch(innerTokens));
            }
            else
            {
                sb.AppendLine(ConvertSingleCompleteSqlAndSqlBatch(sqlTokens));
            }
        }
        //添加结束标志
        sb.AppendLine(" END IF;");
        return sb.ToString();
    }

    /// <summary>
    /// 转换if 条件
    /// </summary>
    /// <param name="ifConditionSql">要转换的if条件</param>
    /// <param name="needConvertIfContent">是否继续转换后续的if条件语句体</param>
    /// <returns></returns>
    protected virtual string ConvertIfConditionSqlOnly(IList<TSqlParserToken> tokens,out bool needConvertIfContent)
    {
        needConvertIfContent = true;
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
        // 处理 if not exists(select * from sys.tables where name = 'HotelVoiceQtys') 这类语句
        if(tokens.IsIfNotExistsSelectFromSysTablesWhereNameEqualCondition(out tableName))
        {
            return $"IF to_regclass('{tableName}') IS NULL THEN ";
        }
        //处理IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'TryHotelIdForGroup')  这类语句
        if (tokens.IsIfNotExistsSelectFromSysParaWhereCodeEqualCondition(out codeValue))
        {
            return $"IF NOT EXISTS ( SELECT 1 FROM syspara WHERE code = '{codeValue.TrimQuotes()}') THEN ";
        }
        // 处理 IF NOT EXISTS( SELECT * from sysobjects where name =( SELECT TOP 1 name FROM sys.indexes  WHERE is_primary_key = 1   AND object_id  = Object_Id('posSmMappingHid') AND name='PK_posSm_20190808912' ) )  这类语句
        if(tokens.IsIfNotExistsSysobjectsNameEqualNestedIndexSelect(out tableName,out indexName))
        {
            //此语句太过特殊，不好按顺序一个个Token处理，所以直接整体转换，包含if语句块里面的语句，所以needConvertIfContent设为false
            needConvertIfContent = false;
            var sb = new StringBuilder();
            sb.AppendLine($"IF NOT EXISTS ( select * from pg_class where relname = '{indexName}' and relkind = 'i' LIMIT 1) THEN ")
                .AppendLine("")
                .AppendLine("    -- 查找当前表的主键约束名")
                .AppendLine("    SELECT conname INTO pkname")
                .AppendLine("      FROM pg_constraint c")
                .AppendLine("      JOIN pg_class t ON c.conrelid = t.oid")
                .AppendLine($"     WHERE c.contype = 'p' AND t.relname = '{tableName}'")
                .AppendLine("     LIMIT 1;")
                .AppendLine("")
                .AppendLine("    IF pkname IS NOT NULL THEN")
                .AppendLine($"        EXECUTE format('ALTER TABLE %I DROP CONSTRAINT %I', '{tableName}', pkname);")
                .AppendLine("    END IF;")
                .AppendLine("")
                .AppendLine("    -- 新增主键")
                .AppendLine($"    EXECUTE format('ALTER TABLE %I ADD CONSTRAINT %I PRIMARY KEY(%I)','{tableName}', '{indexName}', 'id');");
            return sb.ToString();
        }
        // 处理 if not exists(select * from sys.indexes where name = 'ix_HotelVoiceQtys')  这类语句
        if (tokens.IsIfNotExistsSelectFromSysIndexesWhereNameEqualCondition(out indexName))
        {
            return $"IF NOT EXISTS ( select * from pg_class where relname = '{indexName.ToPostgreSqlIdentifier()}' and relkind = 'i' LIMIT 1) THEN ";
        }
        //处理 if not exists(select id from sysobjects where name = 'ImeiMappingHid') 这类语句
        if (tokens.IsIfNotExistsSelectFromSysobjectsWhereNameEqualCondition(out tableName))
        {
            return $"IF to_regclass('{tableName}') IS NULL THEN ";
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
        //处理通用的if not exists(select * from table where col = 'value')这种情况
        if(tokens.IsIfNotExistsSelectFromTableWhereColumnEqualValueCommon(out tableName,out var columns))
        {
            var sql = new StringBuilder();
            sql.Append($"IF NOT EXISTS ( SELECT 1 FROM {tableName} WHERE");
            var split = " ";
            foreach(var item in columns)
            {
                sql.Append($"{split}{item.Key} = '{item.Value}'");
                split = " AND ";
            }
            sql.Append(") THEN ");
            return sql.ToString();
        }
        //默认直接拼接，但要确保添加 THEN
        var ifCondition = string.Concat(tokens.Select(w => w.Text));
        // 检查是否已经以 THEN 结尾
        if (ifCondition.Trim().EndsWith("THEN", StringComparison.OrdinalIgnoreCase))
        {
            return ifCondition;
        }
        // 如果不包含 THEN，添加 THEN（注意：ifCondition 已经包含 IF 关键字）
        return $"{ifCondition.Trim()} THEN ";
    }
    #endregion

    #region 处理alter语句
    /// <summary>
    /// 处理alter语句,默认会处理表名称和列名称以及列类型的转换
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    protected virtual string ConvertAlterSql(IList<TSqlParserToken> tokens)
    {
        string tableName, columnName,columnType,constraintName,constraintColumns;
        bool isNullable;
        if(tokens.IsAlterTableAddConstraintPrimaryKey(out tableName,out constraintName,out constraintColumns))
        {
            return $"ALTER TABLE {tableName.ToPostgreSqlIdentifier()} ADD CONSTRAINT {constraintName.ToPostgreSqlIdentifier()} PRIMARY KEY ({constraintColumns});";
        }
        if (tokens.IsAlterTableDropConstraint(out tableName, out constraintName))
        {
            return $"ALTER TABLE {tableName.ToPostgreSqlIdentifier()} DROP CONSTRAINT {constraintName.ToPostgreSqlIdentifier()};";
        }
        if (tokens.IsAlterTableAddColumn(out tableName,out List<ColumnDefineItem> addColumns))
        {
            var addColumnSql = new StringBuilder();
            addColumnSql.Append($"ALTER TABLE {tableName.ToPostgreSqlIdentifier()}");
            var columnSplit = "";
            foreach(var column in addColumns)
            {
                addColumnSql.Append($"{columnSplit} ADD {column.Name.ToPostgreSqlIdentifier()} {column.DataTypeDefine.DataType}{(column.DataTypeDefine.IsNullable ? "": " not null")}");
                //有默认值，并且默认值不是newid()时则添加默认值，因为postgresql中要支持uuid的默认值生成的话，必须安装额外的扩展，改为由业务系统直接赋值，并且要保证顺序性
                if (!string.IsNullOrWhiteSpace(column.DataTypeDefine.DefaultValue) && !column.DataTypeDefine.DefaultValue.Equals("NEWID()",StringComparison.OrdinalIgnoreCase))
                {
                    addColumnSql.Append($" DEFAULT {column.DataTypeDefine.DefaultValue}");
                }
                columnSplit = ",";
            }
            addColumnSql.Append(";");
            return addColumnSql.ToString();
        }
        if (tokens.IsAlterTableAlterColumn(out tableName, out var alterColumnDefine))
        {
            columnName = alterColumnDefine.Name.ToPostgreSqlIdentifier();
            var sb = new StringBuilder();
            sb.Append($"ALTER TABLE {tableName.ToPostgreSqlIdentifier()} alter column {columnName} type {alterColumnDefine.DataTypeDefine.DataType}");
            if (alterColumnDefine.DataTypeDefine.IsNullable)
            {
                sb.Append($", alter column {columnName} drop not null");
            }
            else
            {
                sb.Append($", alter column {columnName} set not null");
            }
            sb.Append(";");
            return sb.ToString();
        }
        //其他情况，则直接拼接
        return string.Concat(tokens.Select(w => w.Text));
    }
    #endregion

    #region 转换exec语句
    /// <summary>
    /// 处理exec语句
    /// </summary>
    /// <param name="sqlTokens"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    protected virtual string ConvertExecuteSql(IList<TSqlParserToken> sqlTokens)
    {
        if (sqlTokens == null) return string.Empty;
        var sb = new StringBuilder();
        //处理exec(@sql)这种形式，直接转换为EXECUTE sql即可
        if(sqlTokens.IsExecSqlVariableInParenthesis(out string varName))
        {
            return $"EXECUTE {varName.ToPostgreVariableName()};";
        }
        //处理exec('sql statement')这种形式，注：sql里面可能包含两个单引号，如exec('update versionParas set vProduct=''pms'' where vProduct is null')
        if (sqlTokens.IsExecSqlStringInParenthesis(out string sql) && !string.IsNullOrWhiteSpace(sql))
        {
            var inner = sql.ParseToFragment();
            // Use a dollar-quoted string with a custom tag to avoid conflicts with outer $$ used by procedure/function
            // Choose a tag unlikely to collide with user content (e.g. $exec$)
            var innerSql = ConvertAllSqlAndSqlBatch(inner.ScriptTokenStream);
            sb.Append("EXECUTE $exec$").Append(innerSql).Append("$exec$");
            // append statement terminator if original exec string did not include one
            if (!sql.TrimEnd().EndsWith(";"))
            {
                sb.Append(";");
            }
            return sb.ToString();
        }
        //非以上特殊情况，直接拼接
        return string.Concat(sqlTokens.Select(t => t.Text));
    }
    #endregion

    #region 转换declare语句
    /// <summary>
    /// 转换declare语句
    /// 变量的定义，已经提前在存储过程的declare部分处理过了，这里只需要处理变量赋值即可
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    protected virtual string ConvertDeclareSql(IList<TSqlParserToken> tokens)
    {
        // 检查是否是游标声明
        bool isCursorDeclaration = tokens.Any(t => t.TokenType == TSqlTokenType.Cursor);
        if (isCursorDeclaration)
        {
            // 游标声明需要保留在SQL中，以便后续的游标循环转换可以找到完整的模式
            // 直接返回原始SQL（不进行任何转换）
            return string.Concat(tokens.Select(t => t.Text));
        }

        //检查是否有等号，没有等号则不处理（变量定义已经在存储过程的DECLARE部分处理过了）
        bool hasEqualsSign = tokens.Any(t => t.TokenType == TSqlTokenType.EqualsSign);
        if (!hasEqualsSign)
        {
            return string.Empty;
        }

        //处理declare @sql varchar(1000) = ''这样的语句
        //处理方式，赋值=前的，只取变量名称，账值=后面的直接拼接
        var foundEqualsSign = false;
        var sb = new StringBuilder();
        var sbValue = new StringBuilder();
        for (var i = 0; i < tokens.Count; i++)
        {
            var item = tokens[i];
            if (item.TokenType == TSqlTokenType.Variable && !foundEqualsSign)
            {
                sb.Append(item.Text.ToPostgreVariableName())
                    .Append(" = ");
                continue;
            }
            if (foundEqualsSign)
            {
                sbValue.Append(item.Text);
            }
            if(item.TokenType == TSqlTokenType.EqualsSign)
            {
                foundEqualsSign = true;
            }
        }

        // 处理赋值部分
        var valueStr = sbValue.ToString().Trim();

        // 检查是否是字符串字面量（被单引号括起来）
        if (valueStr.StartsWith("'") && valueStr.EndsWith("'"))
        {
            // 字符串字面量：去掉外层单引号后解析内容
            var innerContent = valueStr.Substring(1, valueStr.Length - 2);
            if (!string.IsNullOrWhiteSpace(innerContent))
            {
                try
                {
                    var valueTokens = innerContent.ParseToFragment().ScriptTokenStream;
                    var convertedValue = ConvertAllSqlAndSqlBatch(valueTokens);
                    sb.Append("'").Append(convertedValue).Append("';");
                }
                catch
                {
                    // 如果解析失败，直接使用原始值
                    sb.Append(valueStr).Append(";");
                }
            }
            else
            {
                sb.Append("'';");
            }
        }
        else
        {
            // 非字符串字面量（如函数调用、变量等）：直接转换
            try
            {
                var valueTokens = valueStr.ParseToFragment().ScriptTokenStream;
                var convertedValue = ConvertAllSqlAndSqlBatch(valueTokens);
                sb.Append(convertedValue).Append(";");
            }
            catch
            {
                // 如果解析失败（如 getdate(); 这样的表达式），直接使用原值
                sb.Append(valueStr);
                // 如果没有分号，添加分号
                if (!valueStr.EndsWith(";"))
                {
                    sb.Append(";");
                }
            }
        }

        return sb.ToString();
    }
    #endregion

    #region 处理语句结束前的内容，比如存储过程需要添加结束$$
    protected virtual string GetSqlContentBeforeEndFile()
    {
        return "";
    }
    #endregion
}
