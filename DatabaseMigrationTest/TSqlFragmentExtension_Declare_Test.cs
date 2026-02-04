using DatabaseMigration.Migration;
using DatabaseMigration.ScriptGenerator;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigrationTest;

public class TSqlFragmentExtension_Declare_Test
{
    [Fact]
    public void 转换包含Declare的语句时_需要将所有declare提取到最前面()
    {
        var sql = @"CREATE procedure [dbo].[a_update_Sys]  
AS
BEGIN
    -- 切换主键
    IF NOT EXISTS( SELECT * from sysobjects where name =( SELECT TOP 1 name FROM sys.indexes  WHERE is_primary_key = 1   AND object_id  = Object_Id('posSmMappingHid') AND name='PK_posSm_20190808912' ) )  
    BEGIN  
    --删除旧的主键  
    DECLARE @pkname varchar(200)  
    SELECT @pkname = (SELECT TOP 1 name FROM sys.indexes  WHERE is_primary_key = 1   AND object_id  = Object_Id('posSmMappingHid'))  
    execute('ALTER TABLE posSmMappingHid DROP CONSTRAINT ' +  @pkname )  
    --新增主键  
    ALTER TABLE posSmMappingHid ADD CONSTRAINT PK_posSm_20190808912 PRIMARY KEY(Id)  
    END  
END";

        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new StringReader(sql), out var errors);
        Assert.Empty(errors);

        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(fragment);

        var expected = @"CREATE OR REPLACE procedure a_update_sys  () 
LANGUAGE plpgsql
AS $$
DECLARE pkname varchar(200);
begin
BEGIN
    -- 切换主键
    IF NOT EXISTS ( select * from pg_class where relname = 'PK_posSm_20190808912' and relkind = 'i' LIMIT 1) THEN 

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

END;

end;
$$;";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void 获取指定存储过程语句中的所有declareItems_只有一个declare的情况()
    {
        var sql = @"CREATE procedure [dbo].[a_update_Sys]  
AS  
BEGIN
     -- 切换主键  
    IF NOT EXISTS( SELECT * from sysobjects where name =( SELECT TOP 1 name FROM sys.indexes  WHERE is_primary_key = 1   AND object_id  = Object_Id('posSmMappingHid') AND name='PK_posSm_20190808912' ) )  
    BEGIN  
    --删除旧的主键  
    DECLARE @pkname varchar(200)  
    SELECT @pkname = (SELECT TOP 1 name FROM sys.indexes  WHERE is_primary_key = 1   AND object_id  = Object_Id('posSmMappingHid'))  
    execute('ALTER TABLE posSmMappingHid DROP CONSTRAINT ' +  @pkname )  
    --新增主键  
    ALTER TABLE posSmMappingHid ADD CONSTRAINT PK_posSm_20190808912 PRIMARY KEY(Id)  
    END  
END";

        var fragment = sql.ParseToFragment();
        var declareItems = fragment.ScriptTokenStream.GetAllDealreItems();

        Assert.Single(declareItems);
        Assert.Equal("pkname", declareItems[0].Name);
        Assert.Equal("varchar(200)", declareItems[0].TypeText);
    }

    [Fact]
    public void 获取指定存储过程语句中的所有declareItems_有两个连续declare的情况()
    {
        var sql = @"CREATE procedure [dbo].[a_update_Sys]  
AS  
BEGIN
     -- 切换主键  
    IF NOT EXISTS( SELECT * from sysobjects where name =( SELECT TOP 1 name FROM sys.indexes  WHERE is_primary_key = 1   AND object_id  = Object_Id('posSmMappingHid') AND name='PK_posSm_20190808912' ) )  
    BEGIN  
    --删除旧的主键  
    DECLARE @pkname varchar(200)
    DECLARE @othername int
    SELECT @pkname = (SELECT TOP 1 name FROM sys.indexes  WHERE is_primary_key = 1   AND object_id  = Object_Id('posSmMappingHid'))  
    execute('ALTER TABLE posSmMappingHid DROP CONSTRAINT ' +  @pkname )  
    --新增主键  
    ALTER TABLE posSmMappingHid ADD CONSTRAINT PK_posSm_20190808912 PRIMARY KEY(Id)  
    END  
END";

        var fragment = sql.ParseToFragment();
        var declareItems = fragment.ScriptTokenStream.GetAllDealreItems();

        Assert.Equal(2,declareItems.Count);
        Assert.Equal("pkname", declareItems[0].Name);
        Assert.Equal("varchar(200)", declareItems[0].TypeText);
        Assert.Equal("othername", declareItems[1].Name);
        Assert.Equal("int", declareItems[1].TypeText);
    }
}
