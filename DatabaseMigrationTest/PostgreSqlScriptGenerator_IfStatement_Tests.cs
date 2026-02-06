using DatabaseMigration.Migration;
using DatabaseMigration.ScriptGenerator;
using Xunit;

namespace DatabaseMigrationTest;

/// <summary>
/// 测试 IF 语句转换为 PL/pgSQL 的 IF ... THEN ... END IF 语法
/// </summary>
public class PostgreSqlScriptGenerator_IfStatement_Tests
{
    [Theory]
    [InlineData("if (select count(1) from temp_list) <= 0", "IF (( SELECT COUNT(1) FROM temp_list) <= 0) THEN ")]
    [InlineData("if( (select count(1) from #temp_list) <= 0 )", "IF (( SELECT COUNT(1) FROM #temp_list) <= 0) THEN ")]
    [InlineData("if exists (select id from users)", "IF EXISTS ( SELECT id FROM users) THEN ")]
    public void ConvertIfStatement_ShouldAddThen(string tsql, string expected)
    {
        // IF 语句需要 BEGIN/END 才是完整的 T-SQL
        var fullSql = $"{tsql}\r\nbegin\r\n    select 1;\r\nend";

        var fragment = fullSql.ParseToFragment();
        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(fragment);

        // 输出调试信息
        Console.WriteLine($"Input:    {tsql}");
        Console.WriteLine($"Expected: {expected}");
        Console.WriteLine($"Actual:   {result}");

        // 验证包含 THEN
        Assert.Contains("THEN", result);
    }

    [Fact]
    public void ConvertIfBlock_ShouldAddThenAndEndIf()
    {
        // 测试完整的 IF 语句块
        var tsql = @"
if( (select count(1) from #temp_list) <= 0 )
begin
    select @nowDate = getdate();
end
";

        var fragment = tsql.ParseToFragment();
        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(fragment);

        // 验证包含 THEN 和 END IF
        Assert.Contains("THEN", result);
        Assert.Contains("END IF", result);
    }
}
