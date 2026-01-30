using DatabaseMigration.ScriptGenerator;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigrationTest;

public class TSqlFragmentExtension_AlterSql_Test
{
    [Fact]
    public void IsAlterTableAddColumn_Basic_AddsColumnParsed()
    {
        var sql = "alter table dbo.helpFiles add showStatus bit null";
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);
        Assert.Empty(errors);

        var ok = TSqlFragmentExtension_AlterSql.IsAlterTableAddColumn(fragment.ScriptTokenStream, out var table, out var columnItems);
        Assert.True(ok);
        Assert.Equal("helpfiles", table);
        Assert.Equal("showstatus", columnItems[0].Name);
        Assert.Equal("boolean", columnItems[0].DataTypeDefine.DataType);
        Assert.True(columnItems[0].DataTypeDefine.IsNullable);
        Assert.False(columnItems[0].DataTypeDefine.IsPrimaryKey);
        Assert.Null(columnItems[0].DataTypeDefine.DefaultValue);
    }

    [Fact]
    public void IsAlterTableAlterColumn_Basic_AlterColumnParsed()
    {
        var sql = "alter table dbo.helpFiles alter column showStatus bit not null";
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);
        Assert.Empty(errors);

        var ok = TSqlFragmentExtension_AlterSql.IsAlterTableAlterColumn(fragment.ScriptTokenStream, out var table, out var columnDefine);
        Assert.True(ok);
        Assert.Equal("helpfiles", table);
        Assert.Equal("showstatus", columnDefine.Name);
        Assert.Equal("boolean", columnDefine.DataTypeDefine.DataType);
        Assert.False(columnDefine.DataTypeDefine.IsNullable);
        Assert.False(columnDefine.DataTypeDefine.IsPrimaryKey);
        Assert.Null(columnDefine.DataTypeDefine.DefaultValue);
    }

    [Fact]
    public void IsAlterTableDropConstraint_Basic_DropConstraintParsed()
    {
        var sql = "ALTER TABLE HotelPos DROP CONSTRAINT pk_hotelPos";
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);
        Assert.Empty(errors);

        var ok = TSqlFragmentExtension_AlterSql.IsAlterTableDropConstraint(fragment.ScriptTokenStream, out var table, out var constraint);
        Assert.True(ok);
        Assert.Equal("hotelpos", table);
        Assert.Equal("pk_hotelpos", constraint);
    }

    [Fact]
    public void IsAlterTableAddConstraintPrimaryKey_Basic_AddsPrimaryKeyParsed()
    {
        var sql = "ALTER TABLE hotelPos ADD CONSTRAINT pk_hotelPos PRIMARY KEY(ID)";
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);
        Assert.Empty(errors);

        var ok = TSqlFragmentExtension_AlterSql.IsAlterTableAddConstraintPrimaryKey(fragment.ScriptTokenStream, out var table, out var constraint, out var cols);
        Assert.True(ok);
        Assert.Equal("hotelpos", table);
        Assert.Equal("pk_hotelpos", constraint);
        Assert.Equal("id", cols);
    }

    [Fact]
    public void GenerateSqlScript_PostgreProcedure_AlterTableAdd_IsHide_MigratedToPostgres()
    {
        var sql = @"ALTER TABLE dbo.sysPara 
ADD IsHide BIT";
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);
        Assert.Empty(errors);

        var generator = new PostgreSqlProcedureScriptGenerator();
        var migrated = generator.GenerateSqlScript(fragment);
        Assert.Equal("ALTER TABLE syspara ADD ishide boolean;", migrated);
    }

    [Fact]
    public void GenerateSqlScript_PostgreProcedure_IfNotExists_AddIdAndPk_MigratedToPostgres()
    {
        var sql = @"IF NOT EXISTS(SELECT * FROM syscolumns WHERE ID = OBJECT_ID('HotelPos') AND name = 'Id')  
BEGIN  
    ALTER TABLE HotelPos ADD ID UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()
    ALTER TABLE HotelPos DROP CONSTRAINT pk_hotelPos
    ALTER TABLE hotelPos ADD CONSTRAINT pk_hotelPos PRIMARY KEY(ID)
END";

        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);
        Assert.Empty(errors);

        var generator = new PostgreSqlProcedureScriptGenerator();
        var migrated = generator.GenerateSqlScript(fragment);

        var expected = @"IF NOT EXISTS ( SELECT 1 FROM information_schema.columns WHERE table_name = 'hotelpos' AND column_name = 'id') THEN 
  
    ALTER TABLE hotelpos ADD id uuid not null;
    ALTER TABLE hotelpos DROP CONSTRAINT pk_hotelpos;
    ALTER TABLE hotelpos ADD CONSTRAINT pk_hotelpos PRIMARY KEY (id);
 END IF;
