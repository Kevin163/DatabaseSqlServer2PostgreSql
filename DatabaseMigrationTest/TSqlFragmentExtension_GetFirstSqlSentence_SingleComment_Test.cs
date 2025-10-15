using DatabaseMigration.ScriptGenerator;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigrationTest;

/// <summary>
/// MigrationUtils获取第一个SQL语句单元的测试类，专注于单行注释场景
/// </summary>
public class TSqlFragmentExtension_GetFirstSqlSentence_SingleComment_Test
{
    /// <summary>
    /// 场景：以行注释开头，随后是 IF 语句。
    /// 期望：firstSql 应为该注释行。
    /// </summary>
    [Fact]
    public void GetFirstCompleteSqlSentence_LeadingLineCommentThenIf_ReturnsCommentAsFirst()
    {
        var sql = @"-- huanghb 2021年1月7日   推送微信模板消息增加保存技师号

IF NOT EXISTS(SELECT 1 FROM syscolumns WHERE id=OBJECT_ID('HotelUserWxInfo') AND name = 'ArtNo')

BEGIN

	ALTER TABLE HotelUserWxInfo ADD ArtNo VARCHAR(6)  NULL

END";
        var parser = new TSql150Parser(true);
        var fragment = parser.Parse(new StringReader(sql), out var errors);

        Assert.Empty(errors);

        int idx = 0;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref idx);

        var expected = "-- huanghb 2021年1月7日   推送微信模板消息增加保存技师号";
        Assert.Equal(expected, tokens[0].Text);
        Assert.Equal(TSqlTokenType.SingleLineComment, tokens[0].TokenType);
        Assert.Equal(1, idx); // idx should now point to the token after the comment
    }
}
