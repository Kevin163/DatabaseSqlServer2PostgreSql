using DatabaseMigration.Migration;

namespace DatabaseMigrationTest
{
    /// <summary>
    /// MigrationUtils获取第一个SQL语句单元的测试类，专注于SELECT块场景。
    /// </summary>
    public class MigrationUtils_GetFirstSqlSentence_SelectBlock_Test
    {

        /// <summary>
        /// 场景：简单的SELECT语句，没有块注释。
        /// 期望：方法应能正确提取出第一个SELECT语句作为firstSql，其余部分作为otherSql返回。
        /// </summary>
        [Fact]
        public void GetFirstCompleteSqlSentence_NoBlockComments_ReturnsEmptyFirstAndEmptyOther()
        {
            var sql = "SELECT 1;\nSELECT 2;";
            var (first, other) = MigrationUtils.GetFirstCompleteSqlSentence(sql);
            Assert.Equal("SELECT 1;\r\n", first);
            Assert.Equal("SELECT 2;", other);
        }
    }
}
