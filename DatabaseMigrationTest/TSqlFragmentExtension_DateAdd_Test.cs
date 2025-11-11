using DatabaseMigration.Migration;
using DatabaseMigration.ScriptGenerator;

namespace DatabaseMigrationTest;

/// <summary>
/// delete 语句相关的工具类测试
/// </summary>
public class TSqlFragmentExtension_DateAdd_Test
{

    [Fact]
    public void ConvertDeleteSql_SimpleAndDateAddCases()
    {
        var sql = "DELETE FROM sysLog WHERE cDate< DATEADD(DAY,-30,GETDATE())";
        var fragement = sql.ParseToFragment();

        var index = 11;

        var convertedDateAdd = fragement.ScriptTokenStream.GetDateAddSql(ref index);

        var expected = "NOW() - INTERVAL '30 day'";
        Assert.Equal(expected, convertedDateAdd);
    }

    [Fact]
    public void GetDateAdd_WithMonthProducesMonthInterval()
    {
        var sql = "DELETE FROM t WHERE d < DATEADD(MONTH,-1,GETDATE())";
        var fragement = sql.ParseToFragment();
        var index = 12; // position of DATEADD in this token stream

        var converted = fragement.ScriptTokenStream.GetDateAddSql(ref index);

        var expected = "NOW() - INTERVAL '1 month'";
        Assert.Equal(expected, converted);
    }

    [Fact]
    public void GetDateAdd_QuarterIsThreeMonths()
    {
        var sql = "DELETE FROM t WHERE d < DATEADD(quarter,2,GETDATE())";
        var fragement = sql.ParseToFragment();
        var index = 12;

        var converted = fragement.ScriptTokenStream.GetDateAddSql(ref index);

        var expected = "NOW() + INTERVAL '6 month'"; // 2 quarters -> 6 months
        Assert.Equal(expected, converted);
    }

    [Fact]
    public void GetDateAdd_QuotedDatePartAndUtc()
    {
        var sql = "DELETE FROM t WHERE d < DATEADD('day',-30,GETUTCDATE())";
        var fragement = sql.ParseToFragment();
        var index = 12;

        var converted = fragement.ScriptTokenStream.GetDateAddSql(ref index);

        var expected = "TIMEZONE('UTC', NOW()) - INTERVAL '30 day'";
        Assert.Equal(expected, converted);
    }

    [Fact]
    public void GetDateAdd_NonNumericAmount_ProducesExpressionInterval()
    {
        var sql = "DELETE FROM t WHERE d < DATEADD(DAY,n,GETDATE())";
        var fragement = sql.ParseToFragment();
        var index = 12;

        var converted = fragement.ScriptTokenStream.GetDateAddSql(ref index);

        var expected = "NOW() + ((n)::text || ' day')::interval";
        Assert.Equal(expected, converted);
    }
}
