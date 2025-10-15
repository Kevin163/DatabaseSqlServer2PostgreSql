using Xunit;
using DatabaseMigration.Migration;

namespace DatabaseMigrationTest
{
    public class MigrationUtils_RemoveSchemaPrefix_Test
    {
        [Fact]
        public void RemoveSchemaPrefix_RemovesBracketedSchema()
        {
            var sql = "CREATE   view [dbo].m_v_channelCode";
            var outSql = MigrationUtils.RemoveSchemaPrefix(sql, "[dbo]");
            Assert.Equal("CREATE   view m_v_channelCode", outSql);
        }

        [Fact]
        public void RemoveSchemaPrefix_RemovesPlainSchemaWithDot()
        {
            var sql = "CREATE   view dbo.m_v_channelCode";
            var outSql = MigrationUtils.RemoveSchemaPrefix(sql, "dbo.");
            Assert.Equal("CREATE   view m_v_channelCode", outSql);
        }

        [Fact]
        public void RemoveSchemaPrefix_RemovesQuotedSchema()
        {
            var sql = "CREATE   view \"dbo\".m_v_channelCode";
            var outSql = MigrationUtils.RemoveSchemaPrefix(sql, "\"dbo\"");
            Assert.Equal("CREATE   view m_v_channelCode", outSql);
        }

        [Fact]
        public void RemoveSchemaPrefix_DoesNotTouchStringLiterals()
        {
            var sql = "SELECT 'dbo.m_v_channelCode' as s";
            var outSql = MigrationUtils.RemoveSchemaPrefix(sql, "dbo.");
            Assert.Equal(sql, outSql);
        }
    }
}
