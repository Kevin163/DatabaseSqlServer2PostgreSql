using DatabaseMigration.Migration;

namespace DatabaseMigrationTest
{
    /// <summary>
    /// MigrationUtils获取第一个SQL语句单元的测试类，专注于块注释场景。
    /// </summary>
    public class MigrationUtils_GetFirstSqlSentence_BlockComment_Test
    {
        /// <summary>
        /// 场景：第一行为单行块注释（包含开始和结束标记），后面跟一个 SELECT 语句。
        /// 期望：方法将该单行块注释识别为完整的第一个 SQL 片段返回为 <c>firstSql</c>，
        /// 其余 SQL 返回在 <c>otherSql</c> 中（保持原始换行/顺序，使用 Trim() 断言时忽略首尾空白）。
        /// </summary>
        [Fact]
        public void GetFirstCompleteSqlSentence_SingleLineBlockComment_ReturnsCommentAsFirstAndRestAsOther()
        {
            var sql = "/* comment */\nSELECT 1;";
            var (first, other) = MigrationUtils.GetFirstCompleteSqlSentence(sql);
            Assert.Equal("/* comment */", first.Trim());
            Assert.Equal("SELECT 1;", other.Trim());
        }
        /// <summary>
        /// 场景：多行块注释，后面是其他内容
        /// 期望：方法应识别并返回注释作为第一个语句单元，剩余内容作为其他 SQL 返回。
        /// </summary>
        [Fact]
        public void GetFirstCompleteSqlSentence_ProcedureWithLeadingBlockComment_ReturnsHeaderBeforeComment()
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
            var (first, other) = MigrationUtils.GetFirstCompleteSqlSentence(sql);

            // 期望 first 包含 整个注释块
            var expectedFirst = @" /****************************************************************************

作者：陈提见

日期：2016-05-7

功能：命名成这样是为了这个最常用的存储过程排序在最前面



这个存储过程的作用是为了程序启用后用来更改数据库结构或加入一些固定数据，例如系统参数，权限列表等。

 

exec a_update_Sys

****************************************************************************/
";
            Assert.Equal(expectedFirst, (first ?? string.Empty));

            // other 应包含注释及后续 SQL
            Assert.False(string.IsNullOrWhiteSpace(other));
            Assert.Contains("-- 后续脚本行应该被视为 otherSql 的一部分", other, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
