using DatabaseMigration.ScriptGenerator;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigrationTest;

/// <summary>
/// MigrationUtils获取第一个SQL语句单元的测试类，专注于IF块场景。
/// </summary>
public class TSqlFragmentExtension_GetFirstSqlSentence_View_Test
{
    #region 简单select语句和union组成的视图创建语句单元测试
    //简单select语句和union组成的视图创建语句
    private const string _viewSql = @"/*
捷云基础资料集团管控属性
用来定义基础资料集团是否管控，管控力度等
集团管控：这些基础资料只能由集团来统一维护，并且数据只存在一份，标识为集团标识，所有取数据的地方使用集团标识来取数据
集团分发：这些基础资料需要由集团先设置分发和管控相关属性，一般都是由集团来创建，然后分发给分店使用。数据会复制多份，集团和分发的每个分店一份
分店自主：这些基础资料集团不管控，由分店自主设置，各分店查看各自的数据
创建人：陈前良
创建时间：2017-6-16 16:29:26
drop view  m_v_basicDataType 
select * from m_v_basicDataType
select distinct  typecode, typename  from codelist
*/
CREATE  view m_v_basicDataType
as
select 'mbrCardType' as code,'会员卡类型' as name,'集团管控' as dataControl
union all
select 'corpType' , '合约单位类型'  ,'集团管控' 
union
select '20' , '优惠券类别'  ,'集团管控' 
/*union all select '04','市场分类'  ,'集团分发'  */
union
select '05' ,'客人来源'  ,'集团分发'  
";


    /// <summary>
    /// 场景：带注释的完整视图创建语句。
    /// 期望：从0开始获取第一个完整SQL语句，应该返回整个注释块作为第一个完整语句。
    /// </summary>
    [Fact]
    public void GetFirstCompleteSqlSentenceFromCreateViewWithMultipleComments_From0_ReturnsFirstMultipleComments()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_viewSql), out var errors);

        Assert.Empty(errors);

        int startIndex = 0;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"/*
捷云基础资料集团管控属性
用来定义基础资料集团是否管控，管控力度等
集团管控：这些基础资料只能由集团来统一维护，并且数据只存在一份，标识为集团标识，所有取数据的地方使用集团标识来取数据
集团分发：这些基础资料需要由集团先设置分发和管控相关属性，一般都是由集团来创建，然后分发给分店使用。数据会复制多份，集团和分发的每个分店一份
分店自主：这些基础资料集团不管控，由分店自主设置，各分店查看各自的数据
创建人：陈前良
创建时间：2017-6-16 16:29:26
drop view  m_v_basicDataType 
select * from m_v_basicDataType
select distinct  typecode, typename  from codelist
*/";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count, startIndex);
        Assert.Equal(1, startIndex);
    }
    /// <summary>
    /// 场景：带注释的完整视图创建语句。
    /// 期望：从1开始获取第一个完整SQL语句，应该返回create view ... as
    /// </summary>
    [Fact]
    public void GetFirstCompleteSqlSentenceFromCreateViewWithMultipleComments_From1_ReturnsCreateView()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_viewSql), out var errors);

        Assert.Empty(errors);

        int startIndex = 1;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"
CREATE  view m_v_basicDataType
as";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count + 1, startIndex);
        Assert.Equal(9, startIndex);
    }
    /// <summary>
    /// 场景：带注释的完整视图创建语句。
    /// 期望：从9开始获取第一个完整SQL语句，应该返回第一个select 语句
    /// </summary>
    [Fact]
    public void GetFirstCompleteSqlSentenceFromCreateViewWithMultipleComments_From9_ReturnsFirstSelect()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_viewSql), out var errors);

        Assert.Empty(errors);

        int startIndex = 9;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"
select 'mbrCardType' as code,'会员卡类型' as name,'集团管控' as dataControl
";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count + 9, startIndex);
        Assert.Equal(30, startIndex);
    }

    /// <summary>
    /// 场景：带注释的完整视图创建语句。
    /// 期望：从30开始获取第一个完整SQL语句，应该返回union all
    /// </summary>
    [Fact]
    public void GetFirstCompleteSqlSentenceFromCreateViewWithMultipleComments_From30_ReturnsUnionAll()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_viewSql), out var errors);

        Assert.Empty(errors);

        int startIndex = 30;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"union all";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count + 30, startIndex);
        Assert.Equal(33, startIndex);
    }

    /// <summary>
    /// 场景：带注释的完整视图创建语句。
    /// 期望：从33开始获取第一个完整SQL语句，应该返回下一个select语句
    /// </summary>
    [Fact]
    public void GetFirstCompleteSqlSentenceFromCreateViewWithMultipleComments_From33_ReturnsSelect()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_viewSql), out var errors);

        Assert.Empty(errors);

        int startIndex = 33;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"
