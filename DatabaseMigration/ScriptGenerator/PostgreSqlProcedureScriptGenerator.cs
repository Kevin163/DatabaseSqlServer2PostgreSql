using DatabaseMigration.Migration;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Text;

namespace DatabaseMigration.ScriptGenerator;

/// <summary>
/// PostgreSQL存储过程脚本生成器
/// </summary>
public class PostgreSqlProcedureScriptGenerator : PostgreSqlScriptGenerator
{
    private List<DeclareItem> _declareItems = new List<DeclareItem>();
    private Dictionary<string, string> _parameterMapping = new Dictionary<string, string>();
    //是否需要在整个存储过程结束的时候，添加$$;结束符，默认不添加，在执行了create procedure...as之后，因为这里面添加的as $$,则更改为需要添加
    private bool needAddEndSymbols = false;
    //是否已经添加了 begin，用于后续判断是否需要添加 end;
    private bool _hasAddedBegin = false;
    /// <summary>
    /// 重写父类的生成脚本方法
    /// 增加提取存储过程所有语句中的declare语句，提取到存储过程所有语句的前面的declare区中
    /// </summary>
    /// <param name="fragment"></param>
    /// <returns></returns>
    public override string GenerateSqlScript(TSqlFragment fragment)
    {
        _declareItems = fragment.ScriptTokenStream.GetAllDealreItems();
        _hasAddedBegin = false; // 重置标志
        return GenerateSqlScript(fragment.ScriptTokenStream);
    }

    /// <summary>
    /// 转换所有SQL语句，并在转换后替换函数体内的参数引用
    /// </summary>
    /// <param name="sqlTokens"></param>
    /// <returns></returns>
    protected override string ConvertAllSqlAndSqlBatch(IList<TSqlParserToken> sqlTokens)
    {
        var sql = base.ConvertAllSqlAndSqlBatch(sqlTokens);
        // 替换函数体内的参数引用
        foreach (var kvp in _parameterMapping)
        {
            sql = sql.Replace(kvp.Key, kvp.Value);
        }
        return sql;
    }