";

        Assert.Equal(expected, migrated);
    }

    [Fact]
    public void GenerateSqlScript_PostgreProcedure_IfNotExists_HotelPos_AddIdAndPk_MigratedToPostgres()
    {
        var sql = @"IF NOT EXISTS(SELECT * FROM syscolumns WHERE ID = OBJECT_ID('HotelPos') AND name = 'Id')  
BEGIN  
    ALTER TABLE HotelPos ADD ID UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()
    ALTER TABLE HotelPos DROP CONSTRAINT pk_hotelPos
    ALTER TABLE hotelPos ADD CONSTRAINT pk_hotelPos PRIMARY KEY(ID)
END";

        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);
        Assert.Empty(errors);

        var generator = new PostgreSqlProcedureScriptGenerator();
        var migrated = generator.GenerateSqlScript(fragment);

        var expected = @"IF NOT EXISTS ( SELECT 1 FROM information_schema.columns WHERE table_name = 'hotelpos' AND column_name = 'id') THEN 
  
    ALTER TABLE hotelpos ADD id uuid not null;
    ALTER TABLE hotelpos DROP CONSTRAINT pk_hotelpos;
    ALTER TABLE hotelpos ADD CONSTRAINT pk_hotelpos PRIMARY KEY (id);
 END IF;
";

        Assert.Equal(expected, migrated);
    }

    [Fact]
    public void GenerateSqlScript_PostgreProcedure_IfNotExists_AddReadonlyColumns_MigratedToPostgres()
    {
        var sql = @"IF NOT EXISTS(SELECT * FROM syscolumns WHERE  id = OBJECT_ID('dbList') AND name = 'readonlyDbServer')  
BEGIN  
    ALTER TABLE dbList ADD readonlyDbServer VARCHAR(200)
    ALTER TABLE dbList ADD readonlyDbName VARCHAR(200)
    ALTER TABLE dbList ADD readonlyLogId VARCHAR(30)
    ALTER TABLE dbList ADD readonlyLogPwd VARCHAR(30)
END";

        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);
        Assert.Empty(errors);

        var generator = new PostgreSqlProcedureScriptGenerator();
        var migrated = generator.GenerateSqlScript(fragment);

        var expected = @"IF NOT EXISTS ( SELECT 1 FROM information_schema.columns WHERE table_name = 'dblist' AND column_name = 'readonlydbserver') THEN 
  
    ALTER TABLE dblist ADD readonlydbserver varchar(200);
    ALTER TABLE dblist ADD readonlydbname varchar(200);
    ALTER TABLE dblist ADD readonlylogid varchar(30);
    ALTER TABLE dblist ADD readonlylogpwd varchar(30);
 END IF;
