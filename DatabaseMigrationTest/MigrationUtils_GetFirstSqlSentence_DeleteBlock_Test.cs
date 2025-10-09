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
            Assert.Equal(" DELETE FROM dbo.sysLog WHERE cDate< DATEADD(DAY,-30,GETDATE())", first.Replace("\r\n", "\n").TrimEnd());
            Assert.False(string.IsNullOrWhiteSpace(other));
            Assert.Contains("/* 初始化菜单开始 */", other);
        }
    }
}
