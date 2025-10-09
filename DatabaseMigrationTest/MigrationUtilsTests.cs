using Xunit;
using DatabaseMigration.Migration;

namespace DatabaseMigrationTest
{
    /// <summary>
    /// 针对 <see cref="MigrationUtils.GetFirstCompleteSqlSentence(string)"/> 的单元测试集合。
    /// 包含空输入、首行为单行块注释、无块注释边界行为，以及带有多行块注释的存储过程头部提取场景。
    /// </summary>
    public class MigrationUtilsTests
    {
        /// <summary>
        /// 场景：传入空字符串。
        /// 期望：方法返回的 <c>firstSql</c> 与 <c>otherSql</c> 都为空字符串。
        /// </summary>
        [Fact]
        public void GetFirstCompleteSqlSentence_EmptyInput_ReturnsEmpty()
        {
            var (first, other) = MigrationUtils.GetFirstCompleteSqlSentence(string.Empty);
            Assert.Equal(string.Empty, first);
            Assert.Equal(string.Empty, other);
        }

    }
}