    /// <summary>
    /// 转换单个Create ... As语句，需要处理存储过程的参数
    /// sql server的存储过程允许没有参数时省略括号，而postgresql不允许
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    protected override string ConvertCreateAsOnly(IList<TSqlParserToken> tokens)
    {
        var sb = new StringBuilder();
        var inParameterList = false;
        var parenDepth = 0;
        var hasLeftParenthesis = false;
        var addedLeftParen = false;     // 是否已经添加了左括号（用于没有括号的参数列表）
        var foundProcKeyword = false;   // 是否已经找到 PROC/PROCEDURE 关键字
        var foundProcName = false;      // 是否已经找到存储过程名称
        var addedParameter = false;     // 是否已经添加了参数
        bool hasBeginInOriginalSql = false; //检查 AS 后面的 token 中是否已经有 BEGIN 关键字

        for (var i = 0; i < tokens.Count; i++)
        {
            var item = tokens[i];
            //处理create,替换为create or replace
            if (item.TokenType == TSqlTokenType.Create)
            {
                sb.Append("CREATE OR REPLACE");
                continue;
            }
            //处理 PROCEDURE 关键字
            if (item.TokenType == TSqlTokenType.Proc || item.TokenType == TSqlTokenType.Procedure)
            {
                foundProcKeyword = true;
                sb.Append("procedure ");
                continue;
            }
            //处理空白换行符，在创建语句中不需要多余的换行
            if(item.TokenType == TSqlTokenType.WhiteSpace)
            {
                // 如果是在 PROCEDURE 关键字之后，AS 之前的空白，全部跳过
                if (foundProcKeyword && !foundProcName)
                {
                    continue;
                }
                // 否则只跳过换行符，保留其他空白
                if (item.Text.Equals("\r\n"))
                {
                    continue;
                }
            }
            //处理存储过程名称（在找到 PROCEDURE 后，找到的第一个标识符）
            if (foundProcKeyword && !foundProcName)
            {
                // 检查是否是标识符类型
                if (item.TokenType == TSqlTokenType.Identifier ||
                    item.TokenType == TSqlTokenType.QuotedIdentifier ||
                    item.TokenType == TSqlTokenType.AsciiStringOrQuotedIdentifier)
                {
                    // 检查是否是 dbo 前缀
                    bool isDbo = item.Text.ToLower() == "dbo" || item.Text.ToLower() == "[dbo]";
                    if (isDbo && i + 1 < tokens.Count && tokens[i + 1].TokenType == TSqlTokenType.Dot)
                    {
                        // 跳过 dbo .
                        i += 1;  // 跳过点号
                        continue;
                    }

                    // 现在应该是真正的存储过程名称
                    var name = item.Text.ToPostgreSqlIdentifier();
                    sb.Append(name);
                    foundProcName = true;
                    // 找到存储过程名称后，参数列表可能开始（对于没有括号的参数）
                    inParameterList = true;
                    continue;
                }
            }
            //处理左括号，进入参数列表
            if (item.TokenType == TSqlTokenType.LeftParenthesis)
            {
                if (foundProcName)
                {
                    inParameterList = true;
                    hasLeftParenthesis = true;
                    parenDepth++;
                }
                sb.Append(item.Text);
                continue;
            }
            //处理右括号，离开参数列表
            if (item.TokenType == TSqlTokenType.RightParenthesis)
            {
                parenDepth--;
                if (parenDepth == 0)
                {
                    inParameterList = false;
                }
                sb.Append(item.Text);
                continue;
            }
            //处理参数列表中的变量名，去掉@前缀
            if (inParameterList && item.TokenType == TSqlTokenType.Variable)
            {
                // 如果没有括号且还没添加左括号，现在添加
                if (!hasLeftParenthesis && !addedLeftParen)
                {
                    sb.Append("(");
                    addedLeftParen = true;
                }
                var originalParamName = item.Text;
                var paramName = originalParamName.ToPostgreVariableName();
                // 记录参数映射，用于函数体内的变量引用转换
                _parameterMapping[originalParamName] = paramName;
                sb.Append(paramName);
                addedParameter = true;
                continue;
            }
            //处理参数列表中的类型，如 uniqueidentifier 需要转换为 uuid
            if (inParameterList && item.TokenType == TSqlTokenType.Identifier)
            {
                // 检查后面是否跟着空括号（类型修饰符，如 uniqueidentifier()）
                if (i + 2 < tokens.Count &&
                    tokens[i + 1].TokenType == TSqlTokenType.LeftParenthesis &&
                    tokens[i + 2].TokenType == TSqlTokenType.RightParenthesis)
                {
                    // 跳过空括号
                    i += 2;
                }
                var typeText = item.Text.TrimQuotes();
                var convertedType = MigrationUtils.ConvertToPostgresType(typeText, null);
                sb.Append(" ").Append(convertedType);
                continue;
            }
            //创建存储过程需要在as前面，处理语言和没有参数的情况
            if (item.TokenType == TSqlTokenType.As)
            {
                inParameterList = false;  // 遇到AS，参数列表结束
                // 如果添加了参数但没有括号，需要添加右括号
                if (addedParameter && addedLeftParen && !hasLeftParenthesis)
                {
                    sb.Append(")");
                }
                //如果没有添加参数且没有左括号，则补上空括号
                if (!addedParameter && !hasLeftParenthesis)
                {
                    //保持()和存储过程名称在同一行，所以如果当前sb最后一个字符是换行符，则需要去掉
                    if(sb[sb.Length - 1] == '\n')
                    {
                        sb.Length -= 1;
                    }
                    else if (sb.Length >= 2 && sb[sb.Length - 2] == '\r' && sb[sb.Length - 1] == '\n')
                    {
                        sb.Length -= 2;
                    }
                    sb.Append("() ");
                }
                //添加 LANGUAGE 和 AS（保留 AS 的原始大小写）
                sb.AppendLine().AppendLine("LANGUAGE plpgsql");
                //检查as后面的下一个非空白token，是否是begin关键字，如果是，则不需要在后面添加begin
                var nextNonEmptyTokenAfterAs = tokens.GetFirstNotWhiteSpaceTokenTypeFromIndex(i + 1, skipComment: true);
                hasBeginInOriginalSql = nextNonEmptyTokenAfterAs == TSqlTokenType.Begin;
                // 保留 AS token 的原始文本大小写
                var asText = item.Text; // AS 或 as
                sb.AppendLine($"{asText} $$");
                continue;  // 跳过默认的 item.Text 添加
            }
            else
            {
                sb.Append(item.Text);
            }
        }
        //由于create procedure是到as就结束了，AS 后面已经添加了 $$，所以现在直接处理 declare
        needAddEndSymbols = true;
        //处理所有declare变量声明
        foreach(var declare in _declareItems)
        {
            sb.AppendLine($"DECLARE {declare.Name} {MigrationUtils.ConvertToPostgresType(declare.TypeText,null)};");
        }
        //只有在原始 SQL 中没有 BEGIN，或者有 DECLARE 语句时才添加 begin
        //如果有 DECLARE，必须添加 BEGIN 来包裹变量声明
        //如果原始 SQL 中已经有 BEGIN，就不添加额外的 begin
        if(_declareItems.Count > 0 || !hasBeginInOriginalSql)
        {
            sb.Append("begin"); // 不添加换行符，避免产生额外的空行
        }
        //记录是否添加了 begin，用于后续判断是否需要添加 end;
        _hasAddedBegin = (_declareItems.Count > 0 || !hasBeginInOriginalSql);
        return sb.ToString();
    }
    /// <summary>
    /// 由于create as重写了，添加了$$开始符号，所以在结束前需要添加结束符号
    /// </summary>
    /// <returns></returns>
    protected override string GetSqlContentBeforeEndFile()
    {
        //只有当添加了 begin 时才添加 end;
        var endPart = needAddEndSymbols && _hasAddedBegin ? $"{Environment.NewLine}end;" : "";
        return needAddEndSymbols ? $"{endPart}{Environment.NewLine}$$;" : "";
    }
}
