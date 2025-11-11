using DatabaseMigration.ScriptGenerator;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigrationTest;

public class TSqlFragmentExtension_CreateSql_Test
{
    [Fact]
    public void GetCompleteCreateSql_CreateView_DelegatesToCreateView()
    {
        var sql = "CREATE VIEW dbo.TestView AS SELECT 1";
        var parser = new TSql150Parser(true);
        IList<ParseError> errors;
        TSqlFragment fragment;
        using (var rdr = new StringReader(sql))
        {
            fragment = parser.Parse(rdr, out errors);
        }

        Assert.Empty(errors);
        var tokens = fragment.ScriptTokenStream;
        int startIdx = tokens.ToList().FindIndex(t => t.TokenType == TSqlTokenType.Create);
        Assert.True(startIdx >= 0);

        int idx = startIdx;
        var list = tokens.GetCompleteCreateSql(ref idx);

        Assert.NotEmpty(list);
        Assert.Equal(TSqlTokenType.As, list.Last().TokenType);
        Assert.True(idx > startIdx);
    }

    [Fact]
    public void GetCompleteCreateSql_CreateView_Works()
    {
        var sql = "CREATE VIEW [dbo].[TestView] AS SELECT 1";
        var parser = new TSql150Parser(true);
        IList<ParseError> errors;
        TSqlFragment fragment;
        using (var rdr = new StringReader(sql))
        {
            fragment = parser.Parse(rdr, out errors);
        }

        Assert.Empty(errors);
        var tokens = fragment.ScriptTokenStream;
        int startIdx = tokens.ToList().FindIndex(t => t.TokenType == TSqlTokenType.Create);
        Assert.True(startIdx >= 0);

        int idx = startIdx;
        var list = tokens.GetCompleteCreateSql(ref idx);

        Assert.NotEmpty(list);
        Assert.Equal(TSqlTokenType.As, list.Last().TokenType);
        Assert.True(idx > startIdx);
    }

    [Fact]
    public void GetCompleteCreateSql_IndexOutOfRange_ReturnsEmpty()
    {
        var sql = "CREATE VIEW dbo.TestView AS SELECT 1";
        var parser = new TSql150Parser(true);
        IList<ParseError> errors;
        TSqlFragment fragment;
        using (var rdr = new StringReader(sql))
        {
            fragment = parser.Parse(rdr, out errors);
        }

        Assert.Empty(errors);
        int idx = fragment.ScriptTokenStream.Count + 3;
        var list = fragment.ScriptTokenStream.GetCompleteCreateSql(ref idx);
        Assert.Empty(list);
    }
}
