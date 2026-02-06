using DatabaseMigration.Migration;
using DatabaseMigration.ScriptGenerator;
using Xunit;

namespace DatabaseMigrationTest;

public class PostgreSqlScriptGenerator_WithNoLock_Tests
{
    [Fact]
    public void ConvertSelectWithNoLock_ShouldRemoveWithNoLock()
    {
        var tsql = "select top 1 @databaseId = [dbid] from hotelProducts with(nolock) where hid = @hid and productCode = 'pms';";
        var expected = "select dbid from hotelProducts where hid = @hid and productCode = 'pms' LIMIT 1;";

        var fragment = tsql.ParseToFragment();
        // Since the user mentioned 'databaseid = dbid', but standard SQL is just select column, 
        // and variable assignment usually implies stored proc context, but here let's just test the query part mainly.
        // However, the generator might generate 'databaseid = dbid' if it's preserving variable assignment in select?
        // Actually, looking at previous conversations, variable assignment in select usually becomes distinct statements or SELECT INTO.
        // But here let's focus on with(nolock) removal.
        
        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(fragment);

        // We specifically want to check that "with(nolock)" is gone.
        Assert.DoesNotContain("with(nolock)", result.ToLower());
        Assert.DoesNotContain("(nolock)", result.ToLower());
        
        // Also verifying it generates valid pg sql for the rest
        // Note: variable assignment in select might need special handling but that's outside the scope of *this* specific error unless it affects parsing.
        // The error reported "select databaseid = dbid", implying the generator kept the assignment syntax which might be issue #2, 
        // but the user complained about "with".
        // Let's first ensure we reproduce the "with(nolock)" presence (or ensure it's removed).
    }

    [Theory]
    [InlineData("select * from users with(nolock)", "select * from users")]
    [InlineData("select * from users (nolock)", "select * from users")]
    public void SelectTable_WithNoLock_ShouldRemoveHint(string tsql, string expectedFragment)
    {
        var fragment = tsql.ParseToFragment();
        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(fragment);

        Assert.DoesNotContain("with(nolock)", result.ToLower());
        Assert.DoesNotContain("(nolock)", result.ToLower());
    }
}
