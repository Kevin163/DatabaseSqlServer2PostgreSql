using Xunit;
using DatabaseMigration.Migration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseMigrationTest
{
    /// <summary>
    /// MigrationUtils.GetFirstSqlSentence 方法的单元测试，专注于处理create procedure 语句的场景。
    /// </summary>
    public class MigrationUtils_GetFirstSqlSentence_CreateProcedure_Test
    {

        /// <summary>
        /// 场景：创建存储过程头部，后面紧跟多行块注释（长注释用于描述作者／功能等）。
        /// 期望：方法应识别并返回注释之前完整的第一个语句单元（包含 CREATE ... 与 as 行）。
        /// </summary>
        [Fact]
        public void GetFirstCompleteSqlSentence_ProcedureWithLeadingBlockComment_ReturnsHeaderBeforeComment()
        {
            var sql = @"CREATE procedure [dbo].[a_update_Sys]

as

/****************************************************************************

作者：陈提见

日期：2016-05-7

功能：命名成这样是为了这个最常用的存储过程排序在最前面



这个存储过程的作用是为了程序启用后用来更改数据库结构或加入一些固定数据，例如系统参数，权限列表等。

 

exec a_update_Sys

****************************************************************************/

-- 后续脚本行应该被视为 otherSql 的一部分
SELECT 1;";
            var (first, other) = MigrationUtils.GetFirstCompleteSqlSentence(sql);

            // 期望 first 包含 CREATE ... 和 as 两行（保留中间的一个空行）
            var expectedFirst = "CREATE procedure [dbo].[a_update_Sys]\n\nas";
            Assert.Equal(expectedFirst, (first ?? string.Empty).Replace("\r\n", "\n").Trim());

            // other 应包含注释及后续 SQL（至少应包含 exec a_update_Sys 或 SELECT 1）
            Assert.False(string.IsNullOrWhiteSpace(other));
            Assert.Contains("exec a_update_Sys", other, System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 场景：create procedure 包含参数定义的情况，后面紧跟注释。
        /// 期望：firstSql 应包含整个 create procedure 头与参数列表以及 AS 行。
        /// </summary>
        [Fact]
        public void GetFirstCompleteSqlSentence_CreateProcedureWithParameters_ReturnsFullHeader()
        {
            var sql = @"create  procedure [dbo].[up_crsAddLqHotel]
(
	@xmlText text = null
)
AS

--1.解析并获取参数值";
            var (first, other) = MigrationUtils.GetFirstCompleteSqlSentence(sql);

            var expected = @"create  procedure [dbo].[up_crsAddLqHotel]
(
	@xmlText text = null
)
AS
";
            Assert.Equal(expected, (first ?? string.Empty));
            Assert.False(string.IsNullOrWhiteSpace(other));
            Assert.Contains("--1.解析并获取参数值", other);
        }
    }
}