";

        Assert.Equal(expected, migrated);
    }

    [Fact]
    public void IsAlterTableAddColumn_MultipleColumns_FirstParsed()
    {
        var sql = "alter table hotelpos add isQtyControl bit not null default 0, qty int null";
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);
        Assert.Empty(errors);

        var ok = TSqlFragmentExtension_AlterSql.IsAlterTableAddColumn(fragment.ScriptTokenStream, out var table, out var columnItems);
        Assert.True(ok);
        Assert.Equal("hotelpos", table);
        Assert.Equal("isqtycontrol", columnItems[0].Name);
        Assert.Equal("boolean", columnItems[0].DataTypeDefine.DataType);
        Assert.False(columnItems[0].DataTypeDefine.IsNullable);
        Assert.Equal("qty", columnItems[1].Name);
        Assert.Equal("integer", columnItems[1].DataTypeDefine.DataType);
        Assert.True(columnItems[1].DataTypeDefine.IsNullable);
    }

    [Fact]
    public void IsAlterTableAddColumn_UniqueIdentifier_AddsUuidParsed()
    {
        var sql = "ALTER TABLE HotelPos ADD ID UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()";
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);
        Assert.Empty(errors);

        var ok = TSqlFragmentExtension_AlterSql.IsAlterTableAddColumn(fragment.ScriptTokenStream, out var table, out var columnItems);
        Assert.True(ok);
        Assert.Equal("hotelpos", table);
        Assert.Equal("id", columnItems[0].Name);
        Assert.Equal("uuid", columnItems[0].DataTypeDefine.DataType);
        Assert.False(columnItems[0].DataTypeDefine.IsNullable);
    }

    [Fact]
    public void IsAlterTableAddColumn_VarcharWithDefault_NotNull_Parsed()
    {
        var sql = "ALTER TABLE hotel ADD customerStatus VARCHAR(2) NOT NULL DEFAULT '0'";
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);
        Assert.Empty(errors);

        var ok = TSqlFragmentExtension_AlterSql.IsAlterTableAddColumn(fragment.ScriptTokenStream, out var table, out var columnItems);
        Assert.True(ok);
        Assert.Equal("hotel", table);
        Assert.Equal("customerstatus", columnItems[0].Name);
        Assert.Equal("varchar(2)", columnItems[0].DataTypeDefine.DataType);
        Assert.False(columnItems[0].DataTypeDefine.IsNullable);
    }

    [Fact]
    public void GenerateSqlScript_PostgreProcedure_IfNotExists_AddBitColumnWithDefaultZero_MigratedToPostgres()
    {
        var sql = @"IF NOT EXISTS(SELECT * FROM syscolumns WHERE id = OBJECT_ID('dbList') AND name = 'isDefalut')
BEGIN
  ALTER TABLE dbo.dbList
ADD isDefalut BIT NOT NULL DEFAULT(0)
END";

        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);
        Assert.Empty(errors);

        var generator = new PostgreSqlProcedureScriptGenerator();
        var migrated = generator.GenerateSqlScript(fragment);

        var expected = @"IF NOT EXISTS ( SELECT 1 FROM information_schema.columns WHERE table_name = 'dblist' AND column_name = 'isdefalut') THEN 

  ALTER TABLE dblist ADD isdefalut boolean not null DEFAULT false;
 END IF;
";

        Assert.Equal(expected, migrated);
    }

    [Fact]
    public void IsAlterTableAddColumn_BitWithDefaultZeroInParentheses_DefaultParsed()
    {
        var sql = "ALTER TABLE dbo.dbList ADD isDefalut BIT NOT NULL DEFAULT(0)";
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);
        Assert.Empty(errors);

        var ok = TSqlFragmentExtension_AlterSql.IsAlterTableAddColumn(fragment.ScriptTokenStream, out var table, out var columnItems);
        Assert.True(ok);
        Assert.Equal("dblist", table);
        Assert.Equal("isdefalut", columnItems[0].Name);
        Assert.Equal("boolean", columnItems[0].DataTypeDefine.DataType);
        Assert.False(columnItems[0].DataTypeDefine.IsNullable);
        Assert.Equal("false", columnItems[0].DataTypeDefine.DefaultValue); // 应该提取到默认值 0，并且转换为false
    }
}
