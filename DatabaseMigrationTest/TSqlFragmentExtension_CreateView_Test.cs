using DatabaseMigration.ScriptGenerator;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigrationTest;

public class TSqlFragmentExtension_CreateView_Test
{
    [Fact]
    public void GetCompleteCreateViewSql_BasicCreateView_ReturnsTokensUpToAs()
    {
        var sql = "CREATE VIEW dbo.TestView AS SELECT 1";
        var parser = new TSql150Parser(true);
        IList<ParseError> errors;
        TSqlFragment fragment;
        using (var rdr = new StringReader(sql))
        {
            fragment = parser.Parse(rdr, out errors);
        }

        Assert.Empty(errors);
        // find index of first Create token
        var tokens = fragment.ScriptTokenStream;
        int startIdx = tokens.ToList().FindIndex(t => t.TokenType == TSqlTokenType.Create);
        Assert.True(startIdx >= 0);

        int idx = startIdx;
        var list = tokens.GetCompleteCreateViewSql(ref idx);

        // should have at least the create/view/name/as tokens
        Assert.True(list.Count >= 4);
        // last returned token should be AS
        Assert.Equal(TSqlTokenType.As, list.Last().TokenType);
        // index should point to token after AS
        Assert.True(idx > startIdx);
        if (idx < tokens.Count)
        {
            Assert.NotEqual(TSqlTokenType.As, tokens[idx].TokenType);
        }
    }

    [Fact]
    public void GetCompleteCreateViewSql_IndexOutOfRange_ReturnsEmpty()
    {
        var sql = "CREATE VIEW dbo.TestView AS SELECT 1";
        var parser = new TSql150Parser(true);
        IList<ParseError> errors;
        TSqlFragment fragment;
        using (var rdr = new StringReader(sql))
        {
            fragment = parser.Parse(rdr, out errors);
        }

        Assert.Empty(errors);
        int idx = fragment.ScriptTokenStream.Count + 5; // out of range
        var list = fragment.ScriptTokenStream.GetCompleteCreateViewSql(ref idx);
        Assert.Empty(list);
    }
    [Fact]
    public void ConvertViewToPostgreSql_ComplexSelectView_ReturnsConvertedSql()
    {
        var sql = @"CREATE   view [dbo].[v_hotel]  as
select a.grpid ,hid ,a.name , servername = b.name  , dbName  = c.name   , db = c.dbName , intIp = c.intIp , dbServer = c.dbServer ,createDate  
from  hotel a left join   
 serverList b on a.serverid = b.id  left join   
 dblist c  on a.dbid = c.id";

        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);

        Assert.Empty(errors);

        var converter = new PostgreSqlViewScriptGenerator();
        var result = converter.GenerateSqlScript(fragment);

        var expected = @"CREATE OR REPLACE    view v_hotel  as

select a.grpid ,hid ,a.name , b.name AS servername  , c.name AS dbname   , c.dbName AS db , c.intIp AS intip , c.dbServer AS dbserver ,createdate  
from  hotel a left join   
 serverlist b on a.serverid = b.id  left join   
 dblist c  on a.dbid = c.id";
        Assert.Equal(expected, result);
    }
    [Fact]
    public void ConvertViewToPostgreSql_ViewWithQuotation_ReturnsConvertedSql()
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