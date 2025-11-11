using DatabaseMigration.Migration;
using DatabaseMigration.ScriptGenerator;

namespace DatabaseMigrationTest
{
    public class PostgreSqlScriptGenerator_Convert_Tests
    {
        [Fact]
        public void Convert_SimpleConvert_ReturnsCast()
        {
            var sql = "SELECT convert(varchar(30), 'gs') AS switch";
            var frag = sql.ParseToFragment();
            var tokens = frag.ScriptTokenStream;
            int i = 0;
            // find token index of 'convert'
            for (int k = 0; k < tokens.Count; k++) if (tokens[k].Text.Equals("convert", System.StringComparison.OrdinalIgnoreCase)) { i = k; break; }
            var res = frag.GetConvertSql(ref i);
            Assert.Equal("CAST('gs' AS varchar(30))", res);
        }

        [Fact]
        public void Convert_WithStyle_IgnoresStyle()
        {
            var sql = "SELECT convert(varchar(30), mycol, 121)";
            var frag = sql.ParseToFragment();
            var tokens = frag.ScriptTokenStream;
            int i = 0;
            for (int k = 0; k < tokens.Count; k++) if (tokens[k].Text.Equals("convert", System.StringComparison.OrdinalIgnoreCase)) { i = k; break; }
            var res = frag.GetConvertSql(ref i);
            Assert.Equal("CAST(mycol AS varchar(30))", res);
        }

        [Fact]
        public void Convert_ExpressionWithParens_Works()
        {
            var sql = "SELECT convert(decimal(10,2), (a + b) * c)";
            var frag = sql.ParseToFragment();
            var tokens = frag.ScriptTokenStream;
            int i = 0;
            for (int k = 0; k < tokens.Count; k++) if (tokens[k].Text.Equals("convert", System.StringComparison.OrdinalIgnoreCase)) { i = k; break; }
            var originI = i;
            var res = frag.GetConvertSql(ref i);
            Assert.Equal("CAST((a + b) * c AS decimal(10,2))", res);
            Assert.True(i > originI); // i should have advanced
        }
        [Fact]
        public void ConvertDeleteWhereInSelect()
        {
            var sql = "DELETE roleauth WHERE authcode NOT IN (SELECT authcode FROM authlist)";
            var frag = sql.ParseToFragment();

            var generator = new PostgreSqlProcedureScriptGenerator();
            var converted = generator.GenerateSqlScript(frag);

            var expected = "DELETE FROM roleauth WHERE authcode NOT IN (SELECT authcode FROM authlist);";
            Assert.Equal(expected, converted);
        }
    }
}
