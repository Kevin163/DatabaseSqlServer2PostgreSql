using DatabaseMigration.Migration;
using DatabaseMigration.ScriptGenerator;
using Xunit;

namespace DatabaseMigrationTest;

/// <summary>
/// 测试 SELECT ... UNION ALL ... SELECT 语法转换
/// </summary>
public class PostgreSqlScriptGenerator_UnionAll_Tests
{
    [Fact]
    public void ConvertSelectUnionAll_ShouldNotHaveSemicolonBeforeUnionAll()
    {
        // 测试 UNION ALL 前面不应该有分号
        var tsql = @"
SELECT 'Main' id,'捷信达中央物品分类管理' name,'' parentid,0 seqid,0 checked
UNION ALL
SELECT id,name,'Main' parentid ,seqid,0 checked FROM itemcategory WHERE parentid='0'
UNION all
SELECT id,name, parentid ,seqid,0 checked FROM itemcategory WHERE parentid<>'0'
ORDER BY seqid
";

        var fragment = tsql.ParseToFragment();
        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(fragment);

        // 输出调试信息
        Console.WriteLine($"Result: {result}");

        // 验证 UNION ALL 前面没有分号
        Assert.DoesNotContain(";UNION", result);
        Assert.DoesNotContain("; UNION", result);
        
        // 验证 UNION ALL 存在
        Assert.Contains("UNION ALL", result, StringComparison.OrdinalIgnoreCase);
        
        // 验证只有一个分号（在语句末尾）
        var semicolonCount = result.Count(c => c == ';');
        Assert.Equal(1, semicolonCount);
    }

    [Theory]
    [InlineData("SELECT 1 UNION ALL SELECT 2")]
    [InlineData("SELECT a FROM t1 UNION SELECT b FROM t2")]
    public void ConvertSimpleUnion_ShouldNotHaveSemicolonBeforeUnion(string tsql)
    {
        var fragment = tsql.ParseToFragment();
        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(fragment);

        Console.WriteLine($"Input: {tsql}");
        Console.WriteLine($"Result: {result}");

        // 验证 UNION 前面没有分号
        Assert.DoesNotContain(";UNION", result);
        Assert.DoesNotContain("; UNION", result);
    }
}
