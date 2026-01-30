using DatabaseMigration.ScriptGenerator;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigrationTest;

public class TSqlFragmentExtension_CreateProcedure_Test
{
    [Fact]
    public void ConvertProcedureToPostgreSql_ComplexSelectView_ReturnsConvertedSql()
    {
        var sql = @"CREATE procedure [dbo].[a_update_Sys]  
as
/****************************************************************************  
作者：陈提见  
日期：2016-05-7  
功能：命名成这样是为了这个最常用的存储过程排序在最前面  
  
这个存储过程的作用是为了程序启用后用来更改数据库结构或加入一些固定数据，例如系统参数，权限列表等。  
   
exec a_update_Sys  
****************************************************************************/  
 DELETE FROM dbo.sysLog WHERE cDate< DATEADD(DAY,-30,GETDATE())  
 DELETE FROM dbo.slowlog WHERE logTime< DATEADD(DAY,-30,GETDATE())  
    
  
/* 初始化菜单开始 */   
  
 begin --初始化菜单  
     
  delete authlist  
  delete AuthButtons  
  --DELETE AuthButtons   
  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,mask) values('0','1','捷信达捷云系统运营管理',1,'1001000000')  
    
  --运营平台一级菜单放在一起重新排序  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','10','版本列表',180,'','VersionList','Index','1001000000','material-icon')  
        insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','11','产品版本管理',140,'','ProductVersionList','Index','1001000000','material-icon')  
  
  DELETE roleauth WHERE authcode NOT IN (SELECT authcode FROM authlist)  
  
 end  
   
/* 初始化菜单结束 */   
   
  
--增加数据库实例的只读连接信息，陈前良，2018-10-17 11:32:27  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE  id = OBJECT_ID('dbList') AND name = 'readonlyDbServer')  
BEGIN  
    ALTER TABLE dbList ADD readonlyDbServer VARCHAR(200)  
    ALTER TABLE dbList ADD readonlyDbName VARCHAR(200)  
    ALTER TABLE dbList ADD readonlyLogId VARCHAR(30)  
    ALTER TABLE dbList ADD readonlyLogPwd VARCHAR(30)  
END";

        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);

        Assert.Empty(errors);

        var converter = new PostgreSqlProcedureScriptGenerator();
        var result = converter.GenerateSqlScript(fragment);

        var expected = @"CREATE OR REPLACE procedure a_update_sys  () 
LANGUAGE plpgsql
as $$

/****************************************************************************  
作者：陈提见  
日期：2016-05-7  
功能：命名成这样是为了这个最常用的存储过程排序在最前面  
  
这个存储过程的作用是为了程序启用后用来更改数据库结构或加入一些固定数据，例如系统参数，权限列表等。  
   
exec a_update_Sys  
****************************************************************************/  
 DELETE FROM dbo.sysLog WHERE cDate< NOW() - INTERVAL '30 day';  
 DELETE FROM dbo.slowlog WHERE logTime< NOW() - INTERVAL '30 day';  
    
  
/* 初始化菜单开始 */   
  
 BEGIN --初始化菜单  
     
  DELETE FROM authlist;  
  DELETE FROM AuthButtons;  
  --DELETE AuthButtons   
  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,mask) values('0','1','捷信达捷云系统运营管理',1,'1001000000');  
    
  --运营平台一级菜单放在一起重新排序  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,action,mask,class) values('1','10','版本列表',180,'','VersionList','Index','1001000000','material-icon');  
        insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,action,mask,class) values('1','11','产品版本管理',140,'','ProductVersionList','Index','1001000000','material-icon');  
  
  DELETE FROM roleauth WHERE authcode NOT IN (SELECT authcode FROM authlist);
END;
  
   
/* 初始化菜单结束 */   
   
  
--增加数据库实例的只读连接信息，陈前良，2018-10-17 11:32:27  
IF NOT EXISTS ( SELECT 1 FROM information_schema.columns WHERE table_name = 'dblist' AND column_name = 'readonlydbserver') THEN 
  
    ALTER TABLE dblist ADD readonlydbserver varchar(200);  
    ALTER TABLE dblist ADD readonlydbname varchar(200);  
    ALTER TABLE dblist ADD readonlylogid varchar(30);  
    ALTER TABLE dblist ADD readonlylogpwd varchar(30);
 END IF;

$$;";
        Assert.Equal(expected, result);
    }
    [Fact]
    public void ConvertProcedureToPostgreSql_ViewWithQuotation_ReturnsConvertedSql()
    {
        var sql = @"  
CREATE view v_templateIdCloseStatus  
as  
(  
 select   
 'xWv-ZHvbvyGOqbKFHyfCCjmmJmDHyS6wm_O81lgImd4' as templateId,--模板ID  
 '维修单通知' as templateName,--模板名称  
 '0' as [status]--是否启用（1：启用此规则，0：禁用此规则）  
  
 UNION SELECT  'wV6PUtNF2D3klC4x-tw-AGmbdbUXkpszVXUTgjpxFHc','审批状态变更通知','0'  
 UNION SELECT  'ul8w_ASaz5CQODS6swFhnhhcDCRD_gTmZz6H2wzNa4s','预约状态提醒','0'  
)";

        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);

        Assert.Empty(errors);

        var converter = new PostgreSqlViewScriptGenerator();
        var result = converter.GenerateSqlScript(fragment);

        var expected = @"  
CREATE OR REPLACE  view v_templateIdCloseStatus  
as
  
(  
 select   
 'xWv-ZHvbvyGOqbKFHyfCCjmmJmDHyS6wm_O81lgImd4' as templateid,--模板ID  
 '维修单通知' as templatename,--模板名称  
 '0' as status--是否启用（1：启用此规则，0：禁用此规则）  
  
 UNION SELECT  'wV6PUtNF2D3klC4x-tw-AGmbdbUXkpszVXUTgjpxFHc' AS templateid,'审批状态变更通知' AS templatename,'0'   AS status
 UNION SELECT  'ul8w_ASaz5CQODS6swFhnhhcDCRD_gTmZz6H2wzNa4s' AS templateid,'预约状态提醒' AS templatename,'0' AS status
)
";
        Assert.Equal(expected, result);
    }
}