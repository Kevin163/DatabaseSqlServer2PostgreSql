using DatabaseMigration.Migration;
using DatabaseMigration.ScriptGenerator;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Xunit;

namespace DatabaseMigrationTest
{
    /// <summary>
    /// Tests for IF/ELSE block conversion
    /// </summary>
    public class PostgreSqlScriptGenerator_IfElse_Tests
    {
        [Fact]
        public void ConvertIfElseBlock_WithBeginEnd_ShouldConvertCorrectly()
        {
            // Arrange
            var sourceSql = @"
if(@isTest = 1)
begin
    select top 1 
    conn = 'data source=' + dbServer + ';initial catalog=' + dbName + ';user id=jxd;password=jxd598;'
    from dbList with(nolock)
    where id = @databaseId;
end
ELSE
BEGIN
    select top 1 
    conn = 'data source=' + dbServer + ';initial catalog=' + dbName + ';user id='+logid+';password=jxd;'
    from dbList with(nolock)
    where id = @databaseId;
END";

            // Parse
            var fragment = sourceSql.ParseToFragment();

            // Act
            var generator = new PostgreSqlProcedureScriptGenerator();
            var result = generator.GenerateSqlScript(fragment);

            // Assert
            Console.WriteLine("Generated SQL:");
            Console.WriteLine(result);

            // Should have proper IF/ELSE structure
            Assert.Contains("IF", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("THEN", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ELSE", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("END IF", result, StringComparison.OrdinalIgnoreCase);
            
            // Should convert both SELECT statements
            Assert.Contains("LIMIT 1", result, StringComparison.OrdinalIgnoreCase);
            
            // Should not have SQL Server syntax
            Assert.DoesNotContain("top 1", result, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("with(nolock)", result, StringComparison.OrdinalIgnoreCase);
            
            // Should not have multiple consecutive BEGINs
            Assert.DoesNotContain("BEGINBEGIN", result, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ConvertIfElseBlock_WithStringConcat_ShouldConvertBothBranches()
        {
            // Arrange
            var sourceSql = @"
if(@isTest = 1)
begin
    select 'prefix' + dbServer + 'suffix' as conn;
end
ELSE
BEGIN
    select 'other' + logid + 'value' as conn;
END";

            // Parse
            var fragment = sourceSql.ParseToFragment();

            // Act
            var generator = new PostgreSqlProcedureScriptGenerator();
            var result = generator.GenerateSqlScript(fragment);

            // Assert
            Console.WriteLine("Generated SQL:");
            Console.WriteLine(result);

            // ELSE branch should be converted (not raw SQL Server syntax)
            Assert.Contains("ELSE", result, StringComparison.OrdinalIgnoreCase);
            // Both branches should have string concatenation converted
            int concatCount = System.Text.RegularExpressions.Regex.Matches(result, @"\|\|", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;
            Assert.True(concatCount >= 2, "Should have converted concatenations in both branches");
        }

        [Fact]
        public void ConvertSelectWithColumnAlias_ShouldHaveSpaceBeforeFromKeyword()
        {
            // Arrange - this is the exact pattern that was reported as broken
            var sourceSql = @"
select 
conn = 'data source=' + dbServer + ';initial catalog=' + dbName + ';user id=jxd;'
from dbList
where id = @databaseId;";

            // Parse
            var fragment = sourceSql.ParseToFragment();

            // Act
            var generator = new PostgreSqlProcedureScriptGenerator();
            var result = generator.GenerateSqlScript(fragment);

            // Assert
            Console.WriteLine("Generated SQL:");
            Console.WriteLine(result);

            // Should NOT have "connfrom" (missing space)
            Assert.DoesNotContain("connfrom", result, StringComparison.OrdinalIgnoreCase);
            
            // Should have proper spacing: "AS conn" followed by space/newline then "from"
            // We check for the presence of " from" or "\nfrom" after "conn"
            Assert.True(
                result.Contains("conn from", StringComparison.OrdinalIgnoreCase) ||
                result.Contains("conn\nfrom", StringComparison.OrdinalIgnoreCase) ||
                result.Contains("conn\r\nfrom", StringComparison.OrdinalIgnoreCase) ||
                result.Contains("conn FROM", StringComparison.OrdinalIgnoreCase),
                $"Should have proper space before FROM keyword. Actual result: {result}");
        }
    }
}
