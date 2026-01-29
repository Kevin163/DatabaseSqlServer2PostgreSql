using DatabaseMigration.ScriptGenerator;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigrationTest;

public class TSqlFragmentExtension_ExecSql_Test
{
    [Fact]
    public void IsExecSqlStringInParenthesis_BasicExecString_ReturnsSqlString()
    {
        var sql = "exec('update helpFiles set showStatus=checkStatus where 1=1');";
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);
        Assert.Empty(errors);

        var ok = fragment.ScriptTokenStream.IsExecSqlStringInParenthesis(out var inner);
        Assert.True(ok);
        Assert.Equal("update helpFiles set showStatus=checkStatus where 1=1", inner);
    }

    [Fact]
    public void IsExecSqlStringInParenthesis_VersionParasExecString_ReturnsSqlString()
    {
        var sql = "exec('update versionParas set vProduct=''pms'' where vProduct is null')";
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);
        Assert.Empty(errors);

        var ok = fragment.ScriptTokenStream.IsExecSqlStringInParenthesis(out var inner);
        Assert.True(ok);
        Assert.Equal("update versionParas set vProduct='pms' where vProduct is null", inner);
    }

    [Fact]
    public void IsExecSqlStringInParenthesis_FaceDevicesExecString_ReturnsSqlString()
    {
        var sql = "EXEC('update FaceDevices set DeviceType = ''人脸''')";
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);
        Assert.Empty(errors);

        var ok = fragment.ScriptTokenStream.IsExecSqlStringInParenthesis(out var inner);
        Assert.True(ok);
        Assert.Equal("update FaceDevices set DeviceType = '人脸'", inner);
    }
}
