using DatabaseMigration.Migration;
using DatabaseMigration.ScriptGenerator;
using Xunit;

namespace DatabaseMigrationTest;

/// <summary>
/// 测试 SELECT TOP 语法转换为 PostgreSQL LIMIT
/// </summary>
public class PostgreSqlScriptGenerator_SelectTop_Tests
{
    [Theory]
    [InlineData("SELECT TOP 5 id, name FROM users", "SELECT id, name FROM users LIMIT 5;")]
    [InlineData("SELECT TOP 10 * FROM products", "SELECT * FROM products LIMIT 10;")]
    [InlineData("select top 1 id,hid,dbid from auditextend", "SELECT id,hid,dbid FROM auditextend LIMIT 1;")] // 大小写不敏感
    public void ConvertSelectWithTop_ShouldConvertToLimit(string tsql, string expected)
    {
        var fragment = tsql.ParseToFragment();
        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(fragment);

        // 输出调试信息
        Console.WriteLine($"Expected: {expected}");
        Console.WriteLine($"Actual:   {result}");

        // 忽略大小写和多余空格的比较
        var normalizedExpected = expected.Replace(" ", "").Replace("\r", "").Replace("\n", "").ToUpper();
        var normalizedResult = result.Replace(" ", "").Replace("\r", "").Replace("\n", "").ToUpper();
        Assert.Equal(normalizedExpected, normalizedResult);
    }

    [Fact]
    public void ConvertSelectIntoWithTop_ShouldConvertCorrectly()
    {
        // 测试 SELECT TOP ... INTO 的转换
        var tsql = @"
SELECT TOP 1 id,hid,dbid,audittype
INTO #temp_list_retry
FROM auditExtend
WHERE status = 1
";

        var fragment = tsql.ParseToFragment();
        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(fragment);

        // 验证转换为 CREATE TEMP TABLE ... AS SELECT ... LIMIT
        Assert.Contains("CREATE TEMP TABLE", result);
        Assert.Contains("AS SELECT id,hid,dbid,audittype", result);
        Assert.Contains("FROM", result);
        Assert.Contains("LIMIT 1", result);
        Assert.DoesNotContain("TOP", result);
        Assert.DoesNotContain("#temp_list_retry", result);
    }

    [Fact]
    public void ConvertComplexSelectWithTop_ShouldConvertCorrectly()
    {
        // 测试复杂的 SELECT TOP 语句
        var tsql = @"
insert into temp_list
select top 5 id,hid,dbid,auditType from
(
select ROW_NUMBER() over(partition by dbid,auditType order by cdate) as rowIndex,* from auditExtend where status=0
)a where rowIndex=1
";

        var fragment = tsql.ParseToFragment();
        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(fragment);

        // 验证 TOP 被转换为 LIMIT
        Assert.Contains("LIMIT 5", result);
        Assert.DoesNotContain("TOP 5", result);
    }
}
