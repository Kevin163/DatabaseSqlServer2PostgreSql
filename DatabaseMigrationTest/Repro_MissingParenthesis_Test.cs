using DatabaseMigration.ScriptGenerator;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Xunit;

namespace DatabaseMigrationTest;

public class Repro_MissingParenthesis_Test
{
    [Fact]
    public void ConvertProcedure_WithVarcharParam_ShouldIncludeClosingParenthesis()
    {
        var sql = @"create PROC [dbo].[GetHotelRoleList]  
@hotelid VARCHAR(100)  
AS  
SELECT  RoleID,HotelID HotelRoleID,CASE WHEN RoleID LIKE '%s%' THEN 'SCM' ELSE 'DEPOT' END  SysType FROM dbo.HotelRole WHERE HotelID=@hotelid";

        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);

        Assert.Empty(errors);

        var converter = new PostgreSqlProcedureScriptGenerator();
        var result = converter.GenerateSqlScript(fragment);
        
        // Expected output should have closing parenthesis after varchar(100)
        // and before LANGUAGE plpgsql
        // Note: The generator preserves original whitespace, so we get extra spaces
        Assert.Contains("procedure gethotelrolelist  (hotelid  varchar(100)  )", result.Replace("\r\n", " ").Replace("\n", " ").Replace("\t", ""));
    }
}
