using DatabaseMigration.Migration;
using System.Data;
using Xunit;

namespace DatabaseMigrationTest;

public class MigrationUtils_ConvertToPostgresType_Tests
{
    private static DataRow MakeColumnRow(object charMaxLen = null, object precision = null, object scale = null)
    {
        var dt = new DataTable();
        dt.Columns.Add("CHARACTER_MAXIMUM_LENGTH", typeof(object));
        dt.Columns.Add("NUMERIC_PRECISION", typeof(object));
        dt.Columns.Add("NUMERIC_SCALE", typeof(object));
        var row = dt.NewRow();
        row["CHARACTER_MAXIMUM_LENGTH"] = charMaxLen ?? DBNull.Value;
        row["NUMERIC_PRECISION"] = precision ?? DBNull.Value;
        row["NUMERIC_SCALE"] = scale ?? DBNull.Value;
        dt.Rows.Add(row);
        return row;
    }

    [Fact]
    public void Varchar_WithLength_ReturnsVarcharN()
    {
        var row = MakeColumnRow(50, null, null);
        var res = MigrationUtils.ConvertToPostgresType("varchar", row);
        Assert.Equal("varchar(50)", res);
    }

    [Fact]
    public void Varchar_Max_ReturnsText()
    {
        var row = MakeColumnRow(-1, null, null);
        var res = MigrationUtils.ConvertToPostgresType("varchar", row);
        Assert.Equal("text", res);
    }

    [Fact]
    public void Nvarchar_NullColumn_ReturnsVarchar()
    {
        var res = MigrationUtils.ConvertToPostgresType("nvarchar", null);
        Assert.Equal("varchar", res);
    }

    [Fact]
    public void Char_WithLength_ReturnsCharN()
    {
        var row = MakeColumnRow(10, null, null);
        var res = MigrationUtils.ConvertToPostgresType("char", row);
        Assert.Equal("char(10)", res);
    }

    [Fact]
    public void Decimal_WithPrecisionAndScale_ReturnsNumericWithParams()
    {
        var row = MakeColumnRow(null, 10, 2);
        var res = MigrationUtils.ConvertToPostgresType("decimal", row);
        Assert.Equal("numeric(10, 2)", res);
    }

    [Fact]
    public void Decimal_WithoutPrecision_ReturnsNumeric()
    {
        var row = MakeColumnRow(null, DBNull.Value, DBNull.Value);
        var res = MigrationUtils.ConvertToPostgresType("decimal", row);
        Assert.Equal("numeric", res);
    }

    [Theory]
    [InlineData("bit", "boolean")]
    [InlineData("tinyint", "smallint")]
    [InlineData("binary", "bytea")]
    [InlineData("varbinary", "bytea")]
    [InlineData("image", "bytea")]
    [InlineData("datetimeoffset", "timestamptz")]
    [InlineData("uniqueidentifier", "uuid")]
    [InlineData("int", "integer")]
    [InlineData("float", "double precision")]
    [InlineData("money", "money")]
    public void CommonTypes_MapCorrectly(string sqlType, string expected)
    {
        var res = MigrationUtils.ConvertToPostgresType(sqlType, null);
        Assert.Equal(expected, res);
    }

    [Fact]
    public void UnknownType_FallsBackToText()
    {
        var res = MigrationUtils.ConvertToPostgresType("some_weird_type", null);
        Assert.Equal("text", res);
    }

    [Fact]
    public void Varchar_WithInlineLength_ReturnsVarchar200()
    {
        var res = MigrationUtils.ConvertToPostgresType("varchar(200)", null);
        Assert.Equal("varchar(200)", res);
    }
}
