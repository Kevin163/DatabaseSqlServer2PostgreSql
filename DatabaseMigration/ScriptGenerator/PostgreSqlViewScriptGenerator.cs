using DatabaseMigration.Migration;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Text;

namespace DatabaseMigration.ScriptGenerator;

/// <summary>
/// 视图脚本生成器
/// </summary>
public class PostgreSqlViewScriptGenerator : PostgreSqlScriptGenerator
{
    /// <summary>
    /// 第一行select语句中的列名
    /// </summary>
    private List<string> _columnNames = new List<string>();
    /// <summary>
    /// 是否是第一行的select语句
    /// </summary>
    private bool _isFirstSelectSql = false;
    /// <summary>
    /// 非第一行select语句的列索引，用于对应第一行的列名，从0开始，每遇到一个,则+1，每遇到一个select则重置为0
    /// </summary>
    private int _indexForOtherSelectSql = 0;
    /// <summary>
    /// 非第一行select语句的的当前列索引是否包含identity属性，包含则为true，则不需要额外添加列名，否则需要添加列名（在遇到,时进行添加）
    /// </summary>
    private bool _isCurrentColumnHasIdentity = false;
    /// <summary>
    /// 生成Select语句
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    protected override string ConvertSelectSql(IList<TSqlParserToken> tokens)
    {
        int lastEndLineIndex = 0;
        var sb = new StringBuilder();
        //完整的seelct语句包含select,from,where等，而只有select部分中的name='value',name = a.columnName等需要处理,而from ,where等不需要处理
        var isInSelect = false;
        for(var i=0;i<tokens.Count;i++)
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
            //处理第一行的开始,第一行的开始是以第一个select语句开始的，第一行语句开始的时候，列名列表是空的，后面的select语句开始时，列名列表已经有值了
            if (item.TokenType == TSqlTokenType.Select && !_isFirstSelectSql && _columnNames.Count == 0)
            {
                _isFirstSelectSql = true;
                sb.Append(item.Text);
                continue;
            }
            //处理dbo.uf_getmask(...)函数，转换为uf_getmask(...)，但不能处理a.columnName这种，所以要求第一个的文本必须是dbo
            if((item.TokenType == TSqlTokenType.Identifier || item.TokenType == TSqlTokenType.QuotedIdentifier)
                && item.Text.Equals("dbo", StringComparison.OrdinalIgnoreCase)
                 && tokens[i+1].TokenType == TSqlTokenType.Dot)
            {
                //直接跳到.后面的下一个标识符
                i += 2;
                item = tokens[i];
            }
            //处理select 'Engineering' as 'Product'，即as别名也是字符串的情况
            if (item.TokenType == TSqlTokenType.As)
            {
                var nextTokens = tokens.Skip(i + 1).Take(5).ToList();
                var nextTokenType = nextTokens.GetFirstNotWhiteSpaceTokenType();
                if(nextTokenType == TSqlTokenType.AsciiStringLiteral)
                {
                    sb.Append(item.Text);
                    for(var j = 0; j < nextTokens.Count; j++)
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
            //处理所有Identifier,都更改为小写
            if (item.TokenType == TSqlTokenType.Identifier || item.TokenType == TSqlTokenType.QuotedIdentifier)
            {
                //判断是否是colName = 'value'这样的形式，如果是，则需要将等号改为as，并且将列名放到后面
                //同时需要妆容colName = a.columnName这样的形式
                var nextTokens = tokens.Skip(i+1).ToList();
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
                //如果是第一行中的列名，则记录下来
                if (_isFirstSelectSql)
                {
                    _columnNames.Add(item.Text.ToPostgreSqlIdentifier());
                }
                //如果不是第一行，则表示当前列已经有identity属性了
                else
                {
                    _isCurrentColumnHasIdentity = true;
                }
                continue;
            }
            //处理类似convert(varchar(30) , 'gs') 的语句，转换为 CAST('gs' AS varchar(30))
            if (item.TokenType == TSqlTokenType.Convert)
            {
                sb.Append(tokens.GetConvertSql(ref i));
                continue;
            }
            //处理非第一行的select语句，需要重置列索引和identity标志
            if (item.TokenType == TSqlTokenType.Select && !_isFirstSelectSql && _columnNames.Count > 0)
            {
                _indexForOtherSelectSql = 0;
                _isCurrentColumnHasIdentity = false;
                sb.Append(item.Text);
                continue;
            }

            //处理非第一行的,逗号列分隔，表示一列已经完成，如果当前列没有identity属性，则需要添加列名，并且增加列索引
            if (item.TokenType == TSqlTokenType.Comma && !_isFirstSelectSql)
            {
                if (!_isCurrentColumnHasIdentity)
                {
                    sb.Append($" AS {_columnNames[_indexForOtherSelectSql]}");
                }
                _indexForOtherSelectSql++;
            }
            //如果是换行符，则记录最后一个换行符的位置
            if (item.TokenType == TSqlTokenType.WhiteSpace && item.Text.Contains("\r\n"))
            {
                lastEndLineIndex = sb.Length;
            }
            //非特殊类型的，直接添加
            sb.Append(item.Text);
        }
        //循环结束后，判断最后一列是否需要添加列名
        if (!_isFirstSelectSql && _columnNames.Count > 0)
        {
            if (!_isCurrentColumnHasIdentity)
            {
                if(lastEndLineIndex <= 0) lastEndLineIndex = sb.Length;
                sb.Insert(lastEndLineIndex, $" AS {_columnNames[_indexForOtherSelectSql]}");
            }
        }
        //当前select语句处理完毕后，如果仍然是第一行，则重置为非第一行
        if (_isFirstSelectSql)
        {
            _isFirstSelectSql = false;
        }
        return sb.ToString();
    }
}
