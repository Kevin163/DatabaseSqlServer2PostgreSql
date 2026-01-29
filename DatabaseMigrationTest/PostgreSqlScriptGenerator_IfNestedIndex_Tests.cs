using DatabaseMigration.ScriptGenerator;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigrationTest;

public class PostgreSqlScriptGenerator_IfNestedIndex_Tests
{
    [Fact]
    public void IsIfNotExistsSysobjectsNameEqualNestedIndexSelect_Match()
    {
        var sql = @"IF NOT EXISTS( SELECT * from sysobjects where name =( SELECT TOP 1 name FROM sys.indexes  WHERE is_primary_key = 1   AND object_id  = Object_Id('posSmMappingHid') AND name='PK_posSm_20190808912' ) )  
BEGIN  
--删除旧的主键  
DECLARE @pkname varchar(200)  
SELECT @pkname = (SELECT TOP 1 name FROM sys.indexes  WHERE is_primary_key = 1   AND object_id  = Object_Id('posSmMappingHid'))  
execute('ALTER TABLE posSmMappingHid DROP CONSTRAINT ' +  @pkname )  
--新增主键  
ALTER TABLE posSmMappingHid ADD CONSTRAINT PK_posSm_20190808912 PRIMARY KEY(Id)  
END";

        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);
        Assert.Empty(errors);

        var isMatch = fragment.ScriptTokenStream.IsIfNotExistsSysobjectsNameEqualNestedIndexSelect(out var tableName,out var indexName);

        Assert.True(isMatch);
        Assert.Equal("possmmappinghid", tableName);
        Assert.Equal("PK_posSm_20190808912", indexName);
    }
    [Fact]
    public void Convert_IfNestedIndexToPlpgsql_DoBlock()
    {
        var sql = @"IF NOT EXISTS( SELECT * from sysobjects where name =( SELECT TOP 1 name FROM sys.indexes  WHERE is_primary_key = 1   AND object_id  = Object_Id('posSmMappingHid') AND name='PK_posSm_20190808912' ) )  
BEGIN  
--删除旧的主键  
DECLARE @pkname varchar(200)  
SELECT @pkname = (SELECT TOP 1 name FROM sys.indexes  WHERE is_primary_key = 1   AND object_id  = Object_Id('posSmMappingHid'))  
execute('ALTER TABLE posSmMappingHid DROP CONSTRAINT ' +  @pkname )  
--新增主键  
ALTER TABLE posSmMappingHid ADD CONSTRAINT PK_posSm_20190808912 PRIMARY KEY(Id)  
END";

        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);
        Assert.Empty(errors);

        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(fragment.ScriptTokenStream);

        var expected = @"IF NOT EXISTS ( select * from pg_class where relname = 'PK_posSm_20190808912' and relkind = 'i' LIMIT 1) THEN 

    -- 查找当前表的主键约束名
    SELECT conname INTO pkname
      FROM pg_constraint c
      JOIN pg_class t ON c.conrelid = t.oid
     WHERE c.contype = 'p' AND t.relname = 'possmmappinghid'
     LIMIT 1;

    IF pkname IS NOT NULL THEN
        EXECUTE format('ALTER TABLE %I DROP CONSTRAINT %I', 'possmmappinghid', pkname);
    END IF;

    -- 新增主键
    EXECUTE format('ALTER TABLE %I ADD CONSTRAINT %I PRIMARY KEY(%I)','possmmappinghid', 'PK_posSm_20190808912', 'id');

 END IF;
";

        Assert.Equal(expected, result);
    }
}
