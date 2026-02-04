using DatabaseMigration.Migration;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Text;

namespace DatabaseMigration.ScriptGenerator;

/// <summary>
/// List&lt;TSqlParserToken&gt;的扩展方法
/// </summary>
public static class ListTSqlParserTokenExtension
{
    /// <summary>
    /// 获取第一个非空白的Token类型，如果全部是空白，则返回空白类型
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    public static TSqlTokenType GetFirstNotWhiteSpaceTokenType(this IList<TSqlParserToken> tokens, bool skipComment = false)
    {
        foreach (var tk in tokens)
        {
            if (tk.TokenType != TSqlTokenType.WhiteSpace)
            {
                if (skipComment && (tk.TokenType == TSqlTokenType.MultilineComment || tk.TokenType == TSqlTokenType.SingleLineComment))
                    continue;
                return tk.TokenType;
            }
        }
        return TSqlTokenType.WhiteSpace;
    }
    /// <summary>
    /// 从指定索引开始，获取第一个非空白的Token类型，如果全部是空白，则返回空白类型
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="index"></param>
    /// <param name="skipComment"></param>
    /// <returns></returns>
    public static TSqlTokenType GetFirstNotWhiteSpaceTokenTypeFromIndex(this IList<TSqlParserToken> tokens,int index, bool skipComment = false)
    {
        for(var i = index;i<tokens.Count;i++)
        {
            var tk = tokens[i];
            if (tk.TokenType != TSqlTokenType.WhiteSpace)
            {
                if (skipComment && (tk.TokenType == TSqlTokenType.MultilineComment || tk.TokenType == TSqlTokenType.SingleLineComment))
                    continue;
                return tk.TokenType;
            }
        }
        return TSqlTokenType.WhiteSpace;
    }
    /// <summary>
    /// 查找符合条件的第一个Token的索引，如果没有找到则返回-1
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public static int FindFirstIndex(this IList<TSqlParserToken> tokens, Func<TSqlParserToken, bool> predicate,int startIndex = 0)
    {
        for (int i = startIndex; i < tokens.Count; i++)
        {
            if (predicate(tokens[i]))
                return i;
        }
        return -1;
    }
    /// <summary>
    /// 查找符合条件的最后一个Token的索引，如果没有找到则返回-1
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public static int FindLastIndex(this IList<TSqlParserToken> tokens, Func<TSqlParserToken, bool> predicate)
    {
        for (int i = tokens.Count - 1; i >= 0; i--)
        {
            if (predicate(tokens[i]))
                return i;
        }
        return -1;
    }

