using DatabaseMigration.Migration;
using DatabaseMigration.ScriptGenerator;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Xunit;

namespace DatabaseMigrationTest;

/// <summary>
/// 测试临时表的转换
/// SQL Server 使用 #temp 表示局部临时表
/// PostgreSQL 使用 CREATE TEMP TABLE 或直接使用表名（无 # 前缀）
/// </summary>
public class PostgreSqlScriptGenerator_TempTable_Tests
{
    [Theory]
    // SELECT INTO #temp 在 PostgreSQL 中转换为 DROP TABLE IF EXISTS + CREATE TEMP TABLE ... AS SELECT ...
    [InlineData(@"SELECT * INTO #temp FROM table1", @"DROP TABLE IF EXISTS temp;
CREATE TEMP TABLE temp AS SELECT * FROM table1;")]
    [InlineData(@"INSERT INTO #temp VALUES (1)", @"INSERT INTO temp VALUES (1);")]
    [InlineData(@"UPDATE #temp SET col = 1", @"UPDATE temp SET col = 1;")]
    [InlineData(@"DELETE FROM #temp", @"DELETE FROM temp;")]
    [InlineData(@"DROP TABLE #temp", @"DROP TABLE temp;")]
    [InlineData(@"SELECT * FROM #temp", @"SELECT * FROM temp")]
    public void ConvertSingleStatement_WithTempTable_ShouldRemoveHashPrefix(string tsql, string expected)
    {
        var fragment = tsql.ParseToFragment();
        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(fragment);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertComplexProcedure_WithTempTable_ShouldConvertCorrectly()
    {
        var tsql = @"
CREATE PROCEDURE test_temp
AS
BEGIN
    SELECT *
    INTO #temp_hotel
    FROM hotel
    WHERE id > 0;

    UPDATE h
    SET h.col = 1
    FROM #temp_hotel AS h
    WHERE h.id = 1;

    DROP TABLE #temp_hotel;
END;
";

        var fragment = tsql.ParseToFragment();
        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(fragment);

        // 验证临时表名称被正确转换
        Assert.Contains("temp_hotel", result);
        // 验证 # 前缀被移除
        Assert.DoesNotContain("#temp_hotel", result);
        // 验证 SELECT INTO 被转换为 CREATE TEMP TABLE ... AS SELECT ...
        Assert.Contains("CREATE TEMP TABLE temp_hotel AS SELECT *", result);
        // 验证核心功能
        Assert.Contains("UPDATE h", result);
        Assert.Contains("DROP TABLE temp_hotel", result);
    }

    [Fact]
    public void ConvertSelectIntoWithComplexColumns_ShouldConvertCorrectly()
    {
        // 测试日志中的实际案例：SELECT ... INTO temp_hotel FROM hotel WHERE ...
        var tsql = @"
SELECT
    hid,
    cast('' AS varchar(50)) AS pmsversioncode,
    producttype AS pmsversionname,
    cast('' AS varchar(50)) AS memberversioncode,
    prodmbrtype AS memberversionname
INTO temp_hotel
FROM hotel
WHERE ltrim(rtrim(isnull(producttype,''))) != '' OR ltrim(rtrim(isnull(prodmbrtype,''))) != ''
";

        var fragment = tsql.ParseToFragment();
        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(fragment);

        // 验证转换为 CREATE TEMP TABLE ... AS SELECT ...
        Assert.Contains("CREATE TEMP TABLE temp_hotel AS", result);
        Assert.Contains("SELECT", result);
        Assert.Contains("FROM hotel", result);
        // 验证不会出现 "不是一个已知变量" 的错误
        Assert.DoesNotContain("INTO temp_hotel", result);
    }
}
