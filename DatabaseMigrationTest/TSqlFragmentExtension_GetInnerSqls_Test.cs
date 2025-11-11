using DatabaseMigration.ScriptGenerator;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigrationTest;

public class TSqlFragmentExtension_GetInnerSqls_Test
{
    [Fact]
    public void GetInnerSqls_BeginEnd_ReturnsInnerTokens()
    {
        var sql = @"BEGIN
 ALTER TABLE dbo.MyTable ADD NewCol VARCHAR(50) NOT NULL DEFAULT 'x'
 ALTER TABLE dbo.MyTable DROP COLUMN OldCol
END";

        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new StringReader(sql), out var errors);
        Assert.Empty(errors);

        var tokens = fragment.ScriptTokenStream;
        int index = tokens.ToList().FindIndex(t => t.TokenType == TSqlTokenType.Begin);
        Assert.True(index >= 0, "Begin token not found");

        var innerTokens = tokens.GetInnerSqls(ref index);
        var innerSql = string.Concat(innerTokens.Select(t => t.Text));

        Assert.Contains("ALTER TABLE dbo.MyTable ADD NewCol", innerSql);
        Assert.Contains("ALTER TABLE dbo.MyTable DROP COLUMN OldCol", innerSql);
        // 索引应该指向end的下一个，由于tokens里面会有一个endoffile的标记，所以索引值就是总数-1
        Assert.Equal(tokens.Count - 1, index);
    }

    [Fact]
    public void GetInnerSqls_Parenthesis_ReturnsInnerTokens()
    {
        var sql = "SELECT * FROM foo WHERE (a =1 AND (b =2))";
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new StringReader(sql), out var errors);
        Assert.Empty(errors);

        var tokens = fragment.ScriptTokenStream;
        int index = tokens.ToList().FindIndex(t => t.TokenType == TSqlTokenType.LeftParenthesis);
        Assert.True(index >= 0, "LeftParenthesis token not found");

        var innerTokens = tokens.GetInnerSqls(ref index);
        var innerSql = string.Concat(innerTokens.Select(t => t.Text));

        Assert.Contains("a =1", innerSql);
        Assert.Contains("(b =2)", innerSql);
        // index should be after the matching right parenthesis
        Assert.True(index > 0 && index <= tokens.Count);
    }

    [Fact]
    public void GetInnerSqls_NoPair_ReturnsAllTokensAndIndexAtEnd()
    {
        var sql = "SELECT 1;";
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new StringReader(sql), out var errors);
        Assert.Empty(errors);

        var tokens = fragment.ScriptTokenStream;
        int index = 0; // token at0 is likely SELECT, not a supported start token

        var result = tokens.GetInnerSqls(ref index);

        // When no matching pair found, method returns the original token list and sets index to tokens.Count
        Assert.Equal(tokens.Count, index);
        Assert.Equal(tokens, result);
    }
}