    /// <summary>
    /// 判断tokens序列是否与tokenTypes序列匹配，并输出OutValue的值
    /// </summary>
    public static MatchTokenTypesSequenceResult IsMatchTokenTypesSequence(this IList<TSqlParserToken> tokens, List<TSqlTokenTypeItem> tokenTypes, List<TSqlTokenTypeItem>? repeatTokenTypes = null)
    {
        try
        {
            var outValues = new List<string>();
            var outColumnDefines = new List<ColumnDefineItem>();
            if (tokens == null || tokenTypes == null) return MatchTokenTypesSequenceResult.CreateFail();
            /*
             比较是否匹配的规则：
            1 先比较是否满足tokenTypes中的类型和顺序要求
            2 如果满足，并且tokens中还有剩余的Token，则继续循环比较repeatTokenTypes中的类型和顺序要求，同时满足则匹配成功
            3 如果满足，并且tokens中没有剩余的Token，则匹配成功
             */
            var needCheckTokenTypes = tokenTypes;
            int i = 0, j = 0;
            while (i < tokens.Count && j < needCheckTokenTypes.Count)
            {
                var token = tokens[i];
                var typeItem = needCheckTokenTypes[j];
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
                //处理直接输出值的情况，原样直接输出，不做任何额外处理
                if (typeItem.Action == TSqlTokenTypeAction.OutValue)
                {
                    outValues.Add(token.Text);
                }
                //处理输出标识列的情况，需要处理类似schema.table.column的情况，最终只输出column部分
                else if (typeItem.Action == TSqlTokenTypeAction.OutIdentifier)
                {
                    outValues.Add(tokens.GetIdentityName(ref i));
                }
                //处理列的定义情况，需要处理类似column_name data_type [NULL | NOT NULL] [IDENTITY] default getdate() 这样的完整列定义
                else if (typeItem.Action == TSqlTokenTypeAction.OutColumnDefinition)
                {
                    var columnDefine = new ColumnDefineItem();
                    //先取出列名
                    columnDefine.Name = tokens.GetIdentityName(ref i);
                    //跳过可能的空白token到最近的数据类型
                    i = tokens.FindFirstIndex(t => t.TokenType != TSqlTokenType.WhiteSpace, i + 1);
                    //再取出列的数据类型
                    columnDefine.DataTypeDefine = tokens.GetDataTypeText(ref i) ?? new ColumnDataTypeDefineItem();
                    outColumnDefines.Add(columnDefine);
                }
                i++;
                j++;
                //如果已经匹配完tokenTypes，则检查是否还有剩余的Token需要匹配repeatTokenTypes
                if (j == needCheckTokenTypes.Count && i < tokens.Count && repeatTokenTypes != null)
                {
                    //需要确保剩余的不全是空白token，才检查其他列
                    var balanceTokens = tokens.Skip(i+1).ToList();
                    if (balanceTokens.Any(w => w.TokenType != TSqlTokenType.WhiteSpace && w.TokenType != TSqlTokenType.EndOfFile))
                    {
                        needCheckTokenTypes = repeatTokenTypes;
                        j = 0;
                    }
                }
            }
            if (j == needCheckTokenTypes.Count)
            {
                return MatchTokenTypesSequenceResult.CreateSuccess(outValues, i, outColumnDefines);
            }
            return MatchTokenTypesSequenceResult.CreateFail(i);
        }
        catch (Exception ex)
        {
            throw new Exception(string.Concat(tokens.Select(w => w.Text)), ex);
        }
    }
    /// <summary>
    /// 获取标识列的列名，如tableName/dbo.functionName等
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="i"></param>
    /// <returns></returns>
    public static string GetIdentityName(this IList<TSqlParserToken> tokens, ref int i)
    {
        if (tokens == null || i < 0 || i >= tokens.Count) return string.Empty;
        var needReplaceIdentifies = new string[] { "dbo", "[dbo]" };
        int count = tokens.Count;
        int k = i;
        //确保指定索引的下一个是Dot类型的
        var curr = tokens[++k];
        if (curr.TokenType != TSqlTokenType.Dot || !needReplaceIdentifies.Contains(tokens[i].Text.ToLower()))
        {
            return tokens[i].Text.ToPostgreSqlIdentifier();
        }
        //如果是的话，则继续向后查，直到找到第一个Identifier
        for (; k < tokens.Count; k++)
        {
            curr = tokens[k];
            if (curr.TokenType == TSqlTokenType.Identifier || curr.TokenType == TSqlTokenType.QuotedIdentifier)
            {
                break;
            }
        }
        i = k;
        return curr.Text.ToPostgreSqlIdentifier();
    }
    /// <summary>
    /// 获取数据类型定义实例，如varchar(200) not null/bit/int not null identity(1,1)/datetime not null default getdate()等
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="i"></param>
    /// <returns></returns>
    public static ColumnDataTypeDefineItem? GetDataTypeText(this IList<TSqlParserToken> tokens, ref int i)
    {
        if (tokens == null || i < 0 || i >= tokens.Count) return null;
        int count = tokens.Count;
        var dataTypeDefine = new ColumnDataTypeDefineItem();
        #region 处理数据类型，如bit/varchar(200)/numeric(18,2)等
        var dataTypeSb = new StringBuilder();
        var token = tokens[i];
        dataTypeSb.Append(token.Text);
        if (i + 1 < tokens.Count && tokens[i + 1].TokenType == TSqlTokenType.LeftParenthesis)
        {
            while (i < tokens.Count - 1)
            {
                i++;
                dataTypeSb.Append(tokens[i].Text);
                //如果找到了右括号，则结束
                if (tokens[i].TokenType == TSqlTokenType.RightParenthesis)
                {
                    break;
                }
            }
        }
        dataTypeDefine.DataType = MigrationUtils.ConvertToPostgresType(dataTypeSb.ToString(), null);
        #endregion

        /*
         由于后面的not null/default等都是可选的，所以需要继续向后查找，直到找到下一个逗号或结束为止
         并且后面的顺序也是可以随意排列的，并没有一定的先后顺序，所以需要在循环中同时处理
         */
        if (i + 1 < tokens.Count)
        {
            //取出i+1到下一个逗号或结束之间的所有Token，检查not null/identity(1,1)/default/primary key等
            int k = i + 1;
            while (k < tokens.Count)
            {
                var tk = tokens[k];
                if (tk.TokenType == TSqlTokenType.Comma)
                {
                    break;
                }
                //处理not null的情况
                if (tk.TokenType == TSqlTokenType.Not)
                {
                    var nextTokenType = tokens.GetFirstNotWhiteSpaceTokenTypeFromIndex(k + 1);
                    if (nextTokenType == TSqlTokenType.Null)
                    {
                        dataTypeDefine.IsNullable = false;
                        k = tokens.FindFirstIndex(t => t.TokenType == TSqlTokenType.Null, k + 1);
                    }
                }
                //处理identity(1,1)的情况
                else if (tk.TokenType == TSqlTokenType.Identity)
                {
                    dataTypeDefine.IsIdentity = true;
                    //跳过可能的(1,1)部分
                    var nextTokenType = tokens.GetFirstNotWhiteSpaceTokenTypeFromIndex(k + 1);
                    if (nextTokenType == TSqlTokenType.LeftParenthesis)
                    {
                        k = tokens.FindFirstIndex(t => t.TokenType == TSqlTokenType.RightParenthesis, k + 1);
                    }
                }
                //处理primary key的情况
                else if (tk.TokenType == TSqlTokenType.Primary)
                {
                    var indexForKey = tokens.FindFirstIndex(t => t.TokenType == TSqlTokenType.Key, k + 1);
                    if(indexForKey > 0)
                    {
                        dataTypeDefine.IsPrimaryKey = true;
                        k = indexForKey;
                    }
                }
                //处理default 0/default '1'/default newid()/default getdate()等情况
                else if(tk.TokenType == TSqlTokenType.Default)
                {
                    var defaultValueSb = new StringBuilder();
                    k++;
                    //跳过可能的空白token
                    while (k < tokens.Count && tokens[k].TokenType == TSqlTokenType.WhiteSpace)
                    {
                        k++;
                    }
                    if (k < tokens.Count)
                    {
                        // 检查是否是 DEFAULT(0) 这种格式（左括号紧跟在DEFAULT后面或只有空格）
                        if(tokens[k].TokenType == TSqlTokenType.LeftParenthesis)
                        {
                            k++; // 跳过左括号
                            while (k < tokens.Count)
                            {
                                // 遇到右括号时停止，不添加右括号
                                if (tokens[k].TokenType == TSqlTokenType.RightParenthesis)
                                {
                                    break;
                                }
                                defaultValueSb.Append(tokens[k].Text);
                                k++;
                            }
                            dataTypeDefine.DefaultValue = MigrationUtils.ConvertToPostgresFunction(defaultValueSb.ToString());
                        }
                        else
                        {
                            // 正常格式：DEFAULT 0 或 DEFAULT 'value'
                            defaultValueSb.Append(tokens[k].Text);
                            //判断下一个是否是左括号，如果是的话，则继续取出，直到找到对应的右括号
                            if(k+1<tokens.Count && tokens[k+1].TokenType == TSqlTokenType.LeftParenthesis)
                            {
                                k++;
                                while (k < tokens.Count)
                                {
                                    defaultValueSb.Append(tokens[k].Text);
                                    if (tokens[k].TokenType == TSqlTokenType.RightParenthesis)
                                    {
                                        break;
                                    }
                                    k++;
                                }
                            }
                            dataTypeDefine.DefaultValue = MigrationUtils.ConvertToPostgresFunction(defaultValueSb.ToString());
                        }
                    }
                }
                k++;
            }
            i = k - 1; //更新i的位置
        }
        return dataTypeDefine;
    }

