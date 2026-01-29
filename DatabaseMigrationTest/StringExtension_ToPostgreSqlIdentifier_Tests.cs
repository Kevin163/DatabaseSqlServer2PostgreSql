using DatabaseMigration.Migration;
using Xunit;

namespace DatabaseMigrationTest;

public class StringExtension_ToPostgreSqlIdentifier_Tests
{
    [Fact]
    public void DboBracketedTable_ReturnsTableNameLowercaseWithoutSchema()
    {
        var input = "[dbo].[helpfiles]";
        var res = input.ToPostgreSqlIdentifier();
        Assert.Equal("helpfiles", res);
    }

    [Fact]
    public void DboDotTable_ReturnsTableNameLowercaseWithoutSchema()
    {
        var input = "dbo.helpFiles";
        var res = input.ToPostgreSqlIdentifier();
        Assert.Equal("helpfiles", res);
    }

    [Fact]
    public void PlainNameWithUppercase_ReturnsLowercase()
    {
        var input = "Helpfiles";
        var res = input.ToPostgreSqlIdentifier();
        Assert.Equal("helpfiles", res);
    }
}
