using DatabaseMigration.Migration;

namespace DatabaseMigrationTest
{
    /// <summary>
    /// MigrationUtils获取第一个SQL语句单元的测试类，专注于DELETE块场景。
    /// </summary>
    public class MigrationUtils_GetFirstSqlSentence_DeleteBlock_Test
    {

        /// <summary>
        /// 场景：DELETE 语句在块注释之前。
        /// 期望：方法应能提取出完整的第一个 DELETE 语句作为 <c>firstSql</c>，其余部分作为 <c>otherSql</c> 返回。
        /// </summary>
        [Fact]
        public void GetFirstCompleteSqlSentence_FirstDeleteBeforeBlockComment_ReturnsFirstDelete()
        {
            var sql = @" DELETE FROM dbo.sysLog WHERE cDate< DATEADD(DAY,-30,GETDATE())

 DELETE FROM dbo.slowlog WHERE logTime< DATEADD(DAY,-30,GETDATE())


/* 初始化菜单开始 */";
            var (first, other) = MigrationUtils.GetFirstCompleteSqlSentence(sql);
            Assert.Equal(" DELETE FROM dbo.sysLog WHERE cDate< DATEADD(DAY,-30,GETDATE())", first.NormalizeLineEndings().TrimEnd());
            Assert.False(string.IsNullOrWhiteSpace(other));
            Assert.Contains("/* 初始化菜单开始 */", other);
        }

        /// <summary>
        /// 场景：单条 DELETE 语句后直接跟块注释（没有其它 SQL）。
        /// 期望：first 为 DELETE 语句，other 为块注释文本。
        /// </summary>
        [Fact]
        public void GetFirstCompleteSqlSentence_DeleteThenBlockComment_ReturnsDeleteAndComment()
        {
            var sql = @" DELETE FROM slowlog WHERE logTime< DATEADD(DAY,-30,GETDATE())

/* 初始化菜单开始 */";

            var (first, other) = MigrationUtils.GetFirstCompleteSqlSentence(sql);

            var expectedFirst = " DELETE FROM slowlog WHERE logTime< DATEADD(DAY,-30,GETDATE())";
            var expectedOtherTrimmed = "/* 初始化菜单开始 */";

            Assert.Equal(expectedFirst, first.NormalizeLineEndings().TrimEnd());
            Assert.Equal(expectedOtherTrimmed, other.NormalizeLineEndings().Trim());
        }

        /// <summary>
        /// 场景：单条 DELETE 语句后直接跟行注释。
        /// 期望：first 为 DELETE 语句，other 为行注释文本。
        /// </summary>
        [Fact]
        public void GetFirstCompleteSqlSentence_DeleteFollowedByLineComment_ReturnsDeleteAndComment()
        {
            var sql = "delete AuthButtons\n--DELETE AuthButtons\n";
            var (first, other) = MigrationUtils.GetFirstCompleteSqlSentence(sql);
            Assert.Equal("delete AuthButtons", first.NormalizeLineEndings().TrimEnd());
            Assert.Equal("--DELETE AuthButtons", other.NormalizeLineEndings().Trim());
        }
    }
}
