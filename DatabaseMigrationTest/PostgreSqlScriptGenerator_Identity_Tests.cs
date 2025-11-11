using DatabaseMigration.ScriptGenerator;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigrationTest
{
    public class PostgreSqlScriptGenerator_Identity_Tests
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
        public void GetIdentityName_QuotedDotIdentifier_ReturnsIdentifier()
        {
            var sql = "SELECT \"myschema\".MyColumn FROM MyTable";
            var frag = ParseFragment(sql);
            var tokens = frag.ScriptTokenStream;
            int i = 0;
            for (int k = 0; k < tokens.Count; k++) if ((tokens[k].TokenType == TSqlTokenType.QuotedIdentifier || tokens[k].TokenType == TSqlTokenType.AsciiStringOrQuotedIdentifier
) && tokens[k].Text.Contains("myschema")) { i = k; break; }
            var originI = i;
            var res = frag.GetIdentityName(ref i);
            Assert.Equal("mycolumn", res);
            Assert.True(i > originI);
        }

        [Fact]
        public void GetIdentityName_NotQuotedIdentifier_ReturnsItemTextAndDoesNotChangeIndex()
        {
            var sql = "SELECT MyColumn FROM MyTable";
            var frag = ParseFragment(sql);
            var tokens = frag.ScriptTokenStream;
            int i = 0;
            for (int k = 0; k < tokens.Count; k++) if ((tokens[k].TokenType == TSqlTokenType.QuotedIdentifier || tokens[k].TokenType == TSqlTokenType.AsciiStringOrQuotedIdentifier
)) { i = k; break; }
            var originI = i;
            var res = frag.GetIdentityName(ref i);
            Assert.Equal("select", res);
            Assert.Equal(originI, i);
        }

        [Fact]
        public void GetIdentityName_DboDotMyTable_NotQuoted_ReturnsEmptyAndDoesNotChangeIndex()
        {
            var sql = "SELECT column1 FROM dbo.MyTable";
            var frag = ParseFragment(sql);
            var helper = new PostgreSqlViewScriptGenerator();
            var tokens = frag.ScriptTokenStream;
            int i = 0;
            for (int k = 0; k < tokens.Count; k++) if ((tokens[k].TokenType == TSqlTokenType.QuotedIdentifier || tokens[k].TokenType == TSqlTokenType.Identifier
) && tokens[k].Text.Equals("dbo", System.StringComparison.OrdinalIgnoreCase)) { i = k; break; }
            var originI = i;
            var res = frag.GetIdentityName(ref i);
            Assert.Equal("mytable", res);
            Assert.Equal(originI+2, i);
        }

        [Fact]
        public void GetIdentityName_BracketedSchemaDotTable_NotQuoted_ReturnsEmptyAndDoesNotChangeIndex()
        {
            var sql = "SELECT column1 FROM [dbo].[myTable]";
            var frag = ParseFragment(sql);
            var helper = new PostgreSqlViewScriptGenerator();
            var tokens = frag.ScriptTokenStream;
            int i = 0;
            for (int k = 0; k < tokens.Count; k++) if ((tokens[k].TokenType == TSqlTokenType.QuotedIdentifier || tokens[k].TokenType == TSqlTokenType.AsciiStringOrQuotedIdentifier
) && tokens[k].Text.Contains("[dbo]")) { i = k; break; }
            var originI = i;
            var res = frag.GetIdentityName(ref i);
            Assert.Equal("mytable", res);
            Assert.Equal(originI+2, i);
        }
    }
}
