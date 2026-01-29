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
    //是否需要在整个存储过程结束的时候，添加$$;结束符，默认不添加，在执行了create procedure...as之后，因为这里面添加的as $$,则更改为需要添加
    private bool needAddEndSymbols = false;
    /// <summary>
    /// 重写父类的生成脚本方法
    /// 增加提取存储过程所有语句中的declare语句，提取到存储过程所有语句的前面的declare区中
    /// </summary>
    /// <param name="fragment"></param>
    /// <returns></returns>
    public override string GenerateSqlScript(TSqlFragment fragment)
    {
        _declareItems = fragment.ScriptTokenStream.GetAllDealreItems();
        return GenerateSqlScript(fragment.ScriptTokenStream);
    }

    /// <summary>
    /// 转换单个Create ... As语句，需要处理存储过程的参数
    /// sql server的存储过程允许没有参数时省略括号，而postgresql不允许
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    protected override string ConvertCreateAsOnly(IList<TSqlParserToken> tokens)
    {
        //是否拥有左括号,如果拥有的话，则表示是有参数的存储过程
        var hasLeftParenthesis = tokens.Any(t=>t.TokenType == TSqlTokenType.LeftParenthesis);
        var sb = new StringBuilder();
        for (var i = 0; i < tokens.Count; i++)
        {
            var item = tokens[i];
            //处理create,替换为create or replace
            if (item.TokenType == TSqlTokenType.Create)
            {
                sb.Append("CREATE OR REPLACE");
                continue;
            }
            //处理dbo.name这样的标识符，去掉前面的dbo.
            if (item.TokenType == TSqlTokenType.AsciiStringOrQuotedIdentifier || item.TokenType == TSqlTokenType.QuotedIdentifier)
            {
                sb.Append(tokens.GetIdentityName(ref i));
                continue;
            }
            //处理空白换行符，在创建语句中不需要多余的换行
            if(item.TokenType == TSqlTokenType.WhiteSpace && item.Text.Equals("\r\n"))
            {
                continue;
            }
            //创建存储过程需要在as前面，处理语言和没有参数的情况
            if (item.TokenType == TSqlTokenType.As)
            {
                //如果没有参数，则补上空括号
                if (!hasLeftParenthesis)
                {
                    sb.Append("() ");
                }
                //补充上语言标识
                sb.AppendLine()
                    .AppendLine("LANGUAGE plpgsql");
            }
            sb.Append(item.Text);
        }
        //由于create procedure是到as就结束了，所以在最后需要补上一个$$换行
        sb.AppendLine(" $$");
        needAddEndSymbols = true;
        //处理所有declare变量声明
        foreach(var declare in _declareItems)
        {
            sb.AppendLine($"DECLARE {declare.Name} {MigrationUtils.ConvertToPostgresType(declare.TypeText,null)};");
        }
        //如果有变量声明项，则需要添加一个Begin块来隔离变量声明与其他语句
        if(_declareItems.Count > 0)
        {
            sb.AppendLine("BEGIN");
        }
        return sb.ToString();
    }
    /// <summary>
    /// 由于create as重写了，添加了$$开始符号，所以在结束前需要添加结束符号
    /// </summary>
    /// <returns></returns>
    protected override string GetSqlContentBeforeEndFile()
    {
        var declareEnd = needAddEndSymbols && _declareItems.Count > 0 ? $"{Environment.NewLine}END;" : "";
        return needAddEndSymbols ? $"{declareEnd}{Environment.NewLine}$$;" : declareEnd;
    }
}
