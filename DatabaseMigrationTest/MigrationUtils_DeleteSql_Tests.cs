using DatabaseMigration.Migration;

namespace DatabaseMigrationTest
{
    /// <summary>
    /// delete 语句相关的工具类测试
    /// </summary>
    public class MigrationUtils_DeleteSql_Tests
    {
        /// <summary>
        /// 验证 TryParseDeleteOlderThanDateAdd 方法能正确解析示例语句
        /// </summary>
        [Fact]
        public void TryParseDeleteOlderThanDateAdd_ParsesExamples()
        {
            var s1 = "DELETE FROM sysLog WHERE cDate< DATEADD(DAY,-30,GETDATE())";
            Assert.True(DeleteSqlUtils.TryParseDeleteOlderThanDateAdd(s1, out var table1, out var col1, out var days1));
            Assert.Equal("sysLog", table1);
            Assert.Equal("cDate", col1);
            Assert.Equal(-30, days1);

            var s2 = "DELETE FROM slowlog WHERE logTime< DATEADD(DAY,-20,GETDATE())";
            Assert.True(DeleteSqlUtils.TryParseDeleteOlderThanDateAdd(s2, out var table2, out var col2, out var days2));
            Assert.Equal("slowlog", table2);
            Assert.Equal("logTime", col2);
            Assert.Equal(-20, days2);
        }

        [Fact]
        public void ConvertDeleteSql_SimpleAndDateAddCases()
        {
            var s1 = "DELETE FROM sysLog WHERE cDate< DATEADD(DAY,-30,GETDATE())";
            var (converted1, need1) = DeleteSqlUtils.ConvertDeleteSql(s1);
            Assert.Equal("DELETE FROM sysLog WHERE cDate < current_date - 30;\n", converted1);
            Assert.Equal("", need1);

            var s2 = "delete authlist";
            var (converted2, need2) = DeleteSqlUtils.ConvertDeleteSql(s2);
            Assert.Equal("DELETE FROM authlist;\n", converted2);
            Assert.Equal("", need2);

            var s3 = "DELETE FROM slowlog WHERE logTime< DATEADD(DAY,-20,GETDATE())";
            var (converted3, need3) = DeleteSqlUtils.ConvertDeleteSql(s3);
            Assert.Equal("DELETE FROM slowlog WHERE logTime < current_date - 20;\n", converted3);
            Assert.Equal("", need3);
        }
    }
}