select 'corpType' , '合约单位类型'  ,'集团管控' 
";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count + 33, startIndex);
        Assert.Equal(46, startIndex);
    }
    /// <summary>
    /// 场景：带注释的完整视图创建语句。
    /// 期望：从46开始获取第一个完整SQL语句，应该返回union
    /// </summary>
    [Fact]
    public void GetFirstCompleteSqlSentenceFromCreateViewWithMultipleComments_From46_ReturnsUnion()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_viewSql), out var errors);

        Assert.Empty(errors);

        int startIndex = 46;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"union";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count + 46, startIndex);
        Assert.Equal(47, startIndex);
    }
    /// <summary>
    /// 场景：带注释的完整视图创建语句。
    /// 期望：从47开始获取第一个完整SQL语句，应该返回下一个select语句
    /// </summary>
    [Fact]
    public void GetFirstCompleteSqlSentenceFromCreateViewWithMultipleComments_From47_ReturnsSelect()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_viewSql), out var errors);

        Assert.Empty(errors);

        int startIndex = 47;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"
select '20' , '优惠券类别'  ,'集团管控' 
";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count + 47, startIndex);
        Assert.Equal(60, startIndex);
    } 
    #endregion
    #region 复杂的单个select语句视图创建语句单元测试
    //复杂的单个select语句视图创建语句
    private const string _viewSql2 = @"CREATE   view [dbo].[v_hotel]  as  
select a.grpid ,hid ,a.name , servername = b.name  , dbName  = c.name   , db = c.dbName , intIp = c.intIp , dbServer = c.dbServer ,createDate  
from  hotel a left join   
 serverList b on a.serverid = b.id  left join   
 dblist c  on a.dbid = c.id";
    /// <summary>
    /// 场景：带注释的完整视图创建语句。
    /// 期望：从0开始获取第一个完整SQL语句，应该返回整个注释块作为第一个完整语句。
    /// </summary>
    [Fact]
    public void GetFirstCompleteSqlSentenceFromCreateView2_From0_ReturnsCreateViewAs()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_viewSql2), out var errors);

        Assert.Empty(errors);

        int startIndex = 0;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"CREATE   view [dbo].[v_hotel]  as";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count, startIndex);
        Assert.Equal(9, startIndex);
    }
    /// <summary>
    /// 场景：带注释的完整视图创建语句。
    /// 期望：从1开始获取第一个完整SQL语句，应该返回create view ... as
    /// </summary>
    [Fact]
    public void GetFirstCompleteSqlSentenceFromCreateView2_From9_ReturnsCompleteSelect()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_viewSql2), out var errors);

        Assert.Empty(errors);

        int startIndex = 9;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"  
select a.grpid ,hid ,a.name , servername = b.name  , dbName  = c.name   , db = c.dbName , intIp = c.intIp , dbServer = c.dbServer ,createDate  
from  hotel a left join   
 serverList b on a.serverid = b.id  left join   
 dblist c  on a.dbid = c.id";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count + 9, startIndex);
        Assert.Equal(129, startIndex);
    }
    #endregion
    #region 使用()括起来的特殊语句单元测试
    //复杂的select语句和union组成的语句，但将所有语句都使用()括起来的特殊视图创建语句
    private const string _viewSql3 = @"  
