using DatabaseMigration.Migration;

namespace DatabaseMigrationTest
{
    /// <summary>
    /// MigrationUtils获取第一个SQL语句单元的测试类，专注于单行注释场景
    /// </summary>
    public class MigrationUtils_GetFirstSqlSentence_SingleComment_Test
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
            var (first, other) = MigrationUtils.GetFirstCompleteSqlSentence(sql);
            var expected = "-- huanghb 2021年1月7日   推送微信模板消息增加保存技师号";
            Assert.Equal(expected, (first ?? string.Empty).Replace("\r\n", "\n").Trim());
            Assert.False(string.IsNullOrWhiteSpace(other));
            Assert.Contains("ALTER TABLE HotelUserWxInfo ADD ArtNo", other.Replace("\r\n", "\n"), System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