    /// <summary>
    /// 处理类似convert(varchar(30) , 'gs') 的语句，转换为 CAST('gs' AS varchar(30))
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="i"></param>
    /// <returns></returns>
    public static string GetConvertSql(this IList<TSqlParserToken> tokens, ref int i,PostgreSqlScriptGenerator postgreSqlScriptGenerator)
    {
        if (tokens == null || i < 0 || i >= tokens.Count) return string.Empty;

        // 确保指定索引的token是'convert
        var curr = tokens[i];
        if (curr.TokenType != TSqlTokenType.Convert)
            return string.Empty;

        // 查找最近一个左括号的索引
        int openIdx = -1;
        for (int k = i + 1; k < tokens.Count; k++)
        {
            if (tokens[k].Text == "(") { openIdx = k; break; }
            // skip possible whitespace/comments tokens
        }
        if (openIdx < 0) { i = i + 1; return "convert"; }

        // 解析括号内的内容，分离出type, expr, style三个部分
        var typeSb = new StringBuilder();
        var exprSb = new StringBuilder();

        int depth = 0; // depth inside nested parentheses AFTER the convert( ...
        int phase = 0; // 0: parsing type, 1: parsing expr, 2: skipping style
        int endIdx = openIdx; // will point to closing ')'

        for (int k = openIdx + 1; k < tokens.Count; k++)
        {
            var t = tokens[k].Text;

            // Handle parentheses to track nesting within arguments
            if (t == "(")
            {
                depth++;
                // include the parenthesis in current buffer
                if (phase == 0) typeSb.Append(t);
                else if (phase == 1) exprSb.Append(t);
                continue;
            }
            if (t == ")")
            {
                if (depth == 0)
                {
                    // This is the closing parenthesis of CONVERT(...)
                    endIdx = k;
                    break;
                }
                depth--;
                if (phase == 0) typeSb.Append(t);
                else if (phase == 1) exprSb.Append(t);
                continue;
            }

            // Comma separating arguments at top level (depth == 0)
            if (t == "," && depth == 0)
            {
                if (phase == 0)
                {
                    phase = 1; // switch to parsing expression
                    continue;
                }
                else if (phase == 1)
                {
                    // third parameter (style) starts; we'll skip or ignore it
                    phase = 2;
                    continue;
                }
            }

            // Append token text to appropriate buffer depending on phase
            if (phase == 0)
            {
                typeSb.Append(t);
            }
            else if (phase == 1)
            {
                exprSb.Append(t);
            }
            else
            {
                // phase == 2 -> skipping style; do nothing other than continue until closing )
            }
        }

        // Move i to the closing parenthesis index so caller will continue after it
        i = endIdx;

        var typeStr = typeSb.ToString().Trim();
        var exprStr = exprSb.ToString().Trim();

        // Post-process some common patterns: remove outer parentheses from expression
        if (exprStr.StartsWith("(") && exprStr.EndsWith(")"))
        {
            // only trim if parentheses match
            int cnt = 0; bool ok = true;
            for (int p = 0; p < exprStr.Length; p++)
            {
                if (exprStr[p] == '(') cnt++; else if (exprStr[p] == ')') cnt--; if (cnt == 0 && p < exprStr.Length - 1) { ok = false; break; }
            }
            if (ok) exprStr = exprStr.Substring(1, exprStr.Length - 2).Trim();
        }
        // 如果表达式是带+号的复杂表达式，则在前面加上select，做为一个完整的语句进行替换，替换完成后再去掉select
        if (exprStr.Contains("+"))
        {
            var sqlExprStr = $"SELECT {exprStr}";
            try
            {
                var sqlExprTokens = sqlExprStr.ParseToFragment().ScriptTokenStream;
                var sqlExprConverted = postgreSqlScriptGenerator.GenerateSqlScript(sqlExprTokens, needAddSqlContentBeforeEndFile: false);
                // 去掉select
                if (sqlExprConverted.StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase))
                {
                    exprStr = sqlExprConverted.Substring(7).Trim();
                }
                else
                {
                    exprStr = sqlExprConverted;
                }
                //去年掉末尾的分号
                if (exprStr.EndsWith(";"))
                {
                    exprStr = exprStr.Substring(0, exprStr.Length - 1).Trim();
                }
            }
            catch
            {
                // ignore parse error, keep original
            }
        }
        // Construct CAST(expression AS type)
        var result = new StringBuilder();
        result.Append("CAST(");
        result.Append(exprStr.Length > 0 ? exprStr : string.Empty);
        result.Append(" AS ");
        result.Append(typeStr.Length > 0 ? typeStr : string.Empty);
        result.Append(")");

