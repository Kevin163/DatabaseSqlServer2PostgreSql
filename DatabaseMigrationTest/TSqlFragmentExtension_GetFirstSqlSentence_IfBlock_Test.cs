using DatabaseMigration.Migration;
using DatabaseMigration.ScriptGenerator;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigrationTest;

/// <summary>
/// MigrationUtils获取第一个SQL语句单元的测试类，专注于IF块场景。
/// </summary>
public class TSqlFragmentExtension_GetFirstSqlSentence_IfBlock_Test
{

    /// <summary>
    /// 场景：IF NOT EXISTS (...) BEGIN ... END; 且后面还有其他 IF 块。
    /// 期望：应返回第一个 IF ... BEGIN ... END 块作为 firstSql。
    /// </summary>
    [Fact]
    public void GetFirstCompleteSqlSentence_FirstIfBeginEndBlock_ReturnsFirstIfBlock()
    {
        var sql = @"IF NOT EXISTS(SELECT * FROM syscolumns WHERE  id = OBJECT_ID('dbList') AND name = 'readonlyDbServer')

BEGIN

    ALTER TABLE dbList ADD readonlyDbServer VARCHAR(200)

    ALTER TABLE dbList ADD readonlyDbName VARCHAR(200)

    ALTER TABLE dbList ADD readonlyLogId VARCHAR(30)

    ALTER TABLE dbList ADD readonlyLogPwd VARCHAR(30)

END

--修改pos设备表，另外增加一个id来做为主键，原来的设备编号也允许进行修改

IF NOT EXISTS(SELECT * FROM syscolumns WHERE ID = OBJECT_ID('HotelPos') AND name = 'Id')

BEGIN

    ALTER TABLE HotelPos ADD ID UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()

    ALTER TABLE HotelPos DROP CONSTRAINT pk_hotelPos

    ALTER TABLE hotelPos ADD CONSTRAINT pk_hotelPos PRIMARY KEY(ID)

END";

        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);

        Assert.Empty(errors);

        int startIndex = 0;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"IF NOT EXISTS(SELECT * FROM syscolumns WHERE  id = OBJECT_ID('dbList') AND name = 'readonlyDbServer')

BEGIN

    ALTER TABLE dbList ADD readonlyDbServer VARCHAR(200)

    ALTER TABLE dbList ADD readonlyDbName VARCHAR(200)

    ALTER TABLE dbList ADD readonlyLogId VARCHAR(30)

    ALTER TABLE dbList ADD readonlyLogPwd VARCHAR(30)

END";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count, startIndex);
    }

    /// <summary>
    /// 新增测试：复杂的 IF EXISTS 包含子查询和 UNION ALL，应该作为第一个完整语句返回（包含 BEGIN/END 内容）。
    /// </summary>
    [Fact]
    public void GetFirstCompleteSqlSentence_ComplexIfExistsWithSubquery_ReturnsFirstIfBlock()
    {
        var sql = @"if exists(select distinct * from (  
    select hotelCode as hid from dbo.posSmMappingHid  
    union all  
    select groupid from dbo.posSmMappingHid)a  
    where ISNULL(a.hid,'')!='' and hid not in(select hid from dbo.hotelProducts where productCode='ipos'))  
begin  
    insert into hotelProducts(hid,productCode)  
    select distinct a.hid,'ipos' from (  
    select hotelCode as hid from dbo.posSmMappingHid  
    union all  
    select groupid from dbo.posSmMappingHid)a  
    where ISNULL(a.hid,'')!='' and hid not in(select hid from dbo.hotelProducts where productCode='ipos')  
end  
if not exists(select * from INFORMATION_SCHEMA.columns where table_name='posSmMappingHid' and column_name = 'memberVersion')  
begin  
 ALTER TABLE posSmMappingHid add memberVersion varchar(10) null,memberInternetUrl varchar(200) null,BsPmsGrpId varchar(6) null,BsPmsChannelCode varchar(30),BsPmsChannelKey varchar(60)  
end  ";

        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);

        Assert.Empty(errors);

        int startIndex = 0;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"if exists(select distinct * from (  
    select hotelCode as hid from dbo.posSmMappingHid  
    union all  
    select groupid from dbo.posSmMappingHid)a  
    where ISNULL(a.hid,'')!='' and hid not in(select hid from dbo.hotelProducts where productCode='ipos'))  
begin  
    insert into hotelProducts(hid,productCode)  
    select distinct a.hid,'ipos' from (  
    select hotelCode as hid from dbo.posSmMappingHid  
    union all  
    select groupid from dbo.posSmMappingHid)a  
    where ISNULL(a.hid,'')!='' and hid not in(select hid from dbo.hotelProducts where productCode='ipos')  
end";

        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count, startIndex);
    }

    /// <summary>
    /// 新增测试：识别 IF NOT EXISTS(SELECT * FROM helpFiles WHERE code = 'pms') BEGIN UPDATE helpFiles SET code='pms' ... END
    /// 并提取出表名与 WHERE 中的 code 值
    /// </summary>
    [Fact]
    public void IsIfNotExistsSelectFromTableWhereColumnEqualValueCommon_HelpFilesCodePms_ParsesCorrectly()
    {
        var sql = @"IF NOT EXISTS(SELECT * FROM helpFiles WHERE code = 'pms')
BEGIN
  UPDATE helpFiles SET code='pms' WHERE title LIKE'%客房%'
END";

        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);

        Assert.Empty(errors);

        var ok = fragment.ScriptTokenStream.IsIfNotExistsSelectFromTableWhereColumnEqualValueCommon(out var tableName, out var columns);
        Assert.True(ok);
        Assert.False(string.IsNullOrEmpty(tableName));
        Assert.Equal("helpFiles".ToLowerInvariant(), tableName.ToLowerInvariant());
        Assert.True(columns.ContainsKey("code"));
        Assert.Equal("pms", columns["code"]);
    }
    /// <summary>
    /// 新增测试：识别 IF NOT EXISTS(SELECT * FROM helpFiles WHERE code = 'pms') BEGIN UPDATE helpFiles SET code='pms' ... END
    /// 并提取出表名与 WHERE 中的 code 值
    /// </summary>
    [Fact]
    public void IsIfNotExistsSelectFromTableWhereColumnEqualValueCommon_HelpFilesCodePms_ConvertCorrectly()
    {
        var sql = @"IF NOT EXISTS(SELECT * FROM helpFiles WHERE code = 'pms')
BEGIN
  UPDATE helpFiles SET code='pms' WHERE title LIKE'%客房%'
END";

        var frag = sql.ParseToFragment();
        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(frag.ScriptTokenStream);

        var expected = @"IF NOT EXISTS ( SELECT 1 FROM helpfiles WHERE code = 'pms') THEN 

  UPDATE helpfiles SET code='pms' WHERE title LIKE'%客房%';
 END IF;
";
        Assert.Equal(expected, result);
    }
}
