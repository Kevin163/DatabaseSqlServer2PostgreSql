using DatabaseMigration.Migration;
using DatabaseMigration.ScriptGenerator;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Xunit;
using System;

namespace DatabaseMigrationTest
{
    public class PostgreSqlScriptGenerator_Concat_Tests
    {
        [Fact]
        public void ConvertSetStringConcatenation_ShouldNotIncludeTrailingSemicolon()
        {
            var tsql = "SET @auditextendurl = 'http://pmsnotify.gshis.com/Audit/Index?hid=' +  @hid + '&type=' + @type + '&id=' + @id + '&sign=jxd598Audit';";
            var fragment = tsql.ParseToFragment();
            // Use concrete class since PostgreSqlScriptGenerator is abstract
            var generator = new PostgreSqlProcedureScriptGenerator(); 
            var result = generator.GenerateSqlScript(fragment);
            
            // Expected: auditextendurl := CONCAT('http://pmsnotify.gshis.com/Audit/Index?hid=', hid, '&type=', type, '&id=', id, '&sign=jxd598Audit');
            
            // Should NOT have ';);'
            Assert.DoesNotContain("';);", result);
            Assert.EndsWith("');", result.Trim());
            
            // Should contain CONCAT
            Assert.Contains("CONCAT(", result);
        }
    }
}