        return result.ToString();
    }
    /// <summary>
    /// 处理类似DATEADD(DAY,-30,GETDATE())的语句，转换为 now() + interval '-30 day'
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="i"></param>
    /// <returns></returns>
    public static string GetDateAddSql(this IList<TSqlParserToken> tokens, ref int i)
    {
        // 确保指定索引的token是'DATEADD
        if (tokens == null || i < 0 || i >= tokens.Count) return string.Empty;
        var curr = tokens[i];

        if (!(curr.TokenType == TSqlTokenType.Identifier && curr.Text.Equals("DATEADD", StringComparison.OrdinalIgnoreCase)))
        {
            return string.Empty;
        }

        // 查找最近一个左括号的索引
        int openIdx = -1;
        for (int k = i + 1; k < tokens.Count; k++)
        {
            if (tokens[k].Text == "(") { openIdx = k; break; }
        }
        if (openIdx < 0) { i = i + 1; return "DATEADD"; }

        var partSb = new StringBuilder();
        var numberSb = new StringBuilder();
        var dateSb = new StringBuilder();

        int depth = 0; // depth inside nested parentheses AFTER the DATEADD(
        int phase = 0; // 0: parsing datepart, 1: parsing number, 2: parsing date expression
        int endIdx = openIdx;

        for (int k = openIdx + 1; k < tokens.Count; k++)
        {
            var t = tokens[k].Text;

            if (t == "(")
            {
                depth++;
                if (phase == 0) partSb.Append(t);
                else if (phase == 1) numberSb.Append(t);
                else dateSb.Append(t);
                continue;
            }
            if (t == ")")
            {
                if (depth == 0)
                {
                    endIdx = k;
                    break;
                }
                depth--;
                if (phase == 0) partSb.Append(t);
                else if (phase == 1) numberSb.Append(t);
                else dateSb.Append(t);
                continue;
            }

            if (t == "," && depth == 0)
            {
                if (phase == 0)
                {
                    phase = 1; // switch to parsing number
                    continue;
                }
                else if (phase == 1)
                {
                    phase = 2; // switch to parsing date expression
                    continue;
                }
            }

            if (phase == 0) partSb.Append(t);
            else if (phase == 1) numberSb.Append(t);
            else dateSb.Append(t);
        }

        // Move i to the closing parenthesis index so caller will continue after it
        i = endIdx;

        var partStr = partSb.ToString().Trim();
        var numberStr = numberSb.ToString().Trim();
        var dateStr = dateSb.ToString().Trim();

        // Normalize date expression
        string pgDateExpr = dateStr;
        if (string.IsNullOrEmpty(pgDateExpr)) pgDateExpr = "NOW()"; // fallback
        else
        {
            // map GETDATE() or GETDATE to NOW()
            if (pgDateExpr.IndexOf("GETDATE", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                pgDateExpr = "NOW()";
            }
            else if (pgDateExpr.IndexOf("GETUTCDATE", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                pgDateExpr = "TIMEZONE('UTC', NOW())"; // approximation
            }
        }

        // map datepart to postgres units
        string partKey = partStr;
        if ((partKey.StartsWith("'") && partKey.EndsWith("'")) || (partKey.StartsWith("\"") && partKey.EndsWith("\"")))
            partKey = partKey.Substring(1, partKey.Length - 2);
        partKey = partKey.Trim().ToLowerInvariant();

        string unit;
        bool multiplyQuarter = false;
        switch (partKey)
        {
            case "yy":
            case "yyyy":
            case "year":
                unit = "year"; break;
            case "qq":
            case "quarter":
                // quarter is 3 months
                unit = "month"; multiplyQuarter = true; break;
            case "mm":
            case "m":
            case "month":
                unit = "month"; break;
            case "day":
            case "dd":
            case "d":
                unit = "day"; break;
            case "hour":
            case "hh":
                unit = "hour"; break;
            case "minute":
            case "mi":
            case "n":
                unit = "minute"; break;
            case "second":
            case "ss":
            case "s":
                unit = "second"; break;
            case "ms":
            case "millisecond":
                unit = "millisecond"; break;
            default:
                // try to use the raw token as unit (safe fallback)
                unit = partKey; break;
        }

        // handle numeric sign
        var numTrim = numberStr;
        // remove surrounding parentheses
        if (numTrim.StartsWith("(") && numTrim.EndsWith(")")) numTrim = numTrim.Substring(1, numTrim.Length - 2).Trim();

        bool negative = false;
        string absNum = numTrim;
        if (absNum.StartsWith("-")) { negative = true; absNum = absNum.Substring(1).Trim(); }

        // if number is numeric, compute value; otherwise treat as expression
        bool isNumeric = double.TryParse(absNum, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double numericVal);

        string intervalExpr;
        if (isNumeric)
        {
            if (multiplyQuarter) numericVal = numericVal * 3.0;
            // format without scientific notation
            var valStr = numericVal.ToString(System.Globalization.CultureInfo.InvariantCulture);
            intervalExpr = $"INTERVAL '{valStr} {unit}'";
        }
        else
        {
            // expression interval: build ((expr)::text || ' unit')::interval
            // absNum may be an expression, preserve it
            var expr = absNum;
            if (multiplyQuarter)
            {
                // multiply expression by 3: ( (expr) * 3 )
                expr = $"(({expr}) * 3)";
            }
            intervalExpr = $"(({expr})::text || ' {unit}')::interval";
        }

        string result;
        if (negative)
        {
            result = $"{pgDateExpr} - {intervalExpr}";
        }
        else
        {
            result = $"{pgDateExpr} + {intervalExpr}";
        }

        return result;
    }
    /// <summary>
    /// 获取所有的DeclareItem
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    public static List<DeclareItem> GetAllDealreItems(this IList<TSqlParserToken> tokens)
    {
        var result = new List<DeclareItem>();
        if (tokens == null || tokens.Count == 0) return result;
        int count = tokens.Count;
        for (int i = 0; i < count; i++)
        {
            var tk = tokens[i];
            string name = "";
            var typeText = new StringBuilder(30);
            if (tk.TokenType == TSqlTokenType.Declare)
            {
                //处理declare @pkname varchar(200)这样的语句，其中的@pkname类型是Variable，varchar是Identifier
                //找到第一个Identifier
                int j = i + 1;
                for (; j < count; j++)
                {
                    var curr = tokens[j];
                    if (curr.TokenType == TSqlTokenType.Variable)
                    {
                        name = curr.Text.ToPostgreVariableName();
                    }
                    if (curr.TokenType == TSqlTokenType.Identifier || curr.TokenType == TSqlTokenType.QuotedIdentifier)
                    {
                        typeText.Append(curr.Text);
                        //如果后面是左括号，则继续向后查找，直到找到对应的右括号
                        var next = tokens.Count > j + 1 ? tokens[j + 1] : null;
                        if (next != null && next.TokenType == TSqlTokenType.LeftParenthesis)
                        {
                            typeText.Append(next.Text);
                            j++;
                            int parenCount = 1;
                            for (; j + 1 < count; j++)
                            {
                                var tk2 = tokens[j + 1];
                                typeText.Append(tk2.Text);
                                if (tk2.TokenType == TSqlTokenType.LeftParenthesis)
                                    parenCount++;
                                else if (tk2.TokenType == TSqlTokenType.RightParenthesis)
                                    parenCount--;
                                if (parenCount == 0)
                                    break;
                            }
                        }
                        if (!string.IsNullOrWhiteSpace(name) && typeText.Length > 0)
                        {
                            result.Add(new DeclareItem
                            {
                                Name = name,
                                TypeText = typeText.ToString()
                            });
                            break;
                        }
                    }
                }
                i = j; //更新i的位置
            }
        }
        return result;
    }
}
