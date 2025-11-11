using DatabaseMigration.ScriptGenerator;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigrationTest;

/// <summary>
/// MigrationUtils获取第一个SQL语句单元的测试类，专注于块注释场景。
/// </summary>
public class TSqlFragmentExtension_GetFirstSqlSentence_BlockComment_Test
{
    /// <summary>
    /// 场景：第一行为单行块注释（包含开始和结束标记），后面跟一个 SELECT 语句。
    /// 期望：通过 ScriptDom 解析为 TSqlFragment 并调用 GetFirstCompleteSqlTokens 获取第一个完整语句的 tokens，
    /// 验证返回的 token 应该只有第一个注释块
    /// </summary>
    [Fact]
    public void GetFirstCompleteSqlSentence_SingleLineBlockComment_ReturnsCommentAndSelectTokens()
    {
        var sql = "/* comment */\nSELECT 1;";
        var parser = new TSql150Parser(true);
        IList<ParseError> errors;
        TSqlFragment fragment;
        using (var rdr = new StringReader(sql))
        {
            fragment = parser.Parse(rdr, out errors);
        }

        Assert.Empty(errors);
        int idx = 0;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref idx);

        Assert.NotEmpty(tokens);
        Assert.Equal("/* comment */\n", string.Concat(tokens.Select(t => t.Text)));
        Assert.Equal(2,idx); // idx should now point to the token after the comment block
    }
    /// <summary>
    /// 场景：多行块注释，后面是其他内容
    /// 期望：通过 ScriptDom 解析并调用 GetFirstCompleteSqlTokens，验证返回的 token 文本包含整个注释块
    /// </summary>
    [Fact]
    public void GetFirstCompleteSqlSentence_ProcedureWithLeadingBlockComment_ReturnsCommentAndRestTokens()
    {
        var sql = @" /****************************************************************************

作者：陈提见

日期：2016-05-7

功能：命名成这样是为了这个最常用的存储过程排序在最前面



这个存储过程的作用是为了程序启用后用来更改数据库结构或加入一些固定数据，例如系统参数，权限列表等。

 

exec a_update_Sys

****************************************************************************/

-- 后续脚本行应该被视为 otherSql 的一部分
SELECT 1;";
        var parser = new TSql150Parser(true);
        IList<ParseError> errors;
        TSqlFragment fragment;
        using (var rdr = new StringReader(sql))
        {
            fragment = parser.Parse(rdr, out errors);
        }

        Assert.Empty(errors);
        int idx = 0;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref idx);
        var combined = string.Concat(tokens.Select(t => t.Text));

        Assert.NotEmpty(tokens);
        Assert.Equal(6,tokens.Count);
        Assert.Equal(TSqlTokenType.WhiteSpace, tokens[0].TokenType);
        Assert.Equal(TSqlTokenType.MultilineComment, tokens[1].TokenType);
        Assert.Equal(6,idx); // idx should now point to the token after the comment block
    }
}