CREATE view v_templateIdCloseStatus  
as  
(  
 select   
 'xWv-ZHvbvyGOqbKFHyfCCjmmJmDHyS6wm_O81lgImd4' as templateId,--模板ID  
 '维修单通知' as templateName,--模板名称  
 '0' as [status]--是否启用（1：启用此规则，0：禁用此规则）  
  
 UNION SELECT  'wV6PUtNF2D3klC4x-tw-AGmbdbUXkpszVXUTgjpxFHc','审批状态变更通知','0'  
 UNION SELECT  'ul8w_ASaz5CQODS6swFhnhhcDCRD_gTmZz6H2wzNa4s','预约状态提醒','0'  
 UNION SELECT  'hI9x7S8pwkofKnOKbqoQRtiKT8koTTrrmiBg8s_zd6U','报表提醒','0'  
 UNION SELECT  'dwInZkl_zxBsMVvkhD_9bvdHxzk2PZnVEVBmAcHCJcI','查房请求通知','0'  
 UNION SELECT  'd2yfej0ekuSZemEkpLgX-3l6MHEvA7DwTgKwBSV9uwg','新订单提醒','0'  
 UNION SELECT  'b6795-XBp-nRGFMZHV7qUOnPBjBBF7O9bNzVjt5_rxM','商品已发出通知','0'  
 UNION SELECT  '_6iDu0CSCRmFSZKoni_OXiobcV9FnV73wFrmL9rkOMA','受理成功通知','0'  
 UNION SELECT  'Zww8L4pKDkziuxAG3Tt20DEUcOfL9L7aF1AVnGW4rvU','收到投诉建议通知','0'  
 UNION SELECT  'ZRjc5XColreLqhQAtRy-1T3NKAU-s-9e5ml9YX-8tbY','授权提醒','0'  
 UNION SELECT  'YWU2lVIg8nxb2eRe3A6yC6GuuqhwV39NibYB-KoYxEY','宴会厅预定失败通知','0'  
 UNION SELECT  'QQ0NrwGRxwdbrVIo7nIU792Kvsn9rmi2OYIvqYYVv74','预约工单提醒','0'  
 UNION SELECT  'MmZ85uJWoOwDawbNW2zsxkJejyblNORMksB4x91g9-s','会员到店通知','0'  
 UNION SELECT  'LS2240uv4p7RmaOpKDwVO033ra3DLw3CXM_eSiIB1fI','订单取消通知','0'  
 UNION SELECT  'KY2FbBM-OBLXOZ2OSBIwAq5wKaic_l1NeeVMRPfquA0','审批状态更新通知','0'  
 UNION SELECT  'K-swag2XZG3QOYgwt9p8jggl1DKvtXgSCk7nhJt9SB8','入住通知','0'  
 UNION SELECT  'InzsO7HWESGxKyg-lLD1iU557zu_22kvDcawKYycg38','收到保洁单通知','0'  
 UNION SELECT  'BKFZRuty-XnWSzjBoapGj38AJOa2BJL2TF8SvnimA3s','搬家申请成功通知','0'  
 UNION SELECT  '9Q7km_EbBOTbpvErVVoRksBHsewqjI7kXvnJcl_eMi8','商品已发出通知','0'  
 UNION SELECT  '94GqDR1RUHWGFsiAIrMcXARN62nz8f4Jwwtxxg6t8V4','服务评价提醒','0'  
 UNION SELECT  '68d4grtT_dFsmlpmf6v3key9GWq58T1i23xj1uj3doE','上钟通知','0'  
 UNION SELECT  '4q_kChg-yZFa25r0kUXgtDLPQ_4OlB16w9AB4tqgP-8','投诉受理状态通知','0'  
 UNION SELECT  '3el_I5tWfugtoxU7S2Uf9J2RXsuyvD5tXz7YFXei_Kc','会员到期提醒','0'  
 UNION SELECT  '2OeR3MP2WXUeTeybzT-pf1hmkF2WT8r2FY29-BQGDNE','设备维护提醒','0'  
)";
    /// <summary>
    /// 场景：带注释的完整视图创建语句。
    /// 期望：从0开始获取第一个完整SQL语句，应该返回整个注释块作为第一个完整语句。
    /// </summary>
    [Fact]
    public void GetFirstCompleteSqlSentenceFromCreateView3_From0_ReturnsCreateViewAs()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_viewSql3), out var errors);

        Assert.Empty(errors);

        int startIndex = 0;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"  
CREATE view v_templateIdCloseStatus  
as";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count, startIndex);
        Assert.Equal(10, startIndex);
    }
    /// <summary>
    /// 场景：带注释的完整视图创建语句。
    /// 期望：从1开始获取第一个完整SQL语句，应该返回select语句前面的部分
    /// </summary>
    [Fact]
    public void GetFirstCompleteSqlSentenceFromCreateView3_From10_ReturnsBeforeSelect()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_viewSql3), out var errors);

        Assert.Empty(errors);

        int startIndex = 10;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"  
(  
 ";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count + 10, startIndex);
        Assert.Equal(16, startIndex);
    }
    /// <summary>
    /// 场景：带注释的完整视图创建语句。
    /// 期望：从1开始获取第一个完整SQL语句，应该返回select语句
    /// </summary>
    [Fact]
    public void GetFirstCompleteSqlSentenceFromCreateView3_From16_ReturnsSelect()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_viewSql3), out var errors);

        Assert.Empty(errors);

        int startIndex = 16;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"select   
 'xWv-ZHvbvyGOqbKFHyfCCjmmJmDHyS6wm_O81lgImd4' as templateId,--模板ID  
 '维修单通知' as templateName,--模板名称  
 '0' as [status]--是否启用（1：启用此规则，0：禁用此规则）  
";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count + 16, startIndex);
        Assert.Equal(45, startIndex);
    }
    #endregion
    [Fact]
    public void Test()
    {
        var sql = "select distinct 'Engineering' as 'Product',hid,openId as 'openid' from dbo.emWorkerMapping --云工程维修工的微信openid映射表";
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);

        Assert.Empty(errors);
    }
}
