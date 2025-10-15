using DatabaseMigration.ScriptGenerator;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigrationTest
{
    public class PostgreSqlScriptGenerator_Convert_Tests
    {
        private TSqlFragment ParseFragment(string sql)
        {
            var parser = new TSql150Parser(true);
            using var rdr = new StringReader(sql);
            var frag = parser.Parse(rdr, out var errors);
            if (errors != null && errors.Count > 0)
            {
                throw new System.Exception("Parse errors: " + string.Join(";", errors));
            }
            return frag;
        }

        [Fact]
        public void Convert_SimpleConvert_ReturnsCast()
        {
            var sql = "SELECT convert(varchar(30), 'gs') AS switch";
            var frag = ParseFragment(sql);
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
            var frag = ParseFragment(sql);
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
            var frag = ParseFragment(sql);
            var tokens = frag.ScriptTokenStream;
            int i = 0;
            for (int k = 0; k < tokens.Count; k++) if (tokens[k].Text.Equals("convert", System.StringComparison.OrdinalIgnoreCase)) { i = k; break; }
            var originI = i;
            var res = frag.GetConvertSql(ref i);
            Assert.Equal("CAST((a + b) * c AS decimal(10,2))", res);
            Assert.True(i > originI); // i should have advanced
        }
    }
}
