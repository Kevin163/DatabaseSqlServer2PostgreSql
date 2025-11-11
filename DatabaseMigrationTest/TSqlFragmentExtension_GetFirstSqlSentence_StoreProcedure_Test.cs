using DatabaseMigration.ScriptGenerator;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigrationTest;

/// <summary>
/// 存储过程脚本中获取第一个完整SQL语句单元测试
/// </summary>
public class TSqlFragmentExtension_GetFirstSqlSentence_StoreProcedure_Test
{
    #region 更改库结构的存储过程测试
    private const string _update_sys_sql = @"CREATE procedure [dbo].[a_update_Sys]  
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
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','12','运营参数管理',150,'','OperatingParam','Index','1001000000','material-icon')  
        insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','20','用户管理',160,'','UserList','Index','1001000000','user-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','30','角色管理',170,'','RoleList','Index','1001000000','user-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','40','服务器管理',190,'','ServerList','Index','1001000000','server-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','50','数据库管理',200,'','DbList','Index','1001000000','datasorce-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','60','系统日志管理',270,'','SysLog','Index','1001000000','log-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','61','操作日志管理',280,'','SysOpLog','Index','1001000000','log-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','70','广告设置',60,'','AdSet','Index','1001000000','ad-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','80','新店及酒店维护',10,'','Hotel','Index','1001000000','hotel-m-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','90','客户到期预报',90,'','HotelExpire','Index','1001000000','hotel-m-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','91','客户延期日志',100,'','HotelDelayLog','Index','1001000000','hotel-m-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','92','客户授权管理',110,'','AuthorizeList','Index','1001000000','hotel-m-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','100','试用体验管理',130,'','TryInfo','Index','1001000000','tiyan-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','110','平台系统参数',210,'','SysPara','Index','1001000000','platform-system-icon')   
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','120','硬件接口版本管理',220,'','HardwareInterface','Index','1001000000','hardware-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','130','智能POS设备管理',30,'','HotelPos','Index','1001000000','hardware-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','135','人脸识别设备管理',40,'','FaceDevices','Index','1001000000','hardware-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','140','售后服务工程师',120,'','ServiceOperator','Index','1001000000','user-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','150','帮助文档管理',80,'','HelpFiles','Index','1001000000','help-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','160','系统公告',70,'','Notice','Index','1001000000','help-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','170','美团接口管理',230,'','PoleStar','Index','1001000000','hardware-icon')    
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','171','美团接口管理新',230,'','MeiTuanShop','Index','1001000000','hardware-icon')    
  --insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','210','口碑接口日志',260,'','KBOpenApiLog','Index','1001000000','log-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','211','抖音接口管理',240,'','TikTok','Index','1001000000','hardware-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','212','新口碑接口管理',242,'','NewKouBei','Index','1001000000','hardware-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','213','快手接口管理',245,'','KuaiShou','Index','1001000000','hardware-icon')    
  --insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','180','北极星接口日志',240,'','OpenApiLog','Index','1001000000','log-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','190','服务器性能查看',290,'','ServerListPerformance','Index','1001000000','datasorce-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','200','口碑接口管理',250,'','KouBei','Index','1001000000','hardware-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','220','程序更新管理',300,'','AppUpdate','Index','1001000000','public-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','230','扫码点餐酒店管理',20,'','HotelSM','Index','1001000000','public-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','240','捷音数量控制',50,'','HotelVoiceQty','Index','1001000000','hotel-m-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','250','合作伙伴设置',85,'','PartnerSet','Index','1001000000','user-icon')  
  INSERT INTO dbo.authlist(AuthCode ,ParentCode ,AuthName ,Area ,Controller ,Action ,mask ,class ,Seqid)VALUES  ( '400' , '1' , 'SCM客户等级管理' , '' , 'HotelLevel' , 'Index' , '1001000000' , 'hotel-m-icon' , 400  )      
  INSERT INTO dbo.authlist(AuthCode ,ParentCode ,AuthName ,Area ,Controller ,Action ,mask ,class ,Seqid)VALUES  ( '401' , '1' , 'SCM酒店物品类别关联' , '' ,'Hotel' , 'HotelItemcategoryRelation' , '1001000000' , 'hotel-m-icon' , 401  )  
  INSERT INTO dbo.authlist(AuthCode ,ParentCode ,AuthName ,Area ,Controller ,Action ,mask ,class ,Seqid)VALUES  ( '402' , '1' , 'SCM物品类别管理' , '' , 'ItemCategory' , 'Index' , '1001000000' , 'hotel-m-icon' , 402  )  
  INSERT INTO dbo.authlist(AuthCode ,ParentCode ,AuthName ,Area ,Controller ,Action ,mask ,class ,Seqid)VALUES  ( '403' , '1' , 'SCM供应商待确认' , '' , 'Supplier' , 'Confirmed' , '1001000000' , 'hotel-m-icon' , 403  )  
  INSERT INTO dbo.authlist(AuthCode ,ParentCode ,AuthName ,Area ,Controller ,Action ,mask ,class ,Seqid)VALUES  ( '404' , '1' , 'SCM供应商管理' , '' , 'Supplier' ,'Index' , '1001000000' , 'hotel-m-icon' ,404  )  
  INSERT INTO dbo.authlist(AuthCode ,ParentCode ,AuthName ,Area ,Controller ,Action ,mask ,class ,Seqid)VALUES  ( '405' , '1' , 'SCM供应商意见反馈列表' , '' , 'FeedBack' , 'Index' , '1001000000' , 'hotel-m-icon' , 405  )    
        insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','260','营业点扫呗支付管理',310,'','RefeSbPayConfigure','Index','1001000000','hardware-icon')         
    
  --新店及酒店维护  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',1,1,'Query','查询')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',2,2,'Add','增加')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',131072,3,'Update','修改')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',4,3,'Save','保存')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',64,4,'Enable','酒店管理-启用')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',128,5,'Disable','酒店管理-禁用')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',16,6,'ChannelReSetKey','渠道设置-重新生成密钥')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',32,7,'FuncReSetKey','通用功能设置-重新生成密钥')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',256,8,'OtherIsable','其他设置-启用禁用')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',2048,9,'ItemSet','项目设置')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',4096,10,'ChannnelSet','渠道设置')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',8192,11,'FunctionSet','功能设置')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',16384,12,'InterfaceSet','接口设置')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',32768,13,'SystemParaSet','系统参数设置')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',65536,14,'OperatingParaSet','运营参数设置')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',262144,15,'SyncMaster','同步Master')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',8,16,'Delete','集团分店与单店互转')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',8388608,17,'Excel','导出Excel')  
    
  --客户授权管理  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('92',1,1,'Query','查询')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('92',2,2,'Add','生成')  
    
  --合作伙伴设置  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('250',1,1,'Query','查询')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('250',2,2,'Add','增加')   
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('250',131072,3,'Update','修改')  
    
  --平台系统参数  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('110',1,1,'Query','查询')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('110',131072,3,'Update','修改')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('110',4,3,'Save','保存')  
     
  --智能POS设备管理  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('130',1,1,'Query','查询')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('130',2,2,'Add','增加')   
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('130',131072,3,'Update','修改')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('130',4,3,'Save','保存')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('130',8,4,'Delete','删除')  
    
  --售后服务工程师  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('140',1,1,'Query','查询')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('140',2,2,'Add','增加')   
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('140',131072,3,'Update','修改')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('140',4,3,'Save','保存')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('140',8,4,'Delete','删除')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('140',64,5,'Enable','启用')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('140',128,6,'Disable','禁用')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('140',1024,7,'ResetPwd','重置密码')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('140',512,8,'UnbindWeChat','解除绑定微信')  
     
  --帮助文档管理  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('150',1,1,'Query','查询')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('150',131072,3,'Update','修改')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('150',4,3,'Save','保存')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('150',8,4,'Delete','删除')  
     
  --系统公告  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('160',1,1,'Query','查询')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('160',2,2,'Add','增加')   
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('160',131072,3,'Update','修改')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('160',4,3,'Save','保存')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('160',8,4,'Delete','删除')  
    
  --捷音数量控制  
  insert into AuthButtons(AuthButtonId,AuthId,AuthButtonName,AuthButtonValue,seqid) values('Query',240,'查询',1,1)   
  insert into AuthButtons(AuthButtonId,AuthId,AuthButtonName,AuthButtonValue,seqid) values('Add',240,'增加',2,2)   
  insert into AuthButtons(AuthButtonId,AuthId,AuthButtonName,AuthButtonValue,seqid) values('Update',240,'修改',131072,3)  
  insert into AuthButtons(AuthButtonId,AuthId,AuthButtonName,AuthButtonValue,seqid) values('Delete',240,'删除',8,4)  
    
  --扫码点餐酒店管理  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('230',1,1,'Query','查询')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('230',2,2,'Add','增加')   
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('230',131072,3,'Update','修改')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('230',4,3,'Save','保存')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('230',8,4,'Delete','删除')  
  
  --营业点扫呗支付管理  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('260',1,1,'Query','查询')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('260',2,2,'Add','增加')   
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('260',131072,3,'Update','修改')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('260',4,3,'Save','保存')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('260',8,4,'Delete','删除')  
          
        --客户到期预报表  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('90',1,1,'Query','查询')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('90',4,3,'Save','保存')  
  
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
END  
--修改pos设备表，另外增加一个id来做为主键，原来的设备编号也允许进行修改  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE ID = OBJECT_ID('HotelPos') AND name = 'Id')  
BEGIN  
    ALTER TABLE HotelPos ADD ID UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()  
    ALTER TABLE HotelPos DROP CONSTRAINT pk_hotelPos  
    ALTER TABLE hotelPos ADD CONSTRAINT pk_hotelPos PRIMARY KEY(ID)  
END  
  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('hotel') AND name = 'customerStatus')  
BEGIN  
 ALTER TABLE hotel ADD customerStatus VARCHAR(2) NOT NULL DEFAULT '0'  
END  
  
--短信发送日志增加字段[account]账号 2018-11-29 10:56:26 李泽锐 134350  
if not exists(select * from syscolumns where id = OBJECT_ID('smsLog') and name = 'account')  
begin  
 alter table smslog add account VARCHAR(100) null     
END  
--汇颐微信绑定会员openid与酒店映射 肖念 2018-11-29 16:16:13  
IF OBJECT_ID('HuiYiMapping') IS NULL  
BEGIN  
 CREATE TABLE HuiYiMapping(  
  [id] [uniqueidentifier] NOT NULL primary key,  
  [hid] [char](6),    
  [openId] [varchar](60)  
 )  
END  
--酒店表增加产品类型 2018-12-04 11:01 job：134658  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('hotel') AND name = 'productType')  
BEGIN  
 ALTER TABLE hotel ADD productType VARCHAR(10) NULL  
END  
  
--增加一个集团长租客人微信openid与酒店对应关系记录表，李志伟，2018-12-04 09:56:03  
if OBJECT_ID('weixinLongCustomerMapping') is null  
begin  
     create table weixinLongCustomerMapping(  
   [mappingId] [int] IDENTITY(1,1) NOT NULL primary key,  
   [hid] [varchar](6) NOT NULL,  
   [customerWxOpenId] [varchar](60) NOT NULL,  
   [cdate] [datetime] NOT NULL,  
     )  
end  
  
--待付款列表增加支付账务IDPayTransIds 向以胜 2018-12-10  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('WaitPayList') AND name = 'PayTransIds')  
BEGIN  
 ALTER TABLE dbo.WaitPayList ADD PayTransIds VARCHAR(8000) NULL   
END  
  
--增加集团试用酒店id参数 xjy 2018-12-24 17:23  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'TryHotelIdForGroup')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'TryHotelIdForGroup' ,'集团试用酒店id' ,'000060' ,'' ,'集团试用酒店id，用于登录时我要试用的功能' ,50)  
END  
  
--增加集团试用用户名参数 xjy 2018-12-24 17:23  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'TryUsernameForGroup')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'TryUsernameForGroup' ,'集团试用登录名' ,'tryUser' ,'' ,'集团试用登录名，用于登录时我要试用的功能' ,50)  
END  
--go  
--增加集团试用密码 xjy 2018-12-24 17:23  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'TryUserPassForGroup')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'TryUserPassForGroup' ,'集团试用密码' ,'' ,'' ,'集团试用登录名对应的密码，用于登录时我要试用的功能' ,50)  
END  
  
--增加捷信达支付中间程序接口 向以胜 2018-12-29  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'ClientPayUrl')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'ClientPayUrl' ,'捷信达支付中间程序接口' ,'' ,'' ,'捷信达支付中间程序接口' ,17)  
END  
  
  
--增加帮助文档code代码 何磊 2019-01-09  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('helpFiles') AND name = 'code')  
BEGIN  
 ALTER TABLE dbo.helpFiles ADD code VARCHAR(100) NULL   
END  
  
-- 给酒店表增加总裁驾驶舱功能(实现各个模块单独控制) 谭健 2019-1-11 jobId:136626  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id =OBJECT_ID('hotel') AND name='AnalysisModule')  
BEGIN  
   ALTER TABLE hotel ADD AnalysisModule varchar(300)  
END  
  
--增加一个酒店使用的硬件接口表，李志伟，2019-01-11 19:33:03  
if OBJECT_ID('HotelInterface') is null  
begin  
  create table HotelInterface  
  (  
   id uniqueidentifier not null primary key--主键ID  
  ,hid char(6) not null--酒店ID  
  ,typeCode varchar(100) not null--接口类型  
  ,code varchar(100) not null--接口值  
  );  
end  
  
-- 给酒店表增加主题功能(实现不同酒店不同样式和UI) lizw 2019-1-18  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id =OBJECT_ID('hotel') AND name='Theme')  
BEGIN  
   ALTER TABLE hotel ADD Theme varchar(300)  
END  
  
--jobid：134998  
--增加授权码生成人（最后生成的操作员）。李泽锐_Jerry ，2019-1-23 16:58:22  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('ServicesAuthorize') AND name = 'adminUser')  
BEGIN  
 ALTER TABLE ServicesAuthorize ADD adminUser VARCHAR(100) NULL  
END  
  
--增加一个运营后台日志表  
if OBJECT_ID('sysOpLog') is null  
begin  
     create table sysOpLog(  
  [cDate] [DATETIME] NOT NULL,  --操作时间  
  [cUser] [VARCHAR](30) NOT NULL,  --操作员  
  [ip] [VARCHAR](30) NULL,   --操作Ip  
  [xType] [VARCHAR](30) NULL,   --操作类型  
  [cText] [VARCHAR](8000) NULL,  --操作内容  
  [keys] [VARCHAR](60) NULL,   --操作数据关键字  
  [LogId] [BIGINT] IDENTITY(1,1) NOT NULL primary key  
     )  
end  
  
--增加智慧平台体验试用酒店id参数 xn 2019-02-24 17:23  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'TryHotelIdForwisdom')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'TryHotelIdForwisdom' ,'智慧平台试用酒店id' ,'000831' ,'' ,'智慧平台试用酒店id，用于登录时我要试用的功能' ,301)  
END  
  
--增加智慧平台试用用户名参数 xn 2019-02-24 17:23  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'TryUsernameForwisdom')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'TryUsernameForwisdom' ,'智慧平台试用登录名' ,'tryUser' ,'' ,'智慧平台试用登录名，用于登录时我要试用的功能' ,302)  
END  
--增加智慧平台体验账号 xn 2019-12-24 17:23  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'TryUserPassForwisdom')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'TryUserPassForwisdom' ,'智慧平台试用密码' ,'' ,'' ,'智慧平台试用登录名对应的密码，用于登录时我要试用的功能' ,303)  
END  
  
---程序版本发布相关表  
if OBJECT_ID('AppUpdateHistory') is null  
CREATE TABLE [dbo].[AppUpdateHistory](  
 [ID] [uniqueidentifier] NOT NULL,  
 [AppUpdateID] [uniqueidentifier] NOT NULL,  
 [VersionDesc] [varchar](1000) NOT NULL,  
 [ApplicationStartName] [varchar](1000) NOT NULL,  
 [VersionSort] [bigint] NOT NULL,  
 [ReleaseVersion] [varchar](100) NOT NULL,  
 [ReleaseDate] [datetime] NOT NULL,  
 [ReleaseUrl] [varchar](1000) NOT NULL,  
 [UpdateMode] [varchar](100) NOT NULL,  
 [ServerlistID] [varchar](100) NOT NULL,  
 [Status] [int] NOT NULL,  
 [PublishDate] [datetime] NOT NULL,  
 [ModifiedDate] [datetime] NOT NULL,  
 [ModifiedUser] [varchar](100) NOT NULL,  
 [IsAll] [bit] NOT NULL,  
 [IsMust] [bit] NOT NULL,  
 CONSTRAINT [PK__AppUpdat__3214EC271EE485AA] PRIMARY KEY CLUSTERED   
(  
 [ID] ASC  
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]  
) ON [PRIMARY]  
  
if OBJECT_ID('AppUpdateHistoryHotel') is null  
CREATE TABLE [dbo].[AppUpdateHistoryHotel](  
 [ID] [uniqueidentifier] NOT NULL,  
 [UpdateHistoryID] [uniqueidentifier] NOT NULL,  
 [Hid] [varchar](6) NOT NULL,  
 [HotelName] [varchar](100) NOT NULL,  
 [Status] [int] NOT NULL,  
 [ModifiedDate] [datetime] NOT NULL,  
 [ModifiedUser] [varchar](100) NOT NULL,  
 CONSTRAINT [PK__AppUpdat__3214EC2722B5168E] PRIMARY KEY CLUSTERED   
(  
 [ID] ASC  
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]  
) ON [PRIMARY]  
  
if OBJECT_ID('AppUpdateInfo') is null  
CREATE TABLE [dbo].[AppUpdateInfo](  
 [ID] [uniqueidentifier] NOT NULL,  
 [AppName] [varchar](100) NOT NULL,  
 [Desc] [varchar](1000) NULL,  
 [Status] [int] NOT NULL,  
 [ModifiedDate] [datetime] NOT NULL,  
 [ModifiedUser] [varchar](100) NOT NULL,  
 [AppCode] [varchar](100) NOT NULL,  
 [Products] [varchar](500) NULL,  
 CONSTRAINT [PK__AppUpdat__3214EC271B13F4C6] PRIMARY KEY CLUSTERED   
(  
 [ID] ASC  
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]  
) ON [PRIMARY]  
  
-- 汇颐 新增智能手表类型 下拉选择 肖念 2019-03-13  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id =OBJECT_ID('hotel') AND name='SmartWatchType')  
BEGIN  
   ALTER TABLE hotel ADD SmartWatchType varchar(40)  
END  
-- 汇颐 新增一体机类型 下拉选择 肖念 2019-03-13  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id =OBJECT_ID('hotel') AND name='OneMachineType')  
BEGIN  
   ALTER TABLE hotel ADD OneMachineType varchar(40)  
END  
  
--酒店表增加捷云会员版本 2019-3-28 10:44:32 job：138953  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('hotel') AND name = 'prodMbrType')  
BEGIN  
 ALTER TABLE hotel ADD prodMbrType VARCHAR(30) NULL  
END  
  
--设备id与酒店id对应表   创建人：何磊  时间：2019-04-10  
if not exists(select id from sysobjects where name = 'ImeiMappingHid')  
BEGIN  
 create table ImeiMappingHid (  
   hid                  char(6)              not null,  
   imei                 varchar(20)        PRIMARY KEY  not null,  
   status               tinyint              null  
)  
END   
  
--扫码点餐对应酒店 snow 2019年5月8日  
if not exists(select id from sysobjects where name = 'posSmMappingHid')  
begin  
create table posSmMappingHid (  
  hotelCode char(6) not null PRIMARY KEY,  --酒店代码 主键  
  hotelName varchar(300),      --酒店名称  
  hid char(6)  ,       --酒店Id  
  isCs bit ,         --是否线下餐饮  
  notifyURL varchar(500),      --接口地址（线下程序必须要填）  
  status  tinyint ,       --状态（0：启用，51：禁用）  
  modifyDate datetime ,  
  modifyUser varchar(300),  
  GsWxComid varchar(300),  
  GsWxOpenidUrl varchar(4000),  
  GsWxTemplateMessageUrl varchar(4000),  
  GsWxCreatePayOrderUrl varchar(4000),  
  GsWxPayOrderUrl varchar(4000),  
)  
END  
  
--数据库管理增加排序 2019-5-23 19:47:12 job：139619  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('dblist') AND name = 'seqid')  
BEGIN  
 ALTER TABLE dblist ADD seqid int NULL  
END  
  
--POS扫码点餐表增加集团ID 2019-6-24 12:00:00   
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('posSmMappingHid') AND name = 'groupid')  
BEGIN  
 ALTER TABLE posSmMappingHid ADD groupid CHAR(6) NULL  
END  
  
  
-- 增加扫码点餐表字段 Id,ProductCode,ProductName ，修改扫码点餐表的主键为Id  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('posSmMappingHid') AND name = 'Id')  
BEGIN  
 ALTER TABLE posSmMappingHid ADD Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()  
END  
  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('posSmMappingHid') AND name = 'ProductCode')  
BEGIN  
 ALTER TABLE posSmMappingHid ADD ProductCode VARCHAR(100) NULL   
END  
  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('posSmMappingHid') AND name = 'ProductName')  
BEGIN  
 ALTER TABLE posSmMappingHid ADD ProductName VARCHAR(100) NULL   
END  
  
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
  
--增加rabbitmq连接地址参数 2019-09-20 蒋创世  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'rabbitMqHost')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'rabbitMqHost' ,'rabbitmq连接地址' ,'120.24.167.205' ,'120.24.167.205' ,'rabbitMq消息队列连接地址' ,400)  
END  
--增加rabbitmq连接端口参数  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'rabbitMqPort')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'rabbitMqPort' ,'rabbitmq连接端口' ,'5672' ,'5672' ,'rabbitMq消息队列连接端口' ,401)  
END  
--增加rabbitmq连接用户名参数  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'rabbitMqUser')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'rabbitMqUser' ,'rabbitmq连接用户名' ,'jxdpms' ,'jxdpms' ,'rabbitMq消息队列连接用户名' ,402)  
END  
--增加rabbitmq连接密码参数  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'rabbitMqPassword')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'rabbitMqPassword' ,'rabbitmq连接密码' ,'jxd@598.pms' ,'jxd@598.pms' ,'rabbitMq消息队列连接密码' ,403)  
END  
--增加rabbitmq捷云内部信息交互的vhost  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'rabbitMqVHostPms')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'rabbitMqVHostPms' ,'rabbitmq捷云内部vhost' ,'jxdpms' ,'jxdpms' ,'rabbitmq捷云内部信息交互的vhost' ,405)  
END  
--增加rabbitmq捷云人脸识别信息交互vhost  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'rabbitMqVHostFace')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'rabbitMqVHostFace' ,'rabbitmq人脸识别vhost' ,'jxdface' ,'jxdface' ,'rabbitmq捷云人脸识别信息交互vhost' ,406)  
END  
  
--2019-9-24 lish 添加帮助文档语言版本字段  
if(not exists(select 1 from syscolumns where id=object_id('helpFiles') and [name]='language'))  
begin  
 alter table helpFiles add [language] varchar(10) null  
end  
  
--2019-10-16 zk 存ipos、iyacht在达人汇公众号登陆的账号信息  
if OBJECT_ID('DRHPosLoginInfo') is null  
begin  
 create table DRHPosLoginInfo  
 (  
  Id uniqueidentifier not null primary key--主键ID  
  ,HotelCode char(6) not null --酒店code,对应posSmMappingHid的HotelCode  
  ,UserCode varchar(100) not null --登陆账号  
  ,GsWxOpenId varchar(600) not null --openid  
  ,IType varchar(20) not null --登陆系统类别  
  ,CreateDate Datetime null  
  ,ModifyDate Datetime null  
 );  
end  
--增加人脸识别设备信息表，用于在运营后台控制设备和酒店的对应关系，陈前良，2019-8-5 9:59:49  
if OBJECT_ID('FaceDevices') is null  
begin  
 create table FaceDevices(  
  DeviceSN varchar(60) not null,--设备序列号，主键值,可以由人工编码，比如hid_sn,000001_001,也可以由系统自动生成guid的编码  
  Hid varchar(6) not null,--设备所属酒店  
  FaceDataType int not null,--人脸识别数据类型，1：在住客，2：会员，必须以2的次方来赋值  
  FaceMethodType int not null,--人脸识别业务场景，1：早餐（以后扩展使用），用于系统接收到人脸出现通知后，知道要做哪方面的业务处理  
  DeviceStatus tinyint not null,--设备状态,1:正常,51:禁用  
  DeviceName varchar(60) not null,--设备名称  
  DeviceAddress varchar(200) not null,--设备安装地址  
  Score decimal(18,2) not null,--识别推送阀值  
  Alive bit not null,--是否活体检测  
  Token varchar(60) null,--会话令牌  
  LoginDate datetime null,--登录时间  
  LastAccessDate datetime null,--最近一次访问时间  
  DeviceIp varchar(30) null,--设备上传ip,  
  RunningStatus int null,--设备运行状态，1：正常，2：停止、待机  
  SdkVersion varchar(30) null,--算法版本  
  CpuUsage varchar(30) null,--cpu使用率  
  MemorySize varchar(30) null,--内存大小  
  MemoryUsage varchar(30) null,--内存使用率  
  DiskSize varchar(30) null,--磁盘大小  
  DiskUsage varchar(30) null,--磁盘使用率  
  DiskFreeSize varchar(30) null,--磁盘可用空间  
  CreateDate datetime not null,--创建时间  
  Creator varchar(30) null,--创建人  
  ModifyDate datetime null,--修改时间  
  Modifiedor varchar(30) null,--修改人  
  constraint pk_faceDevices primary key(DeviceSN)  
 )  
end  
  
--酒店表新增国籍列 熊壮 2019-10-24 jobid:146491  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('hotel') AND name='nation')  
BEGIN  
 ALTER TABLE dbo.hotel ADD  nation varchar(60) NULL  
end  
  
--帮助文档新增是否显示列 熊壮 2019-10-31  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('helpFiles') AND name='showStatus')  
BEGIN  
 alter table dbo.helpFiles add showStatus bit null  
  exec('update helpFiles set showStatus=checkStatus where 1=1');  
  alter table dbo.helpFiles alter column showStatus bit not null;  
end  
  
--新增帮助文档待审核表 熊壮 2019-10-31  
if object_id('helpFilesNotPass') is null  
begin  
 create table [dbo].[helpFilesNotPass](  
  [id] [int] primary key identity(1,1) not null,  
    [rId] [int] null,  
  [title] [varchar](500) not null,  
  [addUser] [varchar](50) not null,  
  [addDate] [datetime] not null,  
  [updateUser] [varchar](50) null,  
  [updateDate] [datetime] not null,  
  [checkStatus] [bit] not null,  
  [checkUser] [varchar](50) null,  
  [checkDate] [datetime] null,  
  [readNumber] [int] not null,  
  [menuId] [varchar](1000) not null,  
  [menuName] [varchar](1000) not null,  
  [content] [text] null,  
  [code] [varchar](100) null,  
  [language] [varchar](10) null,  
    [showStatus] [bit] not null,  
 );  
end  
--酒店表新增VOD接口类型 付龙 2019-11-25  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('hotel') AND name='vodtype')  
BEGIN  
 ALTER TABLE dbo.hotel ADD  vodtype varchar(60) NULL  
end  
  
--新增捷云版本与系统参数关联表  
if(object_id('versionParas') is null)  
begin  
  create table versionParas  
  (  
    id uniqueidentifier primary key,  
    vCode varchar(100) not null,  
    paraCode varchar(100) not null,  
  )  
end  
  
--增加ispa微信第三方平台 appid 2019-12-12 19:30:30  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'ISPAOpenAppId')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'ISPAOpenAppId' ,'ispa微信第三方平台AppId' ,'wx3d11cc03c09acdb0' ,'' ,'ispa微信第三方平台AppId' ,500)  
END  
--增加ispa微信第三方平台Secret  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'ISPAOpenSecret')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'ISPAOpenSecret' ,'ispa微信第三方平台Secret' ,'3913d0365a27af1ff348594b4f6dcc8a' ,'' ,'ispa微信第三方平台Secret' ,501)  
END  
  
--ispa微信第三方平台Token  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'ISPAOpenToken')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'ISPAOpenToken' ,'ispa微信第三方平台Token' ,'weixin' ,'' ,'ispa微信第三方平台Token' ,502)  
END  
  
--ispa微信第三方平台EncodingAESKey  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'ISPAOpenEncodingAESKey')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'ISPAOpenEncodingAESKey' ,'ispa微信第三方平台EncodingAESKey' ,'xjfmdkc5qkt5vzvi5v5p7pb5ahq7rc8zq8vmakfw6gb' ,'' ,'ispa微信第三方平台EncodingAESKey' ,503)  
END  
  
  
-- 增加微信第三方平台授权关系表 zhangb 2019-12-13 10:51:52  
if object_id('WxOpenAuthorization') is null  
begin  
 CREATE TABLE [dbo].[WxOpenAuthorization] (  
  [Id] int IDENTITY(1,1) NOT NULL ,  
  [ComponentAppid] varchar(256) NULL,  
  [AuthorizerAppId] varchar(256) NULL,  
  [AuthorizerRefreshToken]  varchar(256) NULL,    
  [ModifiedDate] datetime  NULL,  
  [ModifiedUser] varchar(56) NULL    
 PRIMARY KEY CLUSTERED   
 (  
  [Id] ASC  
 )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]  
 ) ON [PRIMARY]  
end  
  
--版本系统参数关联表新增产品类型字段 熊壮 2019-12-17  
if not exists(select * from syscolumns where id=object_id('versionParas') and name='vProduct')  
begin  
 alter table dbo.versionParas add vProduct varchar(60) null  
  exec('update versionParas set vProduct=''pms'' where vProduct is null')  
end  
  
  
  
--增加扫码链接记录表 马嘉禧 2019-12-11   
if not exists (select * from sysobjects where name = 'scanCodeLink' )    
begin  
 create table scanCodeLink  
 (  
  id varchar(30) not null primary key--主键ID  
 ,hid char(6)  null --酒店ID  
 ,regid varchar(30)  null --账号ID  
 ,roomNo varchar(30)  null --房号  
 ,[type] varchar(30)  null --代码类型  
 ,CreateDate datetime  null --链接创建时间  
 ,EndDate datetime not null  --链接失效时间  
 );  
end  
  
--增加rabbitmq ispa vhost  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'rabbitMqVHostIspa')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'rabbitMqVHostIspa' ,'rabbitmq用于ispa的vhost' ,'jxdispa' ,'jxdispa' ,'rabbitmq用于ispa的vhost' ,406)  
END  
  
-- 增加极光推送推送地址  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'JiGuangPushUrl')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'JiGuangPushUrl' ,'极光推送地址' ,'http://127.0.0.1:8314/api/sendPush' ,'http://127.0.0.1:8314/api/sendPush' ,'极光推送地址' ,505)  
end  
  
--新增操作员移动端默认入口设置表 熊壮 2019-12-25  
if(object_id('operatorAppEnter') is null)  
begin  
  create table operatorAppEnter  
  (  
    id uniqueidentifier primary key,  
    openid varchar(60) not null,  
    product varchar(60) not null,  
    isRedirect bit  
  )  
end  
  
--新增运营小组参数设置表 熊壮 2019-12-25  
if(object_id('operatingParam') is null)  
begin  
  create table operatingParam  
  (  
    id  int primary key identity(1,1),  
    code varchar(60) not null,  
    [cDate] datetime,  
  )  
end  
  
  
-- 增加ispa Quartz.Net 数据库ip  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'QuartzNetIp')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'QuartzNetIp' ,'ispa Quartz.Net 数据库ip' ,'' ,'' ,'ispa Quartz.Net 数据库ip' ,506)  
end  
  
-- 增加ispa Quartz.Net 数据库  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'QuartzNetDataBase')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'QuartzNetDataBase' ,'ispa Quartz.Net 数据库名' ,'' ,'' ,'ispa Quartz.Net 数据库名' ,507)  
end  
  
-- 增加ispa Quartz.Net 数据库用户名  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'QuartzNetUser')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'QuartzNetUser' ,'ispa Quartz.Net 数据库用户名' ,'' ,'' ,'ispa Quartz.Net 数据库用户名' ,508)  
end  
  
-- 增加ispa Quartz.Net 数据库密码  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'QuartzNetPwd')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'QuartzNetPwd' ,'ispa Quartz.Net 数据库密码' ,'' ,'' ,'ispa Quartz.Net数据库密码' ,509)  
end  
  
-- 增加捷云服务热线平台参数 xuhj  2020-1-14  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'ServicePhone')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'ServicePhone' ,'服务热线' ,'400-9922-511' ,'400-9922-511' ,'服务热线' ,510)  
end  
  
-- 增加捷云公司名称平台参数 xuhj  2020-1-14  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'CompanyName')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'CompanyName' ,'公司名称' ,'深圳市捷信达电子有限公司' ,'深圳市捷信达电子有限公司' ,'公司名称' ,511)  
end  
  
-- 增加捷云公司地址平台参数 xuhj  2020-1-14  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'CompanyAddress')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'CompanyAddress' ,'公司地址' ,'深圳市福田区深南大道6025号英龙大厦25楼' ,'深圳市福田区深南大道6025号英龙大厦25楼' ,'公司地址' ,512)  
end  
  
-- 增加捷云公司总机平台参数 xuhj  2020-1-14  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'CompanyTelephone')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'CompanyTelephone' ,'公司总机' ,'0755-83664567' ,'0755-83664567' ,'公司总机' ,513)  
end  
  
-- 增加捷云公司传真平台参数 xuhj  2020-1-14  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'CompanyFax')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'CompanyFax' ,'公司传真' ,'0755-83663702' ,'0755-83663702' ,'公司传真' ,514)  
end  
  
-- 增加捷云公司版权号平台参数 xuhj  2020-1-14  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'CompanyVersionNumber')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'CompanyVersionNumber' ,'公司版权号' ,'粤ICP备09046004号' ,'粤ICP备09046004号' ,'公司版权号' ,515)  
end  
if not exists(select * from sys.tables where name = 'HotelVoiceQtys')  
begin  
 create table HotelVoiceQtys(  
  Id uniqueidentifier not null,--主键值  
  Hid varchar(6) not null,--酒店id  
  QtyType varchar(20) not null,--数量类型，目前支持Install,MessagePush，其中Install控制安装数量，MessagePush控制推送数量  
  Qty int not null,--数量值  
  QtyExpired datetime not null,--数量失效时间  
  CheckCode varchar(6) not null,--验证码，由程序随机生成，只有在输入正确的hid+CheckCode的情况下，才允许扣减数量  
  UsedQty int null,--已使用数量，冗余字段，避免每次计算使用数量都要从明细表中进行统计  
  Remark varchar(200) null,--备注  
  Creator varchar(30) null,--创建者  
  CDate datetime null,--创建时间  
  constraint pk_HotelVoiceQtys primary key(Id)  
 )  
end  
  
if not exists(select * from sys.indexes where name = 'ix_HotelVoiceQtys')  
begin  
 create index ix_HotelVoiceQtys on HotelVoiceQtys(Hid,QtyType)  
end  
--增加捷音安装数量使用明细表，陈前良，2020-3-30 9:56:46  
if not exists(select * from sys.tables where name = 'HotelVoiceQtyInstallDetail')  
begin  
 create table HotelVoiceQtyInstallDetail(  
  Id uniqueidentifier not null,--主键值  
  QtyId uniqueidentifier not null,--数量控制id  
  Hid varchar(6) not null,--酒店id，  
  DeviceNo varchar(100) not null,--安装设备编号  
  WxUnionId varchar(64) not null,--安装人微信unionid  
  WxNickname varchar(100) null,--安装人微信昵称  
  Station varchar(100) null,--安装人岗位  
  CDate datetime null,--安装时间  
  constraint pk_HotelVoiceQtyInstallDetail primary key(Id)  
 )  
end  
if not exists(select * from sys.indexes where name = 'ix_HotelVoiceQtyInstallDetail')  
begin  
 create index ix_HotelVoiceQtyInstallDetail on HotelVoiceQtyInstallDetail(QtyId)  
end  
  
--增加整体网站的负载均衡访问端口参数，默认为80，陈前良 ，2020-4-5 17:14:56  
if not EXISTS(select * from sysPara where code = 'SLBPort')  
begin  
 insert into sysPara(code,name,refValue,VALUE,remark,seqid)values('SLBPort','负载均衡监听端口','80','80','用于当不是80时，登录后跳转和报表需要带上端口号进行访问',9999)  
end  
  
--增加第三方URL对应的Token 向以胜 2020-04-23  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'WeixinToken')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES ('WeixinToken' ,'第三方URL对应的Token' ,'szsjxd' ,'' ,'第三方URL对应的Token，注意：修改后通知开发回收应用程序' ,18)  
END  
--增加第三方URL对应的消息加解密密钥 向以胜 2020-04-23  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'WeixinEncodingAESKey')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES ('WeixinEncodingAESKey' ,'第三方URL对应的消息加解密密钥' ,'9vQB3ADB0taWjvykRZKglF6DDMNjZPmGc1kvPNHRy1C' ,'' ,'第三方URL对应的消息加解密密钥，注意：修改后通知开发回收应用程序' ,18)  
END  
--增加微信AppId 向以胜 2020-04-23  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'WeixinAppId')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES ('WeixinAppId' ,'微信AppId' ,'wx5a9d341a4bc0ff50' ,'' ,'微信AppId，注意：修改后通知开发回收应用程序' ,18)  
END  
--增加微信AppSecret 向以胜 2020-04-23  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'WeixinAppSecret')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES ('WeixinAppSecret' ,'微信AppSecret' ,'741388c109e262bfe127f8c9f5ff203b' ,'' ,'微信AppSecret，注意：修改后通知开发回收应用程序' ,18)  
END  
  
--增加业主第三方URL对应的Token 向以胜 2020-04-23  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'OwnerWeixinToken')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES ('OwnerWeixinToken' ,'业主第三方URL对应的Token' ,'jxdOwner' ,'' ,'业主第三方URL对应的Token，注意：修改后通知开发回收应用程序池' ,19)  
END  
--增加业主第三方URL对应的消息加解密密钥 向以胜 2020-04-23  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'OwnerWeixinEncodingAESKey')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES ('OwnerWeixinEncodingAESKey' ,'业主第三方URL对应的消息加解密密钥' ,'HJP7ca6FZPmQiiwkBEANOC8NU5zPsTliHJ4ncueJwNC' ,'' ,'业主第三方URL对应的消息加解密密钥，注意：修改后通知开发回收应用程序' ,19)  
END  
--增加业主微信AppId 向以胜 2020-04-23  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'OwnerWeixinAppId')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES ('OwnerWeixinAppId' ,'业主微信AppId' ,'wx21cce2dcf20a61f5' ,'' ,'业主微信AppId，注意：修改后通知开发回收应用程序' ,19)  
END  
--增加业主微信AppSecret 向以胜 2020-04-23  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'OwnerWeixinAppSecret')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES ('OwnerWeixinAppSecret' ,'业主微信AppSecret' ,'4806ab0e4db6d7fee292de32c6403c6d' ,'' ,'业主微信AppSecret，注意：修改后通知开发回收应用程序' ,19)  
END  
  
--版本系统参数关联表新增产品类型字段 熊壮 2019-12-17  
if not exists(select * from syscolumns where id=object_id('versionParas') and name='vProduct')  
begin  
 alter table dbo.versionParas add vProduct varchar(60) null  
  exec('update versionParas set vProduct=''pms'' where vProduct is null')  
end  
  
--增加拥有自己Master的数据库，李泽锐，2020-4-23 11:16:53  
--用于验证是否能同步酒店信息到代理商的Master  
--需开发代理商的安全策略给4台运营服务器，并在我们主master数据库设置代理商master所在的IP链接服务器  
if not exists(select * from sys.tables where name = 'OwnMaster')  
begin  
 create table OwnMaster(  
  Id uniqueidentifier not null,--主键值  
  dbid uniqueidentifier not null,--主键值  
 )  
end  
  
--增加定时短信表 李志伟 2020-04-29  
if not exists(select id from sysobjects where name = 'TimingRun')  
begin  
 --定时运行 设置表 pmsMaster  
 create table TimingRun(  
  id varchar(32) primary key not null,--主键ID  
  hid char(6) not null,--酒店ID  
  timingType tinyint not null,--类型(1短信，2待扩展)  
  timingId varchar(100) not null,--类型表ID（smsTiming.id）  
  runDateTime datetime not null,--运行时间  
  runStatus tinyint not null,--运行状态（0未运行，1正在运行，2运行完毕）  
 )  
end  
  
--判断是否存在已开通ipos但未设置ipos模块的酒店hid 熊壮 2020-05-13  
if exists(select distinct * from (  
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
end  
--增加捷信达扫呗收款的相关参数信息，用于客户直接购买捷信达的商品，比如捷音激活点数，陈前良，2020-5-14 9:38:43  
if not exists(select * from sysPara where code = 'jxdPaymentUrl')  
begin  
 insert into sysPara(code,name,refValue,value,remark,seqid) values('jxdPaymentUrl','捷信达支付中间件地址','http://pay.gshis.net/clientpay','http://pay.gshis.net/clientpay','捷信达支付中间件地址',200)  
 insert into sysPara(code,name,refValue,value,remark,seqid) values('jxdMerchantNo','捷信达扫呗收款账号','858400203000108','858400203000108','捷信达扫呗收款账号',200)  
 insert into sysPara(code,name,refValue,value,remark,seqid) values('jxdTerminalId','捷信达扫呗终端号','11732516','11732516','捷信达扫呗终端号',200)  
 insert into sysPara(code,name,refValue,value,remark,seqid) values('jxdToken','捷信达终端令牌','11ff5a11bbff42888391757a45e71f76','11ff5a11bbff42888391757a45e71f76','捷信达终端令牌',200)  
end  
--增加捷音激活码的购买者openid，以便跟进是谁自助购买的  
if not exists(select * from INFORMATION_SCHEMA.columns where table_name = 'HotelVoiceQtys' and column_name = 'openid')  
begin  
 alter table HotelVoiceQtys add openid varchar(60) null  
end  
--酒店表增加交接区域，林涛 2020-05-20 10:51: 36 job：152806  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('hotel') AND name = 'handoverArea')  
BEGIN  
 ALTER TABLE hotel ADD handoverArea VARCHAR(100) NULL  
END   
--酒店表增加微信开锁类型，林涛 2020-05-26 16:51: 36 job：155260  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('hotel') AND name = 'WeiXinLockType')  
BEGIN  
 ALTER TABLE hotel ADD WeiXinLockType VARCHAR(50) NULL  
END  
  
--POS增加微信公众号token和ticket记录表  ves  2020-06-01  
if not exists(select 1 from sysobjects where name ='POSMerchantTokenTicket')  
begin  
CREATE TABLE [dbo].[POSMerchantTokenTicket]  
(  
[id] bigint NOT NULL IDENTITY(1,1) PRIMARY KEY,  --序列号  
[hid] varchar(20) NOT NULL,    --酒店ID  
[appid] varchar(2000) not null,--appid  
[secret] varchar(2000) not null,--appsecret  
[token] varchar(2000) not null, --token的值  
[tokenCreateTime] datetime NOT NULL,  --token生效起始时间时间  
[tokenLimitTime] int null, --token时效（单位：s） 目前是7200秒  
[ticket] varchar(2000) null, --ticket的值  
[ticketCreateTime] datetime NOT NULL,  --ticket生效起始时间时间  
[ticketLimitTime] int null --ticket时效（单位：s） 目前是7200秒  
)  
end  
  
--增加运营平台参数：是否使用服务器地址 lzr 2020-6-6 16:32:34  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'IsUsedServerAddress')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'IsUsedServerAddress' ,'是否使用服务器地址' ,'1' ,'1' ,'启用后跳转加上服务器地址(例：vip1.pms.gshis.com)，不启用则使用原域名(例：pms.gshis.com)；1：启用（默认）；0：不启用' ,9998)  
END  
--增加人脸设备所属营业点字段，默认为空。为空表示不受营业点限制。陈前良，2020-6-8 10:13:0  
if not exists(select * from INFORMATION_SCHEMA.columns where table_name = 'FaceDevices' and column_name = 'BusinessPointCode')  
begin  
 alter table FaceDevices add BusinessPointCode varchar(30) null --人脸设备安装位置所属营业点代码  
end  
--增加运营平台参数：tts语音播放接口地址，陈前良，2020-6-15 18:43:10  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'ttsServiceUrl')  
BEGIN  
    INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
    VALUES  ( 'ttsServiceUrl' ,'tts语音播放接口地址' ,'http://tts.gshis.com/speak/' ,'http://tts.gshis.com/speak/' ,'用于将文本转换成语音流，以便于网页中通过Audio播放语音提醒' ,9998)  
END  
  
--增加运营平台参数：是否使用Https协议 Jerry 2020-6-22 11:39:00  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'IsUsedHttps')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'IsUsedHttps' ,'是否使用Https协议' ,'0' ,'0' ,'1：启用；0：不启用（默认）' ,9998)  
END  
--酒店表增加集团类型，林涛 2020-06-22 16:51: 36  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('hotel') AND name = 'GroupType')  
BEGIN  
 ALTER TABLE hotel ADD GroupType VARCHAR(50) NULL  
END  
  
-- 增加ispa 安排模版消息ID  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'ISPWeiXinTemplateIDPlan')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'ISPWeiXinTemplateIDPlan' ,'ispa 安排模版消息ID' ,'68d4grtT_dFsmlpmf6v3key9GWq58T1i23xj1uj3doE' ,'68d4grtT_dFsmlpmf6v3key9GWq58T1i23xj1uj3doE' ,'ispa 安排模版消息ID' ,610)  
end  
  
--增加 会员接口 lizw 2020-06-30  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('hotelPos') AND name = 'ProfileInterfaceUrl')  
BEGIN  
 ALTER TABLE hotelPos ADD ProfileInterfaceUrl VARCHAR(800) NULL;  
 ALTER TABLE hotelPos ADD ProfileFuncCode VARCHAR(800) NULL;  
 ALTER TABLE hotelPos ADD ProfileFuncSecretKey VARCHAR(800) NULL;  
END  
  
--增加捷云达人汇云工程模板参数 熊壮 2020-07-06  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'TemplateIDEngineering')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES ('TemplateIDEngineering' ,'捷云达人汇报修模板' ,'xWv-ZHvbvyGOqbKFHyfCCjmmJmDHyS6wm_O81lgImd4' ,'xWv-ZHvbvyGOqbKFHyfCCjmmJmDHyS6wm_O81lgImd4' ,'云工程 - 报修模板id' ,30)  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES ('TemplateIDComplaint' ,'捷云达人汇投诉模板' ,'4q_kChg-yZFa25r0kUXgtDLPQ_4OlB16w9AB4tqgP-8' ,'4q_kChg-yZFa25r0kUXgtDLPQ_4OlB16w9AB4tqgP-8' ,'云工程 - 投诉模板id' ,30)  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES ('TemplateIDClean' ,'捷云达人汇保洁模板' ,'InzsO7HWESGxKyg-lLD1iU557zu_22kvDcawKYycg38' ,'InzsO7HWESGxKyg-lLD1iU557zu_22kvDcawKYycg38' ,'云工程 - 保洁模板id' ,30)  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES ('TemplateIDMove' ,'捷云达人汇搬家模板' ,'BKFZRuty-XnWSzjBoapGj38AJOa2BJL2TF8SvnimA3s' ,'BKFZRuty-XnWSzjBoapGj38AJOa2BJL2TF8SvnimA3s' ,'云工程 - 搬家模板id' ,30)  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES ('TemplateIDOrder' ,'捷云达人汇预约模板' ,'xWv-ZHvbvyGOqbKFHyfCCjmmJmDHyS6wm_O81lgImd4' ,'xWv-ZHvbvyGOqbKFHyfCCjmmJmDHyS6wm_O81lgImd4' ,'云工程 - 预约模板id' ,30)  
END  
  
--新增更新帮助文档Code代码的值，用于区分各个业务系统 熊壮 2020-07-07  
IF NOT EXISTS(SELECT * FROM helpFiles WHERE code = 'pms')  
BEGIN  
 UPDATE helpFiles SET code='pms'  
 WHERE title LIKE'%客房%'  
END  
IF NOT EXISTS(SELECT * FROM helpFiles WHERE code = 'member')  
BEGIN  
 UPDATE helpFiles SET code='member'  
 WHERE title LIKE'%会员%'  
END  
IF NOT EXISTS(SELECT * FROM helpFiles WHERE code = 'corp')  
BEGIN  
 UPDATE helpFiles SET code='corp'  
 WHERE title LIKE'%合约%'  
END  
  
--增加一个版本，模块模板酒店设置表，陈前良，如果此表中没有数据，则表示取版本中的模板酒店信息，2019-5-31 9:49:1  
if OBJECT_ID('versionProductModels') is null  
BEGIN  
  create TABLE versionProductModels(  
      id uniqueidentifier not null,       --id  
      versionId uniqueidentifier not null,--版本id  
      productCode varchar(30) not null,   --产品模块代码  
      modelHid varchar(6) null,           --模板酒店id  
      constraint pk_versionProductModels PRIMARY key(id)  
  )  
END  
--给数据库增加适用的产品模块和不适用的产品模块列，以防止在给产品模块选择数据库实例时，只能选择那些支持对应产品模块的数据库，以防止选择错误，陈前良，2019-6-3 16:21:31  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id = OBJECT_ID('dbList') AND name = 'supportedProducts')  
BEGIN  
  ALTER TABLE dbList ADD supportedProducts VARCHAR(200) NULL,   --支持的产品模块，为空表示除了明确在不支持的产品模块中列出的模块外都支持，有值则表示只支持指定的模块，多项之间以逗号分隔  
    unsupportedProducts VARCHAR(200) NULL           --不支持的产品模块，有值则表示明确不支持指定的模块，多项之间以逗号分隔，为空同表示没有不支持的产品模块  
END   
  
--修改酒店产品模块表，增加产品模块业务数据库id和状态列，陈前良，2019-6-3 18:15:53  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('hotelProducts') AND name = 'dbid')  
BEGIN  
  ALTER TABLE hotelProducts ADD dbid UNIQUEIDENTIFIER NULL,isEnable BIT NOT NULL DEFAULT 1  
END   
-- 新增接口请求身份验证表 肖念 2020-07-15  
if OBJECT_ID('IdentityRequest') is null  
BEGIN  
create table IdentityRequest (  
   original             uniqueidentifier     not null,  
   grpidHotel           char(6)              not null,  
   cdate                datetime             not null,  
   cUser                varchar(100)         null,  
   remark               varchar(800)         null,  
   md5key               varchar(40)          not null,  
   constraint PK_IDENTITYREQUEST primary key (original)  
)  
END  
  
-- 新增ispa用户绑定表 张勃  
if OBJECT_ID('HotelUserWxInfo') is null  
BEGIN  
CREATE TABLE [dbo].[HotelUserWxInfo](  
 [Id] [uniqueidentifier] NOT NULL,  
 [HotelCode] [char](6) NOT NULL,  
 [UserCode] [varchar](100) NOT NULL,  
 [WxOpenId] [varchar](100) NOT NULL,  
 [WxAppId] [varchar](100) NOT NULL,  
 [IType] [varchar](20) NOT NULL,  
 [CreateDate] [datetime] NOT NULL,  
 [ModifyDate] [datetime] NOT NULL,  
 CONSTRAINT [PK_HotelUserWxInfo] PRIMARY KEY CLUSTERED   
(  
 [Id] ASC  
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]  
) ON [PRIMARY]  
END  
  
--增加notify接口通知地址参数 林涛 2020-07-22  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'PmsNotifyUrl')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'PmsNotifyUrl' ,'notify接口通知地址' ,'http://pmsnotify.gshis.com/DataExchange/Index' ,'http://pmsnotify.gshis.com/DataExchange/Index' ,'notify接口通知地址' ,50)  
END  
  
--延期日志添加延期产品字段 林涛 2020-7-28  
if(not exists(select 1 from syscolumns where id=object_id('HotelDelayLogs') and [name]='UContent'))  
begin  
 alter table HotelDelayLogs add UContent varchar(50) null  
end  
  
  
-- 增加ispa 服务超时 模版消息ID  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'ISPAUpOverWeiXinTemplateID')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'ISPAUpOverWeiXinTemplateID' ,'ispa 服务超时通知模版消息ID' ,'_6iDu0CSCRmFSZKoni_OXiobcV9FnV73wFrmL9rkOMA' ,'_6iDu0CSCRmFSZKoni_OXiobcV9FnV73wFrmL9rkOMA' ,'ispa 服务超时通知模版消息ID' ,611)  
end  
--增加人脸设备数据同步方式，前端比对失败后的处理方式，前端比对失败后的处理接口地址。陈前良，2020-7-17 10:29:33  
if not exists(select * from INFORMATION_SCHEMA.columns where table_name = 'FaceDevices' and column_name = 'SyncType')  
begin  
  alter table FaceDevices add SyncType smallint not null default 1,--数据同步方式，0：不同步，1：白名单同步，2：刷IC卡获取  
    HandleTypeOnFail smallint not null default 0,--前端比对失败后的处理方式，0：不处理，1：采集通知，2：商汤静态比对  
    HandleUrlOnFail varchar(200) null,--前端比对失败后的处理接口地址，比如商汤静态服务接口地址  
  HandleSignKey varchar(100) null--处理接口加密密钥  
end  
  
--系统公告增加字段“产品” lzr 2020-8-10  
if(not exists(select 1 from syscolumns where id=object_id('Notice') and [name]='products'))  
begin  
 alter table Notice add products varchar(1000) null  
end  
  
--广告增加字段“产品” lzr 2020-8-11  
if(not exists(select 1 from syscolumns where id=object_id('adSet') and [name]='products'))  
begin  
 alter table adSet add products varchar(1000) null  
end  
  
--微信二维码增加[products]产品字段 熊壮 2020-08-20 15:39  
if(not exists(select 1 from syscolumns where id=object_id('weixinQrcodes') and [name]='products'))  
begin  
 alter table weixinQrcodes add products varchar(200) null  
end  
  
if not exists(select id from sysobjects where name = 'smsSetting')  
begin  
 --短信设置  
 create table smsSetting(  
  hid char(6) not null primary key, --酒店ID  
  balance int,--短信余额数量  
  balanceUpdateDatetime datetime,--短信余额数量更新时间  
  alarmBalance int,--报警余额数量  
  alarmMobile varchar(30)--报警手机号  
 )  
end  
  
--酒店表增加[WQServicesModule]温泉功能模块权限 majx 2020-09-09 15:39  
if(not exists(select 1 from syscolumns where id=object_id('Hotel') and [name]='WQServicesModule'))  
begin  
 alter table Hotel add WQServicesModule varchar(200) null  
END  
  
  
--ispa退选 微信消息推送ID 黄鸿斌 2020年9月29日  
IF(NOT EXISTS(SELECT 1 FROM sysPara WHERE code = 'ISPAWeiXinTemplateIDQuitSelect'))  
BEGIN  
 INSERT INTO dbo.sysPara  
        ( code ,  
          name ,  
          refValue ,  
          value ,  
          remark ,  
          seqid  
        )  
 VALUES  ( 'ISPAWeiXinTemplateIDQuitSelect' , -- code - varchar(30)  
          'ispa 退选通知模版ID' , -- name - varchar(300)  
          '_6iDu0CSCRmFSZKoni_OXiobcV9FnV73wFrmL9rkOMA' , -- refValue - varchar(300)  
          'OuJTKJpSo-1HzKgneBLuyvOOUaPo0TYfONwxQ0XeGfw' , -- value - varchar(3000)  
          'ispa 退选通知模版ID' , -- remark - varchar(300)  
          614  -- seqid - int  
        );  
END   
  
--ispa预约 微信消息推送ID 黄鸿斌 2020年9月29日  
IF(NOT EXISTS(SELECT 1 FROM sysPara WHERE code = 'ISPAWeiXinTemplateIDBook'))  
BEGIN  
 INSERT INTO dbo.sysPara  
        ( code ,  
          name ,  
          refValue ,  
          value ,  
          remark ,  
          seqid  
        )  
 VALUES  ( 'ISPAWeiXinTemplateIDBook' , -- code - varchar(30)  
          'ispa 预约成功通知模版ID' , -- name - varchar(300)  
          '_6iDu0CSCRmFSZKoni_OXiobcV9FnV73wFrmL9rkOMA' , -- refValue - varchar(300)  
          'OuJTKJpSo-1HzKgneBLuyvOOUaPo0TYfONwxQ0XeGfw' , -- value - varchar(3000)  
          'ispa 预约成功通知模版ID' , -- remark - varchar(300)  
          615  -- seqid - int  
        );   
END   
  
-- 新增版本列表  
if OBJECT_ID('productVersionList') is null  
BEGIN  
create table productVersionList (  
   id   uniqueidentifier    not null,  
   productCode  varchar(30)   not null,  
   vCode  varchar(30)   not null,  
   vName  varchar(100)  not null,  
   roomNum  int     null,  
   seqid  int     null,  
   constraint PK_productVersionList primary key (id)  
)  
END  
  
--酒店表增加房间房数上限，李泽锐 2020-11-2 18:11:08  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('hotel') AND name = 'maxRoomNum')  
BEGIN  
 ALTER TABLE hotel ADD maxRoomNum int null  
END  
  
--增加参数signal数据库名称  lxj 2020-11-16  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'signalrDBServer')  
BEGIN  
INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)VALUES  ( 'signalrDBName' ,'signal数据库名称' ,'posSignalRDev' ,'posSignalRDev' ,'signalr数据库名称' ,700)  
INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)VALUES  ( 'signalrDBPassword' ,'signal数据库密码' ,'1gFvX4nmjhI=' ,'1gFvX4nmjhI=' ,'signal数据库密码' ,701)  
INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)VALUES  ( 'signalrDBServer' ,'signal数据库服务器地址' ,'192.168.1.111\server2008' ,'192.168.1.111\server2008' ,'signal数据库服务器地址' ,702)  
INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)VALUES  ( 'signalrDBUser' ,'signal数据库用户名' ,'jxd' ,'jxd' ,'signal数据库用户名' ,703)  
END  
  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'ispaislogtxt')  
BEGIN  
--增加ispa参数 是否记本地日志参数  
INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)VALUES  ( 'ispaislogtxt' ,'ISPA是否记本地日志' ,'0' ,'0' ,'ISPA是否记本地日志' ,720)  
END  
  
--增加获取微信用户信息URL 向以胜 2020-11-23  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'GetUserInfoUrl')  
BEGIN  
 INSERT INTO sysPara(code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ('GetUserInfoUrl' ,'获取微信用户信息URL' ,'' ,'' ,'获取微信用户信息URL，hid可设置固定参数，或使用@hid取当前酒店ID' ,505)  
END  
  
--修改酒店产品模块表，增加产品模块创建时间，李泽锐，2020-12-8 20:23:512  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('hotelProducts') AND name = 'cDate')  
BEGIN  
  ALTER TABLE hotelProducts ADD cDate datetime default getdate()  
  declare @sql varchar(1000) = 'UPDATE hp SET hp.cDate = h.createDate FROM dbo.hotelProducts hp INNER JOIN dbo.hotel h ON h.hid = hp.hid and h.createDate is not null WHERE hp.cDate IS NULL'  
  exec(@sql)  
END  
  
-- 试用信息表增加试用产品字段 江锦 2021/1/5 jobid=159230  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('TryInfos') AND name = 'Product')  
BEGIN  
 ALTER TABLE dbo.TryInfos ADD Product nvarchar(50) NULL --管理费  
END  
  
--huanghb 2021年1月7日 增加记录微信昵称  
IF NOT EXISTS(SELECT 1 FROM syscolumns WHERE id=OBJECT_ID('HotelUserWxInfo') AND name = 'NickName')  
BEGIN  
 ALTER TABLE HotelUserWxInfo ADD NickName VARCHAR(28)  NULL  
END  
-- huanghb 2021年1月7日   推送微信模板消息增加保存技师号  
IF NOT EXISTS(SELECT 1 FROM syscolumns WHERE id=OBJECT_ID('HotelUserWxInfo') AND name = 'ArtNo')  
BEGIN  
 ALTER TABLE HotelUserWxInfo ADD ArtNo VARCHAR(6)  NULL  
END  
  
-- huanghb 2021年1月7日   推送微信模板消息增加保存微信UnionID  
IF NOT EXISTS(SELECT 1 FROM syscolumns WHERE id=OBJECT_ID('HotelUserWxInfo') AND name = 'UnionID')  
BEGIN  
 ALTER TABLE HotelUserWxInfo ADD UnionID VARCHAR(100)  NULL  
END  
  
-- Jerry 2021-1-15 酒店信息表增加字段“产品特性”[hotel.character]  
IF NOT EXISTS(SELECT 1 FROM syscolumns WHERE id=OBJECT_ID('hotel') AND name = 'character')  
BEGIN  
 ALTER TABLE hotel ADD character VARCHAR(100)  NULL  
END  
  
-- huanghb 2021年1月26日 ISPA酒店微信用户增加微信名称长度   
IF EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('HotelUserWxInfo') AND name = 'NickName' AND length = 28)  
BEGIN  
 ALTER TABLE HotelUserWxInfo ALTER COLUMN NickName VARCHAR(255)  
END   
  
  
-- 酒店产品表（hotelProducts）增加 Versin （版本字段） 江锦 2021/2/1 jobid=163956  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('hotelProducts') AND name = 'Version')  
BEGIN  
 ALTER TABLE dbo.hotelProducts ADD [Version] nvarchar(30) NULL -- 产品版本  
  
 -- 同步原来的捷云客房版本，云会员版本到酒店产品表 hotelProducts  
 --1.找到有设置了版本的酒店  
 SELECT   
  hid  
  ,CAST('' AS VARCHAR(50)) AS pmsVersionCode   
  ,productType AS pmsVersionName  
  ,CAST('' AS VARCHAR(50)) AS memberVersionCode  
  ,prodMbrType AS memberVersionName  
 INTO #temp_hotel  
 FROM dbo.hotel    
 WHERE LTRIM(RTRIM(ISNULL(productType,''))) != '' OR LTRIM(RTRIM(ISNULL(prodMbrType,''))) != ''  
  
 --2.通过酒店ID 和 版本名称 找到 版本代码，把版本代码 更新到 临时表  
 UPDATE h SET h.pmsVersionCode = versionList.vCode FROM #temp_hotel AS h   
 INNER JOIN dbo.ProductVersionList AS versionList ON h.pmsVersionName = versionList.vName  
 WHERE versionList.productCode = 'pms' ;   
  
 UPDATE h SET h.memberVersionCode = versionList.vCode FROM #temp_hotel AS h   
 INNER JOIN dbo.ProductVersionList AS versionList ON h.memberVersionName = versionList.vName  
 WHERE versionList.productCode = 'member';   
  
 --3.酒店产品表 酒店ID + 产品代码 更新的是 version列  
 UPDATE hp SET hp.[Version] = h.pmsVersionCode  
 FROM dbo.hotelProducts hp  
 INNER JOIN #temp_hotel h ON hp.hid = h.hid AND hp.productCode = 'pms' AND h.pmsVersionCode != ''  
  
 UPDATE hp SET hp.[Version] = h.memberVersionCode  
 FROM dbo.hotelProducts hp  
 INNER JOIN #temp_hotel h ON hp.hid = h.hid AND hp.productCode = 'member' AND h.memberVersionCode != ''  
  
 -- 4. 删除临时表  
 drop table #temp_hotel  
  
END  
  
--iPos增加 功能模块  20210220 jiangcs   
if not exists(select 1 from  syscolumns where id=OBJECT_ID('posSmMappingHid') AND  name = 'FunctionModule')  
BEGIN   
 alter  table  posSmMappingHid add  FunctionModule varchar(500) NULL   --功能模块  
END  
  
--增加星云版的平台参数 Jerry 2021年3月2日 jobid：165981  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'nebulaTitle')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)VALUES  ( 'nebulaTitle' ,'星云版标题' ,'星云Nebula酒店管理软件 V1.0' ,'星云Nebula酒店管理软件 V1.0' ,'星云版标题' , 70)  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)VALUES  ( 'nebulaShortTitle' ,'星云版标题简称' ,'星云PMS' ,'星云PMS' ,'星云版标题简称' ,71)  
END  
  
  
  
/** SCM合并脚本 开始 **/  
  
  
if not exists(select * from syscolumns where id = object_id('sysPara') and name = 'IsHide')  
BEGIN  
ALTER TABLE dbo.sysPara  
ADD IsHide BIT  
end  
--增加微信参数的配置  
IF NOT EXISTS (SELECT * FROM dbo.sysPara WHERE code='SCMBindWeChatHttp')  
BEGIN  
INSERT INTO dbo.sysPara  
        ( code ,  
          name ,  
          refValue ,  
          value ,  
          remark ,  
          seqid  
        )  
VALUES  ( 'SCMBindWeChatHttp' , -- code - varchar(30)  
          'SCM微信绑定跳转地址' , -- name - varchar(300)  
          'http://www.gshis.net/oauth/toOauthInterface.do?comid=594c6b08d5e1ea44c958a103&code=ScmCloudBindWx&arg=' , -- refValue - varchar(300)  
          'http://www.gshis.net/oauth/toOauthInterface.do?comid=594c6b08d5e1ea44c958a103&code=ScmCloudBindWx&arg=' , -- value - varchar(3000)  
          '' , -- remark - varchar(300)  
          1000  -- seqid - int  
        )  
end  
IF NOT EXISTS (SELECT * FROM dbo.sysPara WHERE code='SCMWeChatMessageModelID')  
BEGIN  
INSERT INTO dbo.sysPara  
        ( code ,  
          name ,  
          refValue ,  
          value ,  
        remark ,  
          seqid  
        )  
VALUES  ( 'SCMWeChatMessageModelID' , -- code - varchar(30)  
          'SCM微信消息模板ID' , -- name - varchar(300)  
          'esk2n4vAMLLVHm9Lf9SLRT5K2NrrCttGR3TNLRlRdnE' , -- refValue - varchar(300)  
          'esk2n4vAMLLVHm9Lf9SLRT5K2NrrCttGR3TNLRlRdnE' , -- value - varchar(3000)  
          '' , -- remark - varchar(300)  
          1001  -- seqid - int  
        )  
END  
IF NOT EXISTS (SELECT * FROM dbo.sysPara WHERE code='SCMSendModelMessageHttp')  
BEGIN  
INSERT INTO dbo.sysPara  
        ( code ,  
          name ,  
          refValue ,  
          value ,  
          remark ,  
          seqid  
        )  
VALUES  ( 'SCMSendModelMessageHttp' , -- code - varchar(30)  
          'SCM微信模板消息发送地址' , -- name - varchar(300)  
          'http://www.gshis.net/weixinInterface/sendBillsInfoMsg.do' , -- refValue - varchar(300)  
          'http://www.gshis.net/weixinInterface/sendBillsInfoMsg.do' , -- value - varchar(3000)  
          '' , -- remark - varchar(300)  
          1002  -- seqid - int  
        )  
END  
IF NOT EXISTS (SELECT * FROM dbo.sysPara WHERE code='SCMWeChatMessagelink')  
BEGIN  
INSERT INTO dbo.sysPara  
        ( code ,  
          name ,  
          refValue ,  
          value ,  
          remark ,  
          seqid  
        )  
VALUES  ( 'SCMWeChatMessagelink' , -- code - varchar(30)  
          'SCM微信模板消息链接地址' , -- name - varchar(300)  
          'http://www.gshis.net/oauth/toOauthInterface.do?comid=594c6b08d5e1ea44c958a103&code=ScmCloudSendBillsInfoMsg&arg=' , -- refValue - varchar(300)  
          'http://www.gshis.net/oauth/toOauthInterface.do?comid=594c6b08d5e1ea44c958a103&code=ScmCloudSendBillsInfoMsg&arg=' , -- value - varchar(3000)  
          '' , -- remark - varchar(300)  
          1003  -- seqid - int  
        )  
END  
IF NOT EXISTS (SELECT * FROM dbo.sysPara WHERE code='SCMWeChatsecretkey')  
BEGIN  
INSERT INTO dbo.sysPara  
        ( code ,  
          name ,  
          refValue ,  
          value ,  
          remark ,  
          seqid  
        )  
VALUES  ( 'SCMWeChatsecretkey' , -- code - varchar(30)  
          'SCM微信秘钥' , -- name - varchar(300)  
          'scmNet1101115806' , -- refValue - varchar(300)  
          'scmNet1101115806' , -- value - varchar(3000)  
          '' , -- remark - varchar(300)  
          1004  -- seqid - int  
        )  
END  
IF NOT EXISTS (SELECT * FROM dbo.sysPara WHERE code='SCMSystemNetworkHttp')  
BEGIN  
INSERT INTO dbo.sysPara  
        ( code ,  
          name ,  
          refValue ,  
          value ,  
          remark ,  
          seqid  
        )  
VALUES  ( 'SCMSystemNetworkHttp' , -- code - varchar(30)  
          'SCM系统外网地址' , -- name - varchar(300)  
          'http://scm.gshis.com' , -- refValue - varchar(300)  
          'http://scm.gshis.com' , -- value - varchar(3000)  
          '系统外网地址,微信绑定的跳转地址' , -- remark - varchar(300)  
          1005  -- seqid - int  
        )  
END  
  
--新增采购系统需要的表  
if object_id('HotelBrand') is null   
BEGIN  
CREATE TABLE [dbo].[HotelBrand](  
 [ID] [VARCHAR](100) NOT NULL,  
 [HotelID] [VARCHAR](10) NOT NULL,  
 [Name] [VARCHAR](50) NOT NULL,  
 [CreateTime] [DATETIME] NULL,  
PRIMARY KEY CLUSTERED   
(  
 [ID] ASC  
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]  
) ON [PRIMARY]  
END  
if object_id('HotelLevel') is null   
BEGIN  
CREATE TABLE [dbo].[HotelLevel](  
 [LevelID] [VARCHAR](100) NOT NULL,  
 [Name] [VARCHAR](50) NOT NULL,  
 [SeqId] [INT] NULL,  
 [IsStandard] [INT] NULL,  
 [EditionName] [VARCHAR](50) NULL,  
PRIMARY KEY CLUSTERED   
(  
 [LevelID] ASC  
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]  
) ON [PRIMARY]  
END  
if object_id('HotelRole') is null   
BEGIN  
CREATE TABLE [dbo].[HotelRole](  
 [ID] [INT] IDENTITY(1,1) NOT NULL,  
 [HotelID] [VARCHAR](50) NULL,  
 [RoleID] [VARCHAR](30) NULL,  
PRIMARY KEY CLUSTERED   
(  
 [ID] ASC  
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]  
) ON [PRIMARY]  
END  
if object_id('LevelRole') is null   
BEGIN  
CREATE TABLE [dbo].[LevelRole](  
 [ID] [INT] IDENTITY(1,1) NOT NULL,  
 [LevelID] [VARCHAR](100) NULL,  
 [RoleID] [VARCHAR](30) NULL,  
 [RoleName] [VARCHAR](100) NULL,  
 [SeqId] [INT] NULL,  
PRIMARY KEY CLUSTERED   
(  
 [ID] ASC  
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]  
) ON [PRIMARY]  
END  
if object_id('ItemCategory') is null   
BEGIN  
CREATE TABLE [dbo].[ItemCategory](  
 [ID] [VARCHAR](10) NOT NULL,  
 [Name] [VARCHAR](20) NOT NULL,  
 [ParentID] [VARCHAR](10) NOT NULL,  
 [SeachCount] [INT] NOT NULL,  
 [LastUpdate] [VARCHAR](20) NOT NULL,  
 [LastUpdateDate] [DATETIME] NOT NULL,  
 [SeqID] [INT] NOT NULL,  
PRIMARY KEY CLUSTERED   
(  
 [ID] ASC  
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]  
) ON [PRIMARY]  
END  
  
----------  
if object_id('Supplier') is null   
BEGIN  
CREATE TABLE [dbo].[Supplier](  
 [ID] [VARCHAR](100) NOT NULL,  
 [Name] [VARCHAR](100) NOT NULL,  
 [ShortName] [VARCHAR](100) NOT NULL,  
 [TaxRegistrationNo] [VARCHAR](100) NOT NULL,  
 [EnterpriseLegalPerson] [VARCHAR](100) NOT NULL,  
 [EstablishTime] [DATETIME] NULL,  
 [Email] [VARCHAR](100) NOT NULL,  
 [Fax] [VARCHAR](100) NOT NULL,  
 [Mobile] [VARCHAR](100) NOT NULL,  
 [Phone] [VARCHAR](100) NOT NULL,  
 [Contacts] [VARCHAR](100) NOT NULL,  
 [Address] [VARCHAR](100) NOT NULL,  
 [TaxRate] [DECIMAL](18, 2) NULL,  
 [Province] [VARCHAR](100) NOT NULL,  
 [City] [VARCHAR](100) NOT NULL,  
 [Area] [VARCHAR](100) NULL,  
 [Bank] [VARCHAR](100) NULL,  
 [BankAccount] [VARCHAR](100) NULL,  
 [Status] [INT] NOT NULL,  
 [Code] [VARCHAR](50) NOT NULL,  
 [FromHotelID] [VARCHAR](10) NULL,  
 [FromHotelName] [VARCHAR](100) NULL,  
 [SeachCount] [INT] NULL,  
 [TradingVolume] [DECIMAL](18, 4) NULL,  
 [TradingAmount] [DECIMAL](18, 4) NULL,  
 [LastPurchaseDate] [DATETIME] NULL,  
 [Remark] [VARCHAR](400) NULL,  
 [PurchaseAmount] [DECIMAL](18, 4) NULL,  
PRIMARY KEY CLUSTERED   
(  
 [ID] ASC  
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]  
) ON [PRIMARY]  
END  
if object_id('SupplierContact') is null   
BEGIN  
CREATE TABLE [dbo].[SupplierContact](  
 [ID] [NVARCHAR](50) NOT NULL,  
 [Name] [NVARCHAR](50) NOT NULL,  
 [SupplierID] [NVARCHAR](50) NOT NULL,  
 [IsMainContact] [BIT] NOT NULL,  
 [Tel] [NVARCHAR](50) NULL,  
 [Mobile] [NVARCHAR](50) NOT NULL,  
 [Email] [NVARCHAR](50) NULL,  
 [ImagePath] [NVARCHAR](250) NULL,  
 [Remark] [NVARCHAR](1000) NULL,  
 [WeChatCode] [NVARCHAR](50) NULL,  
 [ImageName] [VARCHAR](400) NULL,  
 CONSTRAINT [PK_SupplerContact] PRIMARY KEY CLUSTERED   
(  
 [ID] ASC  
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]  
) ON [PRIMARY]  
END  
if object_id('SupplierFeedBack') is null   
BEGIN  
CREATE TABLE [dbo].[SupplierFeedBack](  
 [ID] [VARCHAR](50) NOT NULL,  
 [Content] [VARCHAR](500) NOT NULL,  
 [BackMan] [VARCHAR](20) NOT NULL,  
 [BackDate] [DATETIME] NOT NULL,  
 [FromHotelID] [VARCHAR](10) NOT NULL,  
 [FromHotenName] [VARCHAR](50) NOT NULL,  
 [FromSupplierID] [VARCHAR](50) NULL,  
 [FromSupplierName] [VARCHAR](50) NULL,  
PRIMARY KEY CLUSTERED   
(  
 [ID] ASC  
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]  
) ON [PRIMARY]  
END  
if object_id('SupplierItemCategrory') is null   
BEGIN  
CREATE TABLE [dbo].[SupplierItemCategrory](  
 [ID] [NVARCHAR](50) NOT NULL,  
 [ItemCategroryName] [NVARCHAR](50) NOT NULL,  
 [ItemCategroryID] [NVARCHAR](50) NOT NULL,  
 [Status] [INT] NOT NULL,  
 [SupplierID] [NVARCHAR](50) NOT NULL,  
 CONSTRAINT [PK_SupplierItemCategrory_1] PRIMARY KEY CLUSTERED   
(  
 [ID] ASC  
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]  
) ON [PRIMARY]  
END  
if object_id('SupplierQualification') is null   
BEGIN  
CREATE TABLE [dbo].[SupplierQualification](  
 [ID] [NVARCHAR](50) NOT NULL,  
 [Explain] [NVARCHAR](50) NULL,  
 [SupplierID] [NVARCHAR](50) NOT NULL,  
 [Type] [NVARCHAR](50) NOT NULL,  
 [Level] [NVARCHAR](50) NOT NULL,  
 [ExpireDate] [DATE] NOT NULL,  
 [IssuingAuthority] [NVARCHAR](50) NOT NULL,  
 [ImagePath] [VARCHAR](250) NULL,  
 [ImageName] [VARCHAR](400) NULL,  
 CONSTRAINT [PK_SupplierQualification] PRIMARY KEY CLUSTERED   
(  
 [ID] ASC  
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]  
) ON [PRIMARY]  
END  
  
--酒店表增加外部代码字段 2019-3-27 13:44:59  
if not exists(select * from syscolumns where id = OBJECT_ID('Hotel') and name = 'OutsideCode')  
begin  
 ALTER TABLE Hotel  
 ADD OutsideCode VARCHAR(30)  
END  
if not exists(select * from syscolumns where id = OBJECT_ID('Hotel') and name = 'LevelID')  
begin  
 ALTER TABLE Hotel  
 ADD LevelID VARCHAR(100)  
END  
if not exists(select * from syscolumns where id = OBJECT_ID('HotelLevel') and name = 'IsStandard')  
begin  
 ALTER TABLE HotelLevel  
 ADD IsStandard int  
END  
  
  
  if object_id('HotelRegion') is null   
begin  
CREATE TABLE dbo.HotelRegion  
(  
HotelID VARCHAR(6) PRIMARY KEY NOT NULL,  
GroupID VARCHAR(6) NOT NULL,  
Name VARCHAR(100) NOT NULL,  
City VARCHAR(100),  
Content VARCHAR(100),  
Mobile VARCHAR(100) NOT NULL,  
[Address] VARCHAR(400),  
--DefalutLoginName VARCHAR(400),  
CreateTime DATETIME  
)  
   
END  
  
     
   
--品牌表  
  if object_id('HotelBrand') is null   
begin  
CREATE TABLE HotelBrand  
(  
ID VARCHAR(100) PRIMARY KEY NOT NULL,  
HotelID VARCHAR(10) NOT NULL,  
Name VARCHAR(50) NOT NULL,  
CreateTime DATETIME  
)  
END  
  
if not exists(select * from syscolumns where id = OBJECT_ID('hotel') and name = 'BrandID')  
begin  
ALTER TABLE dbo.hotel  
ADD BrandID VARCHAR(50)  
END  
                  
if not exists(select * from syscolumns where id = OBJECT_ID('HotelLevel') and name = 'EditionName')  
begin  
ALTER TABLE HotelLevel  
ADD EditionName VARCHAR(50)  
END  
--问吧系统地址  
IF NOT EXISTS (SELECT * FROM dbo.sysPara WHERE code='webDebugUrl')  
BEGIN  
INSERT INTO dbo.sysPara  
        ( code ,  
          name ,  
          refValue ,  
          value ,  
          remark ,  
          seqid   
        )  
VALUES  ( 'webDebugUrl' , -- code - varchar(30)  
          '问吧提交问题地址' , -- name - varchar(300)  
          'http://gemstar.vicp.net:7600/Login.aspx' , -- refValue - varchar(300)  
          'http://gemstar.vicp.net:7600/Login.aspx' , -- value - varchar(3000)  
          '用户点击跳转问吧问题提交系统' , -- remark - varchar(300)  
          50   
        )  
END  
--微信公众号其他参数  
IF NOT EXISTS (SELECT * FROM dbo.sysPara WHERE code='SCMWeixinEncodingAESKey')  
BEGIN  
  
INSERT INTO dbo.sysPara  
        ( code ,  
          name ,  
          refValue ,  
          value ,  
          remark ,  
          seqid   
        )  
VALUES  ( 'SCMWeixinEncodingAESKey' , -- code - varchar(30)  
          'SCM微信第三方URL对应的消息加解密密钥' , -- name - varchar(300)  
          '5U6plGv3j2rEkHUp3JBB90nnWcEGQy4m32VhvqarCa2' , -- refValue - varchar(300)  
          '5U6plGv3j2rEkHUp3JBB90nnWcEGQy4m32VhvqarCa2' , -- value - varchar(3000)  
          '' , -- remark - varchar(300)  
          1006  
        )  
        INSERT INTO dbo.sysPara  
        ( code ,  
          name ,  
          refValue ,  
          value ,  
          remark ,  
          seqid   
        )  
VALUES  ( 'SCMWeixinAppId' , -- code - varchar(30)  
          'SCM微信AppId' , -- name - varchar(300)  
          'wx78fcdd22e8c4c206' , -- refValue - varchar(300)  
          'wx78fcdd22e8c4c206' , -- value - varchar(3000)  
          '' , -- remark - varchar(300)  
          1007  
        )  
        INSERT INTO dbo.sysPara  
        ( code ,  
          name ,  
          refValue ,  
   value ,  
          remark ,  
          seqid  
        )  
VALUES  ( 'SCMWeixinAppSecret' , -- code - varchar(30)  
          'SCM微信AppSecret' , -- name - varchar(300)  
          '17d82a6f4713fd5785b06f60c0925a6c' , -- refValue - varchar(300)  
          '17d82a6f4713fd5785b06f60c0925a6c' , -- value - varchar(3000)  
          '' , -- remark - varchar(300)  
          1008  
        )  
END  
  
  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id = OBJECT_ID('Supplier') AND name = 'PurchaseAmount')  
BEGIN   
   ALTER TABLE Supplier ADD PurchaseAmount DECIMAL(18,4)  
END  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id = OBJECT_ID('dbList') AND name = 'isDefalut')  
BEGIN   
   ALTER TABLE dbo.dbList  
ADD isDefalut BIT NOT NULL DEFAULT(0)  
END  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id = OBJECT_ID('weixinQrcodeLogin') AND name = 'CodeDesc')  
BEGIN   
ALTER TABLE weixinQrcodeLogin  
ADD CodeDesc VARCHAR(100)  
END  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id = OBJECT_ID('hotelProducts') AND name = 'HotelName')  
BEGIN   
ALTER TABLE dbo.hotelProducts  
ADD HotelName VARCHAR(100)  
end  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id = OBJECT_ID('hotel') AND name = 'ScmDiffGrpid')  
BEGIN   
ALTER TABLE dbo.hotel  
ADD ScmDiffGrpid VARCHAR(10)  
end  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id = OBJECT_ID('hotel') AND name = 'PointGrpid')  
BEGIN   
ALTER TABLE dbo.hotel  
ADD PointGrpid VARCHAR(10)  
end  
  if object_id('weixinQrcodeLogin2') is null   
begin  
CREATE TABLE [dbo].[weixinQrcodeLogin2]  
(  
[id] [uniqueidentifier] NOT NULL PRIMARY KEY,  
[keyid] [uniqueidentifier] NOT NULL,  
[createDate] [datetime] NOT NULL,  
[expireDate] [datetime] NOT NULL,  
[status] [int] NOT NULL,  
[loginOpenid] [varchar] (100) COLLATE Chinese_PRC_CI_AS NULL,  
[loginHid] [varchar] (10) COLLATE Chinese_PRC_CI_AS NULL,  
[loginUserid] VARCHAR(100) NULL,  
[loginDate] [datetime] NULL,  
[loginType] [tinyint] NULL,  
[CodeDesc] [varchar] (100) COLLATE Chinese_PRC_CI_AS NULL  
)    
end  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id = OBJECT_ID('ServicesOperator') AND name = 'SCM_LoginOpenid')  
BEGIN   
ALTER TABLE dbo.ServicesOperator  
ADD SCM_LoginOpenid VARCHAR(100)  
end  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id = OBJECT_ID('hotelProducts') AND name = 'ProductGrpid')  
BEGIN   
ALTER TABLE dbo.hotelProducts  
ADD ProductGrpid VARCHAR(10)  
END  
IF NOT EXISTS (SELECT * FROM dbo.AuthButtons WHERE AuthButtonId='SetHotelLevel' AND AuthButtonValue='524288' AND Seqid='101')  
BEGIN  
INSERT INTO dbo.AuthButtons  
        ( AuthButtonId ,  
          AuthId ,  
          AuthButtonName ,  
          AuthButtonValue ,  
          Seqid  
        )  
VALUES  ( 'SetHotelLevel' , -- AuthButtonId - varchar(60)  
          '80' , -- AuthId - varchar(60)  
          '设置酒店模块权限' , -- AuthButtonName - varchar(60)  
          524288 , -- AuthButtonValue - bigint  
          101  -- Seqid - int  
        )  
        INSERT INTO dbo.AuthButtons  
        ( AuthButtonId ,  
          AuthId ,  
          AuthButtonName ,  
          AuthButtonValue ,  
          Seqid  
        )  
VALUES  ( 'SetHotelArea' , -- AuthButtonId - varchar(60)  
          '80' , -- AuthId - varchar(60)  
          '设置酒店区域' , -- AuthButtonName - varchar(60)  
          1048576 , -- AuthButtonValue - bigint  
          102  -- Seqid - int  
        )  
        INSERT INTO dbo.AuthButtons  
        ( AuthButtonId ,  
          AuthId ,  
          AuthButtonName ,  
          AuthButtonValue ,  
          Seqid  
        )  
VALUES  ( 'SetHotelBrand' , -- AuthButtonId - varchar(60)  
          '80' , -- AuthId - varchar(60)  
          '品牌维护' , -- AuthButtonName - varchar(60)  
          2097152 , -- AuthButtonValue - bigint  
          103  -- Seqid - int  
        )  
end  
IF NOT EXISTS (SELECT * FROM dbo.AuthButtons WHERE AuthButtonId='SaveHotelLevel' AND AuthButtonValue='4194304' AND Seqid='102')  
BEGIN  
INSERT INTO dbo.AuthButtons  
        ( AuthButtonId ,  
          AuthId ,  
          AuthButtonName ,  
          AuthButtonValue ,  
          Seqid  
        )  
VALUES  ( 'SaveHotelLevel' , -- AuthButtonId - varchar(60)  
          '80' , -- AuthId - varchar(60)  
          '保存酒店模块权限' , -- AuthButtonName - varchar(60)  
          4194304 , -- AuthButtonValue - bigint  
          102  -- Seqid - int  
        )  
end  
--公司规定这几个参数，不能放运营参数里  
--IF NOT EXISTS (SELECT * FROM dbo.operatingParam WHERE  code='ShowGemStarLogo'  )  
--BEGIN  
--INSERT INTO dbo.operatingParam  
--        ( code, cDate )  
--VALUES  ( 'ShowGemStarLogo', -- code - varchar(60)  
--          GETDATE()  -- cDate - datetime  
--          )  
--          INSERT INTO dbo.operatingParam  
--        ( code, cDate )  
--VALUES  ( 'ShowGemStarInfo', -- code - varchar(60)  
--          GETDATE()  -- cDate - datetime  
--          )  
--INSERT INTO dbo.operatingParam  
--        ( code, cDate )  
--VALUES  ( 'GemStarSystemTitle', -- code - varchar(60)  
--          GETDATE()  -- cDate - datetime  
--          )  
--end  
/** SCM合并脚本 结束 **/  
  
  
--ispa 技师买钟成功消息通知 黄鸿斌 2021年3月5日  
IF(NOT EXISTS(SELECT 1 FROM sysPara WHERE code = 'ISPAWxTemplateIDArtBuyClock'))  
BEGIN  
 INSERT INTO dbo.sysPara  
        ( code ,  
          name ,  
          refValue ,  
          value ,  
          remark ,  
          seqid  
        )  
 VALUES  ( 'ISPAWxTemplateIDArtBuyClock' , -- code - varchar(30)  
          'ispa 技师买钟成功消息通知' , -- name - varchar(300)  
          'LNIiP_YgWLcXlrEBjPIAt9rQdklJhtSGR4hHO7mBVQU' , -- refValue - varchar(300)  
          'LNIiP_YgWLcXlrEBjPIAt9rQdklJhtSGR4hHO7mBVQU' , -- value - varchar(3000)  
          'ispa 技师买钟成功消息通知' , -- remark - varchar(300)  
          650  -- seqid - int  
        );  
END   
   
if object_id('QuickVoiceUser') is null   
BEGIN  
 CREATE TABLE QuickVoiceUser  
 (  
  ID uniqueidentifier NOT NULL,  
  UnionID varchar (50) COLLATE Chinese_PRC_CI_AS NULL,  
  HeartBeatUpdateTime datetime NULL  
 )  
 ALTER TABLE QuickVoiceUser ADD CONSTRAINT PK_QuickVoiceUser PRIMARY KEY CLUSTERED  (ID)   
 CREATE NONCLUSTERED INDEX IX_Table_UnionID ON  QuickVoiceUser (UnionID)  
END  
--给酒店的pos设备增加是否数量控制，数量属性，用于后台只控制数量，具体设备序列号由现场工程师通过程序接口直接增加，陈前良，2021-05-11  
if not exists(select * from INFORMATION_SCHEMA.COLUMNS where TABLE_NAME = 'hotelpos' and COLUMN_NAME = 'isQtyControl')  
begin  
 alter table hotelpos add isQtyControl bit not null default 0,qty int null  
end  
--扫码点餐酒店管理增加vip版本的字段  蒋创世   2021-05-21  
IF NOT EXISTS(SELECT 1 FROM syscolumns WHERE id = OBJECT_ID('posSmMappingHid') AND name = 'versionid')  
BEGIN   
   ALTER TABLE posSmMappingHid ADD versionid varchar(50)  
END  
--扫码点餐增加线下接口程序更新时间字段。  蒋创世   2021-05-24  
IF NOT EXISTS(SELECT 1 FROM syscolumns WHERE id = OBJECT_ID('posSmMappingHid') AND name = 'apiLastUpdateTime')  
BEGIN   
   ALTER TABLE posSmMappingHid ADD apiLastUpdateTime datetime null  
END  
-- 增加萝趣酒店表，关联捷云酒店 fl 2021-06-04 jobid=169636  
IF OBJECT_ID('lqHotelInfo') IS NULL  
BEGIN  
 CREATE TABLE [dbo].[lqHotelInfo](  
  Id UNIQUEIDENTIFIER  PRIMARY KEY  NOT NULL,  
  [lqHid] [varchar](60) NOT NULL,--萝趣酒店ID  
  HotelName VARCHAR(100) NULL,--名称  
  Address [VARCHAR](800) NULL,--地址  
  Remark VARCHAR(800) NULL,--备注  
  Cdate DATETIME NULL,--创建时间  
  hid VARCHAR(6) NULL,--关联捷云  
  status TINYINT NULL,--状态  
  city varchar(60) NULL,--城市  
  province varchar(60) NULL,--省份  
 )  
END  
  
  
  
--ispa 换做钟类型推送消息模板 黄鸿斌 2021年6月8日  
IF(NOT EXISTS(SELECT 1 FROM sysPara WHERE code = 'ISPAWeiXinTemplateIDArtCMtype'))  
BEGIN  
 INSERT INTO dbo.sysPara  
        ( code ,  
          name ,  
          refValue ,  
          value ,  
          remark ,  
          seqid  
        )  
 VALUES  ( 'ISPAWeiXinTemplateIDArtCMtype' , -- code - varchar(30)  
          'ispa 换做钟类型通知模版ID' , -- name - varchar(300)  
          '_6iDu0CSCRmFSZKoni_OXiobcV9FnV73wFrmL9rkOMA' , -- refValue - varchar(300)  
          '_6iDu0CSCRmFSZKoni_OXiobcV9FnV73wFrmL9rkOMA' , -- value - varchar(3000)  
          'ispa 换做钟类型通知模版ID' , -- remark - varchar(300)  
          667  -- seqid - int  
        );   
END   
  
  
  
--ispa 技师超时未确认上钟自动倒牌消息通知模板 黄鸿斌 2021年6月22日  
IF(NOT EXISTS(SELECT 1 FROM sysPara WHERE code = 'ISPAWXTemplateIDArtReversal'))  
BEGIN  
 INSERT INTO dbo.sysPara  
        ( code ,  
          name ,  
          refValue ,  
          value ,  
          remark ,  
          seqid  
        )  
 VALUES  ( 'ISPAWXTemplateIDArtReversal' , -- code - varchar(30)  
          'ispa 技师超时未确认上钟自动倒牌消息通知模板' , -- name - varchar(300)  
          '_6iDu0CSCRmFSZKoni_OXiobcV9FnV73wFrmL9rkOMA' , -- refValue - varchar(300)  
          '_6iDu0CSCRmFSZKoni_OXiobcV9FnV73wFrmL9rkOMA' , -- value - varchar(3000)  
          'ispa 技师超时未确认上钟自动倒牌消息通知模板ID' , -- remark - varchar(300)  
          668  -- seqid - int  
        );   
END   
  
  
--ispa 技师超时未确认上钟通知管理员模板 黄鸿斌 2021年6月22日  
IF(NOT EXISTS(SELECT 1 FROM sysPara WHERE code = 'ISPAWXTemplateIDArtIsOkToAdmin'))  
BEGIN  
 INSERT INTO dbo.sysPara  
        ( code ,  
          name ,  
          refValue ,  
          value ,  
          remark ,  
          seqid  
        )  
 VALUES  ( 'ISPAWXTemplateIDArtIsOkToAdmin' , -- code - varchar(30)  
          'ispa 技师超时未确认上钟通知管理员模板' , -- name - varchar(300)  
          '_6iDu0CSCRmFSZKoni_OXiobcV9FnV73wFrmL9rkOMA' , -- refValue - varchar(300)  
          '_6iDu0CSCRmFSZKoni_OXiobcV9FnV73wFrmL9rkOMA' , -- value - varchar(3000)  
          'ispa 技师超时未确认上钟通知管理员模板消息模板ID' , -- remark - varchar(300)  
          669  -- seqid - int  
        );   
END   
  
-- 售后工程师新增绑定Openid xn 2021-07-14  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id = OBJECT_ID('ServicesOperator') AND name = 'Hy_LoginOpenid')  
BEGIN   
ALTER TABLE dbo.ServicesOperator ADD Hy_LoginOpenid VARCHAR(100)  
end  
  
--通知表增加数据库ID lizw 2021-07-12  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('Notice') AND name = 'dbIds')  
BEGIN  
 ALTER TABLE Notice ADD dbIds varchar(max) null  
END  
  
--通知表增加酒店ID lizw 2021-07-12  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('Notice') AND name = 'hotelIds')  
BEGIN  
 ALTER TABLE Notice ADD hotelIds varchar(max) null  
END  
--增加系统参数：云Pos外部接口IP白名单 liangjy 2021-08-04  
if not exists (SELECT 1 FROM sysPara WHERE code = 'PosInterfaceIPWhiteList')  
begin  
 insert into sysPara(code,name,refValue,value,remark,seqid,IsHide)  
 values('PosInterfaceIPWhiteList','云Pos外部接口IP白名单','','','在此白名单中的IP地址才能调用云Pos外部接口，多个IP地址以英文逗号分隔。',9999,1)  
end  
  
--增加合作伙伴代码 lizr 2021-08-09 172536  
if not exists(select * from syscolumns where id = OBJECT_ID('hotel') and name = 'partnerCode')  
begin  
 ALTER TABLE dbo.hotel ADD partnerCode VARCHAR(30)  
END  
--增加合作伙伴设置表  
IF OBJECT_ID('partnerSet') IS NULL  
BEGIN  
 CREATE TABLE [dbo].[partnerSet](  
  [id] [uniqueidentifier] NOT NULL,  
  [code] [varchar](30) NOT NULL, -- 代码  
  [name] [varchar](100) NOT NULL, -- 名称  
  [picLink] [varchar](500) NULL, -- logo  
  [servicePhone] [varchar](30) NULL, -- 服务热线  
  [companyAddress] [varchar](500) NULL, -- 公司地址  
  [companyFax] [varchar](30) NULL, -- 公司传真  
  [companyName] [varchar](100) NULL, -- 公司名称  
  [companyTelephone] [varchar](30) NULL, -- 公司电话  
  [companyVersionNumber] [varchar](60) NULL, -- 公司版权号  
  [companyDomain] [varchar](500) NULL, -- 公司域名  
  [routeCode] [varchar](30) NULL, -- 路由代码  
  [prodShowName] [varchar](100) NULL, -- 产品展示名称（如“捷信达GSHIS”）  
  [prodShowShortName] [varchar](100) NULL, -- 产品展示简称（如“GS/HIS”）  
  [seqid] [int] NULL, -- 排序  
  [domain] [varchar](500) NULL,  
  [bucket] [varchar](50) NULL,  
  [access_key] [varchar](200) NULL,  
  [secret_key] [varchar](200) NULL,  
  CONSTRAINT [PK_PARTNERSET] PRIMARY KEY CLUSTERED   
 (  
  [id] ASC  
 )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]  
 ) ON [PRIMARY]  
end  
  
--iPos增加 扫码点餐功能模块  20211018 jiangcs   
if not exists(select 1 from  syscolumns where id=OBJECT_ID('posSmMappingHid') AND  name = 'ScanOrderProductCode')  
BEGIN   
 alter  table  posSmMappingHid add  ScanOrderProductCode varchar(500) NULL   --功能模块  
END  
  
--增加发票开票信息表，用于业务库输入企业名称查询，jlb，2021-10-19 bebugId:172285  
IF NOT EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[commonInvoiceInfo]') AND type IN ('U'))  
BEGIN  CREATE TABLE [dbo].[commonInvoiceInfo] (  
  [id] [uniqueidentifier] NOT NULL,  
  [taxNo] varchar(60) COLLATE Chinese_PRC_CI_AS  not null,  
  [taxName] varchar(300) COLLATE Chinese_PRC_CI_AS not NULL,  
  [taxAdd] varchar(300) COLLATE Chinese_PRC_CI_AS  NULL,  
  [taxTel] varchar(300) COLLATE Chinese_PRC_CI_AS  NULL,  
  [taxBank] varchar(300) COLLATE Chinese_PRC_CI_AS  NULL,  
  [taxAccount] varchar(300) COLLATE Chinese_PRC_CI_AS  NULL,  
  constraint PK_CommonInvoiceInfo primary key (id)  
)  
END  
--add slowlog table,chenql,2021-10-26 14:50  
if not exists(select * from INFORMATION_SCHEMA.TABLEs where table_name = 'slowlog')  
begin  
 create table slowlog(  
  logId int IDENTITY,  
  logTime datetime,  
  url text,  
  totalMilliseconds int,  
  logMessage text,  
  queryString text,  
  form text,  
  constraint pk_slowlog PRIMARY key(logId)  
 )  
end  
  
--广告增加字段“省份、城市、星级特性、酒店编号” jlb 2021-11-01  
if(not exists(select 1 from syscolumns where id=object_id('adSet') and [name]='provinces'))  
begin  
 alter table adSet add provinces varchar(1000) null  
end  
if(not exists(select 1 from syscolumns where id=object_id('adSet') and [name]='cities'))  
begin  
 alter table adSet add cities varchar(1000) null  
end  
if(not exists(select 1 from syscolumns where id=object_id('adSet') and [name]='stars'))  
begin  
 alter table adSet add stars varchar(1000) null  
end  
if(not exists(select 1 from syscolumns where id=object_id('adSet') and [name]='hids'))  
begin  
 alter table adSet add hids varchar(1000) null  
end  
  
--增加夜审扩展表  xuhj  2022-01-20  jobid：181358  
if not exists(select id from sysobjects where name = 'auditExtend')  
begin  
 CREATE TABLE auditExtend(  
  [id] bigint IDENTITY(1,1) primary key,--自增主键  
  [hid] varchar(30) NOT NULL,--酒店id  
  [dbid] uniqueidentifier not null,--业务库id  
  [auditType] [varchar](60) NULL,--夜审类型，默认会员  
  [status] int null,--执行状态  
  [cDate] datetime NULL --创建时间  
 )  
end  
  
--增加GS微信接口 李志伟 2022-02-08  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'GsWxComid')  
BEGIN  
 INSERT INTO sysPara(code, name, refValue, value, remark, seqid)VALUES('GsWxComid', 'GS微信接口-酒店ID', '', '', '酒店ID', 18)  
 INSERT INTO sysPara(code, name, refValue, value, remark, seqid)VALUES('GsWxCreatePayOrderUrl', 'GS微信接口-支付下单地址', '', '', '支付下单地址', 18)  
 INSERT INTO sysPara(code, name, refValue, value, remark, seqid)VALUES('GsWxOpenidUrl', 'GS微信接口-Openid地址', '', '', 'Openid地址', 18)  
 INSERT INTO sysPara(code, name, refValue, value, remark, seqid)VALUES('GsWxPayOrderUrl', 'GS微信接口-支付地址', '', '', '支付地址', 18)  
 INSERT INTO sysPara(code, name, refValue, value, remark, seqid)VALUES('GsWxTemplateMessageUrl', 'GS微信接口-模板消息地址', '', '', '模板消息地址', 18)  
 INSERT INTO sysPara(code, name, refValue, value, remark, seqid)VALUES('isGsWxInterface', 'GS微信接口-是否启用', '', '', '是否启用：0不启用，1启用', 18)  
END  
  
  
--增加微信模板ID 李志伟 2022-02-08  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'wxtidbyPriceAuthorize')  
BEGIN  
 INSERT INTO sysPara(code, name, refValue, value, remark, seqid)VALUES('wxtidbyPriceAuthorize', '微信模板消息-授权提醒', '', '', '授权提醒', 19)  
 INSERT INTO sysPara(code, name, refValue, value, remark, seqid)VALUES('wxtidbyBsnsReport', '微信模板消息-报表提醒', '', '', '报表提醒', 19)  
 INSERT INTO sysPara(code, name, refValue, value, remark, seqid)VALUES('wxtidbyInsRoom', '微信模板消息-查房提醒', '', '', '查房提醒', 19)  
 INSERT INTO sysPara(code, name, refValue, value, remark, seqid)VALUES('wxtidbyInsRoomChange', '微信模板消息-换房查房提醒', '', '', '换房查房提醒', 19)  
 INSERT INTO sysPara(code, name, refValue, value, remark, seqid)VALUES('wxtidbyInsRoomCheckout', '微信模板消息-退房查房提醒', '', '', '退房查房提醒', 19)  
END  
  
--人脸设备表增加楼栋信息字段，用于控制使用的楼栋  181258 xuhj 2022-02-08  
if not exists(select 1 from syscolumns where id = OBJECT_ID('FaceDevices') and name = 'buildName')  
begin  
 ALTER TABLE [dbo].[FaceDevices] ADD [buildName] varchar(50) NULL  
end  
  
--新美大验券接口列表增加字段客户Id，注：bid不是open_shop_uuid  fl 2022-02-17  
if not exists(select 1 from syscolumns where id = OBJECT_ID('AppShopList') and name = 'Bid')  
begin  
 ALTER TABLE [dbo].AppShopList ADD Bid nvarchar(200) NULL  
end  
--门店id的唯一标识  fl 2022-02-18  
if not exists(select 1 from syscolumns where id = OBJECT_ID('AppShopList') and name = 'ShopUid')  
begin  
 ALTER TABLE [dbo].AppShopList ADD ShopUid nvarchar(200) NULL  
end  
--增加Hy GS微信接口 肖念 2022-03-01 用来区别云端一个公众号  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'HyGsWxComid')  
BEGIN  
 INSERT INTO sysPara(code, name, refValue, value, remark, seqid)VALUES('HyGsWxComid', '汇颐GS微信接口-酒店ID', '', '', '机构ID', 18)  
 INSERT INTO sysPara(code, name, refValue, value, remark, seqid)VALUES('HyisGsWxInterface', '汇颐GS微信接口-是否启用', '', '', '是否启用：0不启用，1启用', 18)  
END  
--增加平台参数 扫码点餐模板默认参数  jiangcs 2022-03-04  
IF NOT EXISTS(SELECT 1 FROM sysPara WHERE code = 'POSSMGsWxOpenidUrl')  
BEGIN  
 INSERT INTO sysPara(code, name, refValue, value, remark, seqid)VALUES('POSSMGsWxOpenidUrl', '扫码点餐Openid地址', '', '', 'GS微信的获取openid的地址', 18)  
 INSERT INTO sysPara(code, name, refValue, value, remark, seqid)VALUES('POSSMGsWxTemplateMsgUrl', '扫码点餐模板消息地址', '', '', 'GS微信的发送消息模板的地址', 18)  
 INSERT INTO sysPara(code, name, refValue, value, remark, seqid)VALUES('POSSMGsWxCreatePayOrderUrl', '扫码点餐支付下单地址', '', '', 'GS微信的支付下单的地址', 18)  
 INSERT INTO sysPara(code, name, refValue, value, remark, seqid)VALUES('POSSMGsWxPayOrderUrl', '扫码点餐支付地址', '', '', 'GS微信的支付地址', 18)  
END  
  
--增加微信模板ID 李志伟 2022-03-22  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'wxtidbybanquetcancel')  
BEGIN  
 INSERT INTO sysPara(code, name, refValue, value, remark, seqid)VALUES('wxtidbybanquetcancel', '微信模板消息-宴会取消提醒', '', '', '宴会未确认记录自动取消后微信通知操作员', 19)  
END  
  
  
  
if not exists(select * from syspara where code='minio_domain')  
begin  
 insert into syspara (code , name,value,refValue,remark,seqid )  
  values ('minio_domain' ,'minio图片上传外链域名', '' ,'','从minio控制台获取，不带http://' ,5)   
end   
    
if not exists(select * from syspara where code='minio_bucket')  
begin  
 insert into syspara (code , name,value,refValue,remark,seqid )  
  values ('minio_bucket' ,'minio图片上传空间名称', '' ,'','从minio控制台Buckets获取' ,6)   
end  
    
if not exists(select * from syspara where code='minio_access_key')  
begin  
 insert into syspara (code , name,value,refValue,remark,seqid )  
  values ('minio_access_key' ,'minio图片上传公钥', '' ,'','从minio控制台Identity-Users获取' ,7)   
end  
  
if not exists(select * from syspara where code='minio_secret_key')  
begin  
 insert into syspara (code , name,value,refValue,remark,seqid )  
  values ('minio_secret_key' ,'minio图片上传秘钥', '' ,'','从minio控制台Identity-Users获取' ,8)   
end  
--增加抖音接口管理表，2022-04-12 fl job：182736  
IF OBJECT_ID('TkShopList') IS NULL  
BEGIN   
CREATE TABLE [dbo].[TkShopList](  
 [AppShopID] [nvarchar](64)  PRIMARY key NOT NULL,  --商户ID  
 [HotelName] [nvarchar](50) NULL,   --商户名称  
 [HotelCode] [nvarchar](50) NULL,   --捷云酒店代码  
 [poi_id]  [varchar](300) NULL, -- 门店id  
 [poi_name]  [varchar](300) NULL, -- 门店名称  
 [CreateDate] [datetime] NULL,   --创建时间  
 [ModifyDate] [datetime] NULL   --修改时间  
)   
END  
--用于第三方接口保存token和失效时间的。fl  
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'InterfaceToken')  
BEGIN  
  
    CREATE TABLE InterfaceToken   
(  
 hid varchar(60) NULL ,  
 id uniqueidentifier NOT NULL  PRIMARY KEY,  
 typecode VARCHAR(60), --接口类型  
 assessToken varchar(500),   --获取的token值  
 cdate datetime NOT NULL ,  
 expirestime  int null,--有效时长  
 endtime datetime NOT NULL --失效时间  
)  
END  
  
if not exists(select * from syspara where code='client_key')  
begin  
 insert into syspara (code , name,value,refValue,remark,seqid )  
  values ('client_key' ,'抖音验票接口应用ID', '' ,'awezmnjixegrtv2c','抖音验票接口的服务商应用ID' ,10)   
end  
if not exists(select * from syspara where code='client_secret')  
begin  
 insert into syspara (code , name,value,refValue,remark,seqid )  
  values ('client_secret' ,'抖音验票接口应用秘钥', '' ,'4565cbe8f67c34622f85c7d97cf7abe8','抖音验票接口的服务商应用秘钥' ,11)   
end  
  
--酒店表增加业务员和酒店联系人  jiangcs 增加字段 Belonghotel 同业务库中的pmsuser.Belonghotel  
if not exists(select 1 from syscolumns where id=OBJECT_ID('WeixinOperatorHotelMapping') and name = 'Belonghotel')  
begin  
 alter table WeixinOperatorHotelMapping add Belonghotel varchar(500) null   
end  
--睡眠垫在线离线表 xn  2022-04-24  
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name ='SleepEquipmentStatus')  
 BEGIN  
create table SleepEquipmentStatus (  
   hid                  char(6)              not null,  
   id                   uniqueidentifier   primary key  not null,  
   cdate                datetime             null,--录入时间     
   sn                   varchar(40)          null,--设备号  
   ip                    varchar(40)          null,--ip  
   port                 varchar(40)          null,--端口  
   AcceptTime           datetime             null,  
   CloseTime            datetime             null,  
   AcceptCount          int                  null,  
   CloseCount           int                  null  
)  
end  
  
--绑定微信时, 需要知道来源, 才能区分是不是云温泉或者 其他系统, 才能找到登录用户   panj 2022-05-19  
if not exists(select 1 from syscolumns where id=OBJECT_ID('WeixinOperatorHotelMapping') and name = 'Module')  
begin  
 alter table WeixinOperatorHotelMapping add Module varchar(100) null   
end  
  
--绑定微信时, 需要知道来源, 才能区分是不是云温泉或者 其他系统, 才能找到登录用户   panj 2022-05-19  
if not exists(select 1 from syscolumns where id=OBJECT_ID('weixinQrcodeLogin') and name = 'Module')  
begin  
 alter table weixinQrcodeLogin add Module varchar(100) null   
END  
  
IF OBJECT_ID('RefeSbPayConfigure') IS NULL  
BEGIN   
CREATE TABLE [dbo].[RefeSbPayConfigure](  
    [Id] [UNIQUEIDENTIFIER] NOT NULL  primary key,  
 [Hid] [CHAR](6) NOT NULL,  
 [Code] [nvarchar](50) NOT NULL,   --代码  
 [Name] [nvarchar](50) NOT NULL,   --名称  
 SbaccessToken  [nvarchar](256)  NOT NULL,    
 SbmerchantNo  [nvarchar](256)  NOT NULL,    
 SbsubAppid  [nvarchar](256)  NULL,    
 SbterminalId  [nvarchar](256)  NOT NULL,    
 [CreateDate] [DATETIME] NOT NULL,   --创建时间  
 [ModifyDate] [DATETIME] NOT NULL   --修改时间  
)   
END  
   
  
 --后台运营系统 产品版本管理 添加 云Pos餐饮类型，panj 2022-08-24  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('ProductVersionList') AND name = 'CateringServicesType')  
BEGIN  
 ALTER TABLE ProductVersionList ADD CateringServicesType varchar(100) null  
END  
  
 --后台运营系统 产品版本管理 添加 云Pos餐饮模块，panj 2022-08-24  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('ProductVersionList') AND name = 'CateringServicesModule')  
BEGIN  
 ALTER TABLE ProductVersionList ADD CateringServicesModule varchar(100) null  
END  
  
 --后台运营系统 产品版本管理 添加 云Pos收银点数量，panj 2022-08-24  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('ProductVersionList') AND name = 'PosSettlePointCount')  
BEGIN  
 ALTER TABLE ProductVersionList ADD PosSettlePointCount tinyint null  
END  
--增加云音响mqtt连接参数 2022-08-26 fl  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'ZkcAddr')  
BEGIN  
  INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'ZkcAddr' ,'云音响服务器地址' ,'120.78.150.202' ,'120.78.150.202' ,'MQTT服务器地址' ,500)  
  INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'ZkcPort' ,'云音响服务器端口号' ,'1883' ,'1883' ,'MQTT服务器端口号' ,500)  
  INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'ZkcUserName' ,'云音响服务器账号' ,'jxd' ,'jxd' ,'MQTT服务器账号' ,500)  
  INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'ZkcPassword' ,'云音响服务器密码' ,'jxdMQ598' ,'jxdMQ598' ,'MQTT服务器密码' ,500)  
   INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)  
 VALUES  ( 'ZkcClientId' ,'云音响客户端id' ,'MQTT_FX_Client' ,'MQTT_FX_Client' ,'云音响客户端id' ,500)  
 END  
   
   
 --增加短地址转换表  
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name ='Converts')  
BEGIN  
 CREATE TABLE [Converts](  
  [Id] [char](7) NOT NULL primary key,  
  [Value] [varchar](8000) NOT NULL,  
  [CDate] [datetime] NOT NULL default getdate(),  
  [EndDate] [datetime] NOT NULL,  
  [CertType] [varchar](100) NULL,  
 )  
END  
  
if not exists(select * from syspara where code='SignalR_url')  
begin  
 insert into syspara (code , name,value,refValue,remark,seqid )  
  values ('SignalR_url' ,'实时消息网址', '' ,'','SignalR网址' ,51)   
end   
    
if not exists(select * from syspara where code='SignalR_appId')  
begin  
 insert into syspara (code , name,value,refValue,remark,seqid )  
  values ('SignalR_appId' ,'实时消息账号', '' ,'','SignalR应用ID' ,52)   
end  
    
if not exists(select * from syspara where code='SignalR_secret')  
begin  
 insert into syspara (code , name,value,refValue,remark,seqid )  
  values ('SignalR_secret' ,'实时消息秘钥', '' ,'','SignalR秘钥' ,53)   
end  
  
if not exists(select * from syspara where code='SignalR_serviceurl')  
begin  
 insert into syspara (code , name,value,refValue,remark,seqid )  
  values ('SignalR_serviceurl' ,'实时消息服务网址', '' ,'','与SignalR服务保持连接' ,54)   
end  
if not exists(select * from syspara where code='HiYieldApiKey')  
begin  
 insert into syspara (code , name,value,refValue,remark,seqid )  
  values ('HiYieldApiKey' ,'鸿鹊收益系统apidkey', 'HiYield' ,'HiYield','鸿鹊收益系统apidkey' ,55)   
end  
if not exists(select * from syspara where code='HiYieldApiSecret')  
begin  
 insert into syspara (code , name,value,refValue,remark,seqid )  
  values ('HiYieldApiSecret' ,'鸿鹊收益系统ApiSecret', '728e60d3475b44daad5b67456f555dcb' ,'728e60d3475b44daad5b67456f555dcb','鸿鹊收益系统ApiSecret' ,56)   
end  
  
 --后台运营系统 产品版本管理 添加 消费处理方式，zjh 2022-10-20  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('ProductVersionList') AND name = 'ConsumeAction')  
BEGIN  
 ALTER TABLE ProductVersionList ADD ConsumeAction varchar(2000) null  
END  
  
 --后台运营系统 产品版本管理 添加 付款处理方式，zjh 2022-10-20  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('ProductVersionList') AND name = 'ItemAction')  
BEGIN  
 ALTER TABLE ProductVersionList ADD ItemAction varchar(2000) null  
END  
  
 --后台运营系统 产品版本管理 添加 身份证读卡器类型，zjh 2022-10-20  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('ProductVersionList') AND name = 'IdCardType')  
BEGIN  
 ALTER TABLE ProductVersionList ADD IdCardType varchar(2000) null  
END  
  
  
 --后台运营系统 产品版本管理 添加 身份证读卡器类型，zjh 2022-10-20  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('ProductVersionList') AND name = 'IdCardType')  
BEGIN  
 ALTER TABLE ProductVersionList ADD IdCardType varchar(2000) null  
END  
  
-- 后台运营系统 添加扫呗支付方式 从系统参数添加到运营参数 panj 2022-10-25  
IF NOT EXISTS(select code from OperatingParam where code =  'WQSbmerchantNo')  
begin  
 insert into OperatingParam(code, cdate)  
 select 'WQSbmerchantNo', GETDATE()  
end  
IF NOT EXISTS(select code from OperatingParam where code =  'WQSbterminalId')  
begin  
 insert into OperatingParam(code, cdate)  
 select 'WQSbterminalId', GETDATE()  
end  
IF NOT EXISTS(select code from OperatingParam where code =  'WQSbaccessToken')  
begin  
 insert into OperatingParam(code, cdate)  
 select 'WQSbaccessToken', GETDATE()  
end  
  
--合作伙伴添加归属公司  
 if not exists(select 1 from syscolumns where id = OBJECT_ID('PartnerSet') and name = 'AttributionCompany')  
 begin  
  alter table PartnerSet  add  AttributionCompany varchar(100)  null  
 end  
 --广告增加合作伙伴代码 fl 2022-11-25 196612  
 if(not exists(select 1 from syscolumns where id=object_id('adSet') and [name]='partnerCodes'))  
begin  
 alter table adSet add partnerCodes varchar(1000) null  
END  
 if not exists(select 1 from syscolumns where id = OBJECT_ID('TkShopList') and name = 'status')  
 begin  
  alter table TkShopList  add  [status] tinyint not null DEFAULT 1  
  alter table AliShopList  add  [status] tinyint not null DEFAULT 1  
  alter table AppShopList  add  [status] tinyint not null DEFAULT 1  
 end  
   
 --公共开票信息增加字段购方邮箱、手机号 jlb 2023-03-01  
if(not exists(select 1 from syscolumns where id=object_id('commonInvoiceInfo') and ([name]='buyerEmail' OR [name]='buyerPhone')))  
begin  
 alter table commonInvoiceInfo add buyerEmail varchar(100) null  
 alter table commonInvoiceInfo add buyerPhone varchar(20) null  
end  
  
--异常日志表增加产品字段，用来区分不同产品日志 lizw 2023-04-26  
if(not exists(select 1 from syscolumns where id=object_id('sysLog') and [name]='product'))  
begin  
 alter table sysLog add product varchar(60) null  
end  
--产品表增加服务器ID fl 2023-05-23 202884  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('hotelProducts') AND name = 'serverid')  
BEGIN  
  ALTER TABLE hotelProducts ADD serverid UNIQUEIDENTIFIER NULL  
END   
  
--增加凌云版的平台参数 lizw 2023年6月3日 jobid：203107  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'smartTitle')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)VALUES  ( 'smartTitle' ,'凌云版标题' ,'凌云smart酒店管理软件 V1.0' ,'凌云smart酒店管理软件 V1.0' ,'凌云版标题' , 70)  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)VALUES  ( 'smartShortTitle' ,'凌云版标题简称' ,'凌云PMS' ,'凌云PMS' ,'凌云版标题简称' ,71)  
END  
  
--增加长租公寓和电子合同系统通信时的密钥参数，陈前良,2023-05-19  
if not exists(select * from sysPara where code = 'contractClientId')  
begin  
 insert into sysPara(code,name,refValue,value,remark,seqid)  
 values('contractClientId','电子合同接口客户端id','','apt','必须是电子合同授权的客户端id',500)  
   
 insert into sysPara(code,name,refValue,value,remark,seqid)  
 values('contractClientKey','电子合同接口客户端密钥','','aptSecret20230428','必须是电子合同授权的客户端密钥',500)  
end  
--增加电子合同接口地址，陈前良，2023-05-19  
if not exists(select * from sysPara where code = 'contractApiBaseUrl')  
begin  
 insert into sysPara(code,name,refValue,value,remark,seqid)  
 values('contractApiBaseUrl','电子合同接口地址','http://contract.gshis.com:8318','http://contract.gshis.com:8318','电子合同接口基地址，在此地址后面加上各业务相对地址构成完整请求地址',500)  
end  
--增加电子合同通知地址，陈前良，2023-05-19  
if not exists(select * from sysPara where code = 'contractNotifyUrl')  
begin  
 insert into sysPara(code,name,refValue,value,remark,seqid)  
 values('contractNotifyUrl','电子合同通知地址','http://pmsnotify.gshis.com/Home/ContractLockAndEndNotity','http://pmsnotify.gshis.com/Home/ContractLockAndEndNotity','电子合同通知地址，用于电子合同签署完成后通知业务系统合同状态',500)  
end  
  
--增加版权信息，李志伟，2023-08-30  
if not exists(select * from sysPara where code = 'CopyRight')  
begin  
 insert into sysPara(code,name,refValue,value,remark,seqid)  
 values('CopyRight','版权信息','Copyright &copy; 2016-2023 深圳市捷信达电子有限公司','','页面底部版权信息',1000)  
end  
  
if(not exists(select 1 from syscolumns where id=object_id('auditExtend') and [name]='beginDate'))  
begin  
 alter table auditExtend add beginDate datetime NULL  
 alter table auditExtend add endDate datetime NULL  
end  
  
--增加快手接口管理表，2023-10-27 fl  
IF OBJECT_ID('KsShopList') IS NULL  
BEGIN   
CREATE TABLE [dbo].[KsShopList](  
 [AppShopID] [nvarchar](64)  PRIMARY key NOT NULL,  --商户ID  
 [HotelName] [nvarchar](50) NULL,   --商户名称  
 [HotelCode] [nvarchar](50) NULL,   --捷云酒店代码  
 [poi_id]  [varchar](300) NULL, -- 门店id  
 [poi_name]  [varchar](300) NULL, -- 门店名称  
 [status] tinyint not null DEFAULT 1,  
 [CreateDate] [datetime] NULL,   --创建时间  
 [ModifyDate] [datetime] NULL,   --修改时间   
)   
END  
--增加新口碑接口管理表，2023-10-27 fl  
IF OBJECT_ID('NewKbShopList') IS NULL  
BEGIN   
CREATE TABLE [dbo].[NewKbShopList](  
 [AppShopID] [nvarchar](64)  PRIMARY key NOT NULL,  --商户ID  
 [HotelName] [nvarchar](50) NULL,   --商户名称  
 [HotelCode] [nvarchar](50) NULL,   --捷云酒店代码  
 [Shop_id]  [varchar](300) NULL, -- 门店id  
 [Shop_name]  [varchar](300) NULL, -- 门店名称  
 [status] tinyint not null DEFAULT 1,  
 [CreateDate] [datetime] NULL,   --创建时间  
 [ModifyDate] [datetime] NULL,   --修改时间   
)   
END  
if(not exists(select 1 from syscolumns where id=object_id('KsShopList') and [name]='MainSession'))  
begin  
 alter table KsShopList add [MainSession] [nvarchar](800) NULL  
 alter table KsShopList add [RefreshSession] [nvarchar](800) NULL  
 alter table KsShopList add [SessionTime] [datetime] NULL  
 alter table KsShopList add [Expired] [bigint] NULL  
end  
--增加短信内容结尾，李志伟，2023-05-19 211845  
if not exists(select * from sysPara where code = 'smssendendword')  
begin  
 INSERT INTO [dbo].[sysPara]([code],[name],[refValue],[value],[remark],[seqid],[IsHide])  
     VALUES('smssendendword','短信内容结尾','','','结尾关键字,例如:回N退订;拒收请回复R','12','0')  
end  
  
--增加app更新版本信息，2024-04-07 xn  
IF OBJECT_ID('hyappversion') IS NULL  
BEGIN    
create table hyappversion (  
   id                   uniqueidentifier   PRIMARY key  not null,  
   cdate                datetime             null,--更新时间  
   cUser                varchar(40)          null,--更新人  
   vname                varchar(400)          null,--版本信息  
   versionNo            varchar(40)          null --版本号    
)   
END  
-- 添加汇颐七牛地址配置 xn 2024-10-10  
IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'hyqiniudomain')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)VALUES  ( 'hyqiniudomain' ,'汇颐七牛域名' ,'https://img.hyshdp.com/' ,'https://img.hyshdp.com/' ,'汇颐七牛域名' , 1100)  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)VALUES  ( 'hyqiniubucket' ,'汇颐七牛bucket' ,'hy-pmshelpfiles' ,'hy-pmshelpfiles' ,'汇颐七牛域名' , 1101)  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)VALUES  ( 'hyqiniuaccess_key' ,'汇颐七牛access_key' ,'bNprZw5cDzgUTmXgFKDkTSY0C8F6V8FZDYiqD1UR' ,'bNprZw5cDzgUTmXgFKDkTSY0C8F6V8FZDYiqD1UR' ,'汇颐七牛access_key' , 1102)   
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)VALUES  ( 'hyqiniusecret_key' ,'汇颐七牛secret_key' ,'dBbWV3OSJ2kRdh4kdJXYHdxKPUPQEU2WNKVMu34U' ,'dBbWV3OSJ2kRdh4kdJXYHdxKPUPQEU2WNKVMu34U' ,'汇颐七牛secret_key' , 1103)   
  
END  
--增加诺诺发票的服务商信息参数，避免每个用户都要设置相同的内容，陈前良，2024-08-13  
IF NOT EXISTS(SELECT * FROM dbo.sysPara WHERE code = 'NuoNuoAppId')  
BEGIN  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)VALUES  ( 'NuoNuoApiUrl' ,'诺诺接口地址' ,'https://sdk.nuonuo.com/open/v1/services' ,'https://sdk.nuonuo.com/open/v1/services' ,'诺诺接口地址' , 17)  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)VALUES  ( 'NuoNuoAppId' ,'诺诺服务商AppId' ,'86726781' ,'86726781' ,'诺诺服务商AppId' , 17)  
 INSERT INTO sysPara( code ,name ,refValue ,value ,remark ,seqid)VALUES  ( 'NuoNuoAppSecret' ,'诺诺服务商AppSecret' ,'59917EECB6C642C3' ,'59917EECB6C642C3' ,'诺诺服务商AppSecret' , 17)  
END  
  
--增加请求接口日志是否开启，默认不开启，xn 2024-10-10  
if not exists(select * from sysPara where code = 'IsEnableInterfaceLogs')  
begin  
 INSERT INTO [dbo].[sysPara]([code],[name],[refValue],[value],[remark],[seqid],[IsHide])  
     VALUES('IsEnableInterfaceLogs','请求接口日志是否开启','0','0','0：不开启，1：开启','16','0')  
END  
--给人脸设备增加设备类型，因为新的掌静脉设备也保存到此表中，陈前良，2024-12-10  
IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'FaceDevices' AND COLUMN_NAME = 'DeviceType')  
BEGIN  
 ALTER TABLE FaceDevices ADD DeviceType VARCHAR(30) NULL  
 --将现在表中的所有设备类型都修改为人脸  
 EXEC('update FaceDevices set DeviceType = ''人脸''')  
END  
  
--云端系统售后得远程地址，原有得web1800不使用了，使用新得地址，蒋创世，2024-12-10  
if not exists(select 1 from sysPara where code = 'GemstarHelpMeUrl')  
begin  
 INSERT INTO [dbo].[sysPara]([code],[name],[refValue],[value],[remark],[seqid],[IsHide])  
     VALUES('GemstarHelpMeUrl','帮我吧地址','https://service.gshis.com/home/redirect','https://service.gshis.com/home/redirect','售后远程的地址','16','0')  
END  
  
--SAAS系统微信3.0直销通是否启用 tangcl 2025.1.14 id = 223902  
if not exists(select 1 from sysPara where code = 'isGsWxDirectSalesChannel')  
begin  
 INSERT INTO [dbo].[sysPara]([code],[name],[refValue],[value],[remark],[seqid],[IsHide])  
     VALUES('isGsWxDirectSalesChannel','微信3.0直销通是否启用','','','SAAS系统微信3.0直销通是否启用：0不启用，1启用','18','0')  
END  
--微信3.0直销通地址 tangcl 2025.1.14 id = 223902  
if not exists(select 1 from sysPara where code = 'GsWxDirectSalesChannelUrl')  
begin  
 INSERT INTO [dbo].[sysPara]([code],[name],[refValue],[value],[remark],[seqid],[IsHide])  
     VALUES('GsWxDirectSalesChannelUrl','微信3.0直销通地址','https://eshop.gshis.net','https://eshop.gshis.net','微信3.0直销通地址','18','0')  
END  
--酒店表增加最后一次登陆时间 job：224747 fl 2025-03-10  
IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('hotel') AND name = 'lastLoginDate')  
BEGIN  
 ALTER TABLE hotel ADD lastLoginDate datetime NULL  
END  
--增加POS云打印机对应关联表，2025-3-15 jiangcs  
IF OBJECT_ID('PosCloudPrinter') IS NULL  
BEGIN    
create table PosCloudPrinter (  
 id BIGINT IDENTITY(1,1) PRIMARY KEY, -- 自增主键  
    name VARCHAR(300) NOT NULL, -- 打印机名称  
    hid varchar(20) NOT NULL, -- 酒店id  
    grpid varchar(20), -- 酒店集团id  
    sn VARCHAR(255) NOT NULL, -- 云打印机编码  
    cdate DATETIME  NOT NULL, -- 添加时间  
    status tinyint NOT NULL, -- 状态 （1启用；51禁用；）  
 printType varchar(200)  --打印机类型（sunmi商米云打印机）  
)   
END  
-- 商米开发者appid，2025-3-15 jiangcs  
if not exists(select 1 from sysPara where code = 'SunmiAppid')  
begin  
 INSERT INTO [dbo].[sysPara]([code],[name],[refValue],[value],[remark],[seqid],[IsHide])  
    VALUES('SunmiAppid','商米开发者appid','521fe05bb2f040b6bdeaafe680e6ecd9','521fe05bb2f040b6bdeaafe680e6ecd9','商米开发者appid','18','0')  
END  
-- 商米开发者密钥，2025-3-15 jiangcs  
if not exists(select 1 from sysPara where code = 'SunmiAppSecret')  
begin  
 INSERT INTO [dbo].[sysPara]([code],[name],[refValue],[value],[remark],[seqid],[IsHide])  
    VALUES('SunmiAppSecret','商米开发者密钥','9cf8c3f98da64c7ea8dd5ce885158b84','9cf8c3f98da64c7ea8dd5ce885158b84','商米开发者密钥','18','0')  
END  
  
--增加美团直连接口管理表，2025-03-26 lizw  
IF OBJECT_ID('MeiTuanShopList') IS NULL  
BEGIN   
CREATE TABLE [dbo].[MeiTuanShopList](  
  
 [DeveloperId] [nvarchar](100) NULL,--开发者ID  
 [SignKey] [nvarchar](3000) NULL,--开发者密钥  
 [BusinessId] [nvarchar](500) NULL,--业务类型ID  
 [Scope] [nvarchar](500) NULL,--授权权限范围  
   
 [AppShopID] [nvarchar](64)  PRIMARY key NOT NULL,  --商户ID  
  
 [HotelCode] [nvarchar](50) NULL,   --捷云酒店代码  
 [HotelName] [nvarchar](50) NULL,   --商户名称  
  
 AccessToken [nvarchar](500) NULL,--访问令牌  
 ExpiresIn DATETIME NULL,--访问令牌-过期时间  
 RefreshToken [nvarchar](500) NULL,--更新令牌  
  
 [CreateDate] [datetime] NULL,   --创建时间  
 [ModifyDate] [datetime] NULL,   --修改时间   
 [status] tinyint not null DEFAULT 1,  
)  
END  
IF NOT EXISTS(SELECT 1 FROM syscolumns WHERE id = OBJECT_ID('hotel') AND name = 'smsHotelName')  
BEGIN  
 ALTER TABLE dbo.hotel ADD smsHotelName varchar(60) NULL  
END  
--ISPA新公众号通用模版消息  
if not exists(select * from sysPara where code = 'ISPANewTemplateID')  
begin  
 insert into sysPara(code,name,refValue,value,remark,seqid)  
 values('ISPANewTemplateID','ISPA新公众号通用模版消息','','','ISPA新公众号通用模版消息',1000)  
end  ";

    /// <summary>
    /// 场景：获取前面没有注释的存储过程语句的第一个完整SQL语句。
    /// 期望：从0开始获取第一个完整SQL语句，应该返回create proc ... as
    /// </summary>
    [Fact]
    public void GetFirstCompleteSqlSentenceFromUpdateSys_From0_ReturnsCreateAs()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_update_sys_sql), out var errors);

        Assert.Empty(errors);

        int startIndex = 0;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"CREATE procedure [dbo].[a_update_Sys]  
as";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count, startIndex);
        Assert.Equal(10, startIndex);
    }
    /// <summary>
    /// 场景：as后面带多行注释时，从as后面开始获取
    /// 期望：从as后面开始获取第一个完整SQL语句，应该返回多行注释
    /// </summary>
    [Fact]
    public void GetFirstCompleteSqlSentenceFromUpdateSys_From10_ReturnsMultilineComment()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_update_sys_sql), out var errors);

        Assert.Empty(errors);

        int startIndex = 10;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"  
/****************************************************************************  
作者：陈提见  
日期：2016-05-7  
功能：命名成这样是为了这个最常用的存储过程排序在最前面  
  
这个存储过程的作用是为了程序启用后用来更改数据库结构或加入一些固定数据，例如系统参数，权限列表等。  
   
exec a_update_Sys  
****************************************************************************/  
 ";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count + 10, startIndex);
        Assert.Equal(16, startIndex);
    }
    /// <summary>
    /// 场景：注释后是多行delete语句
    /// 期望：从注释开始获取第一个完整SQL语句，应该返回第一个delete 语句
    /// </summary>
    [Fact]
    public void GetFirstCompleteSqlSentenceFromUpdateSys_From16_ReturnsFirstDelete()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_update_sys_sql), out var errors);

        Assert.Empty(errors);

        int startIndex = 16;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"DELETE FROM dbo.sysLog WHERE cDate< DATEADD(DAY,-30,GETDATE())";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count + 16, startIndex);
        Assert.Equal(40, startIndex);
    }

    /// <summary>
    /// 场景：多行delete语句，从第一行delete语句后开始获取
    /// 期望：从第一行语句结束开始获取第一个完整SQL语句，应该返回第一个delete语句和第二个delete语句中间的空白
    /// </summary>
    [Fact]
    public void GetFirstCompleteSqlSentenceFromUpdateSys_From40_ReturnsNext()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_update_sys_sql), out var errors);

        Assert.Empty(errors);

        int startIndex = 40;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"  
 ";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count + 40, startIndex);
        Assert.Equal(43, startIndex);
    }

    [Fact]
    public void GetFirstCompleteSqlSentenceFromUpdateSys_From43_ReturnsNext()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_update_sys_sql), out var errors);

        Assert.Empty(errors);

        int startIndex = 43;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"DELETE FROM dbo.slowlog WHERE logTime< DATEADD(DAY,-30,GETDATE())";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count + 43, startIndex);
        Assert.Equal(67, startIndex);
    }
    [Fact]
    public void GetFirstCompleteSqlSentenceFromUpdateSys_From67_ReturnsNext()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_update_sys_sql), out var errors);

        Assert.Empty(errors);

        int startIndex = 67;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"  
  
/* 初始化菜单开始 */   
  
 ";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count + 67, startIndex);
        Assert.Equal(77, startIndex);
    }
    [Fact]
    public void GetFirstCompleteSqlSentenceFromUpdateSys_From77_ReturnsNext()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_update_sys_sql), out var errors);

        Assert.Empty(errors);

        int startIndex = 77;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"begin --初始化菜单  
     
  delete authlist  
  delete AuthButtons  
  --DELETE AuthButtons   
  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,mask) values('0','1','捷信达捷云系统运营管理',1,'1001000000')  
    
  --运营平台一级菜单放在一起重新排序  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','10','版本列表',180,'','VersionList','Index','1001000000','material-icon')  
        insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','11','产品版本管理',140,'','ProductVersionList','Index','1001000000','material-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','12','运营参数管理',150,'','OperatingParam','Index','1001000000','material-icon')  
        insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','20','用户管理',160,'','UserList','Index','1001000000','user-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','30','角色管理',170,'','RoleList','Index','1001000000','user-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','40','服务器管理',190,'','ServerList','Index','1001000000','server-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','50','数据库管理',200,'','DbList','Index','1001000000','datasorce-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','60','系统日志管理',270,'','SysLog','Index','1001000000','log-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','61','操作日志管理',280,'','SysOpLog','Index','1001000000','log-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','70','广告设置',60,'','AdSet','Index','1001000000','ad-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','80','新店及酒店维护',10,'','Hotel','Index','1001000000','hotel-m-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','90','客户到期预报',90,'','HotelExpire','Index','1001000000','hotel-m-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','91','客户延期日志',100,'','HotelDelayLog','Index','1001000000','hotel-m-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','92','客户授权管理',110,'','AuthorizeList','Index','1001000000','hotel-m-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','100','试用体验管理',130,'','TryInfo','Index','1001000000','tiyan-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','110','平台系统参数',210,'','SysPara','Index','1001000000','platform-system-icon')   
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','120','硬件接口版本管理',220,'','HardwareInterface','Index','1001000000','hardware-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','130','智能POS设备管理',30,'','HotelPos','Index','1001000000','hardware-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','135','人脸识别设备管理',40,'','FaceDevices','Index','1001000000','hardware-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','140','售后服务工程师',120,'','ServiceOperator','Index','1001000000','user-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','150','帮助文档管理',80,'','HelpFiles','Index','1001000000','help-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','160','系统公告',70,'','Notice','Index','1001000000','help-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','170','美团接口管理',230,'','PoleStar','Index','1001000000','hardware-icon')    
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','171','美团接口管理新',230,'','MeiTuanShop','Index','1001000000','hardware-icon')    
  --insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','210','口碑接口日志',260,'','KBOpenApiLog','Index','1001000000','log-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','211','抖音接口管理',240,'','TikTok','Index','1001000000','hardware-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','212','新口碑接口管理',242,'','NewKouBei','Index','1001000000','hardware-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','213','快手接口管理',245,'','KuaiShou','Index','1001000000','hardware-icon')    
  --insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','180','北极星接口日志',240,'','OpenApiLog','Index','1001000000','log-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','190','服务器性能查看',290,'','ServerListPerformance','Index','1001000000','datasorce-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','200','口碑接口管理',250,'','KouBei','Index','1001000000','hardware-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','220','程序更新管理',300,'','AppUpdate','Index','1001000000','public-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','230','扫码点餐酒店管理',20,'','HotelSM','Index','1001000000','public-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','240','捷音数量控制',50,'','HotelVoiceQty','Index','1001000000','hotel-m-icon')  
  insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','250','合作伙伴设置',85,'','PartnerSet','Index','1001000000','user-icon')  
  INSERT INTO dbo.authlist(AuthCode ,ParentCode ,AuthName ,Area ,Controller ,Action ,mask ,class ,Seqid)VALUES  ( '400' , '1' , 'SCM客户等级管理' , '' , 'HotelLevel' , 'Index' , '1001000000' , 'hotel-m-icon' , 400  )      
  INSERT INTO dbo.authlist(AuthCode ,ParentCode ,AuthName ,Area ,Controller ,Action ,mask ,class ,Seqid)VALUES  ( '401' , '1' , 'SCM酒店物品类别关联' , '' ,'Hotel' , 'HotelItemcategoryRelation' , '1001000000' , 'hotel-m-icon' , 401  )  
  INSERT INTO dbo.authlist(AuthCode ,ParentCode ,AuthName ,Area ,Controller ,Action ,mask ,class ,Seqid)VALUES  ( '402' , '1' , 'SCM物品类别管理' , '' , 'ItemCategory' , 'Index' , '1001000000' , 'hotel-m-icon' , 402  )  
  INSERT INTO dbo.authlist(AuthCode ,ParentCode ,AuthName ,Area ,Controller ,Action ,mask ,class ,Seqid)VALUES  ( '403' , '1' , 'SCM供应商待确认' , '' , 'Supplier' , 'Confirmed' , '1001000000' , 'hotel-m-icon' , 403  )  
  INSERT INTO dbo.authlist(AuthCode ,ParentCode ,AuthName ,Area ,Controller ,Action ,mask ,class ,Seqid)VALUES  ( '404' , '1' , 'SCM供应商管理' , '' , 'Supplier' ,'Index' , '1001000000' , 'hotel-m-icon' ,404  )  
  INSERT INTO dbo.authlist(AuthCode ,ParentCode ,AuthName ,Area ,Controller ,Action ,mask ,class ,Seqid)VALUES  ( '405' , '1' , 'SCM供应商意见反馈列表' , '' , 'FeedBack' , 'Index' , '1001000000' , 'hotel-m-icon' , 405  )    
        insert into authlist(ParentCode,AuthCode,AuthName,Seqid,Area,Controller,[Action],mask,class) values('1','260','营业点扫呗支付管理',310,'','RefeSbPayConfigure','Index','1001000000','hardware-icon')         
    
  --新店及酒店维护  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',1,1,'Query','查询')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',2,2,'Add','增加')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',131072,3,'Update','修改')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',4,3,'Save','保存')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',64,4,'Enable','酒店管理-启用')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',128,5,'Disable','酒店管理-禁用')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',16,6,'ChannelReSetKey','渠道设置-重新生成密钥')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',32,7,'FuncReSetKey','通用功能设置-重新生成密钥')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',256,8,'OtherIsable','其他设置-启用禁用')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',2048,9,'ItemSet','项目设置')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',4096,10,'ChannnelSet','渠道设置')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',8192,11,'FunctionSet','功能设置')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',16384,12,'InterfaceSet','接口设置')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',32768,13,'SystemParaSet','系统参数设置')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',65536,14,'OperatingParaSet','运营参数设置')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',262144,15,'SyncMaster','同步Master')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',8,16,'Delete','集团分店与单店互转')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('80',8388608,17,'Excel','导出Excel')  
    
  --客户授权管理  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('92',1,1,'Query','查询')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('92',2,2,'Add','生成')  
    
  --合作伙伴设置  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('250',1,1,'Query','查询')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('250',2,2,'Add','增加')   
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('250',131072,3,'Update','修改')  
    
  --平台系统参数  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('110',1,1,'Query','查询')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('110',131072,3,'Update','修改')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('110',4,3,'Save','保存')  
     
  --智能POS设备管理  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('130',1,1,'Query','查询')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('130',2,2,'Add','增加')   
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('130',131072,3,'Update','修改')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('130',4,3,'Save','保存')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('130',8,4,'Delete','删除')  
    
  --售后服务工程师  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('140',1,1,'Query','查询')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('140',2,2,'Add','增加')   
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('140',131072,3,'Update','修改')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('140',4,3,'Save','保存')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('140',8,4,'Delete','删除')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('140',64,5,'Enable','启用')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('140',128,6,'Disable','禁用')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('140',1024,7,'ResetPwd','重置密码')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('140',512,8,'UnbindWeChat','解除绑定微信')  
     
  --帮助文档管理  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('150',1,1,'Query','查询')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('150',131072,3,'Update','修改')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('150',4,3,'Save','保存')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('150',8,4,'Delete','删除')  
     
  --系统公告  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('160',1,1,'Query','查询')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('160',2,2,'Add','增加')   
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('160',131072,3,'Update','修改')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('160',4,3,'Save','保存')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('160',8,4,'Delete','删除')  
    
  --捷音数量控制  
  insert into AuthButtons(AuthButtonId,AuthId,AuthButtonName,AuthButtonValue,seqid) values('Query',240,'查询',1,1)   
  insert into AuthButtons(AuthButtonId,AuthId,AuthButtonName,AuthButtonValue,seqid) values('Add',240,'增加',2,2)   
  insert into AuthButtons(AuthButtonId,AuthId,AuthButtonName,AuthButtonValue,seqid) values('Update',240,'修改',131072,3)  
  insert into AuthButtons(AuthButtonId,AuthId,AuthButtonName,AuthButtonValue,seqid) values('Delete',240,'删除',8,4)  
    
  --扫码点餐酒店管理  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('230',1,1,'Query','查询')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('230',2,2,'Add','增加')   
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('230',131072,3,'Update','修改')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('230',4,3,'Save','保存')  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('230',8,4,'Delete','删除')  
  
  --营业点扫呗支付管理  
  insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('260',1,1,'Query','查询')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('260',2,2,'Add','增加')   
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('260',131072,3,'Update','修改')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('260',4,3,'Save','保存')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('260',8,4,'Delete','删除')  
          
        --客户到期预报表  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('90',1,1,'Query','查询')  
        insert into AuthButtons(AuthId,AuthButtonValue,Seqid,AuthButtonId,AuthButtonName) values('90',4,3,'Save','保存')  
  
  DELETE roleauth WHERE authcode NOT IN (SELECT authcode FROM authlist)  
  
 end";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count + 77, startIndex);
        Assert.Equal(4398, startIndex);
    }

    [Fact]
    public void GetFirstCompleteSqlSentenceFromUpdateSys_From4398_ReturnsNext()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_update_sys_sql), out var errors);

        Assert.Empty(errors);

        int startIndex = 4398;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"  
   
/* 初始化菜单结束 */   
   
  
--增加数据库实例的只读连接信息，陈前良，2018-10-17 11:32:27  
";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count + 4398, startIndex);
        Assert.Equal(4411, startIndex);
    }

    [Fact]
    public void GetFirstCompleteSqlSentenceFromUpdateSys_From4411_ReturnsNext()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_update_sys_sql), out var errors);

        Assert.Empty(errors);

        int startIndex = 4411;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"IF NOT EXISTS(SELECT * FROM syscolumns WHERE  id = OBJECT_ID('dbList') AND name = 'readonlyDbServer')  
BEGIN  
    ALTER TABLE dbList ADD readonlyDbServer VARCHAR(200)  
    ALTER TABLE dbList ADD readonlyDbName VARCHAR(200)  
    ALTER TABLE dbList ADD readonlyLogId VARCHAR(30)  
    ALTER TABLE dbList ADD readonlyLogPwd VARCHAR(30)  
END";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count + 4411, startIndex);
        Assert.Equal(4518, startIndex);
    }
    [Fact]
    public void GetFirstCompleteSqlSentenceFromUpdateSys_From4518_ReturnsNext()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_update_sys_sql), out var errors);

        Assert.Empty(errors);

        int startIndex = 4518;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"  
--修改pos设备表，另外增加一个id来做为主键，原来的设备编号也允许进行修改  
";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count + 4518, startIndex);
        Assert.Equal(4522, startIndex);
    }
    [Fact]
    public void GetFirstCompleteSqlSentenceFromUpdateSys_From4522_ReturnsNext()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_update_sys_sql), out var errors);

        Assert.Empty(errors);

        int startIndex = 4522;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"IF NOT EXISTS(SELECT * FROM syscolumns WHERE ID = OBJECT_ID('HotelPos') AND name = 'Id')  
BEGIN  
    ALTER TABLE HotelPos ADD ID UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()  
    ALTER TABLE HotelPos DROP CONSTRAINT pk_hotelPos  
    ALTER TABLE hotelPos ADD CONSTRAINT pk_hotelPos PRIMARY KEY(ID)  
END";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count + 4522, startIndex);
        Assert.Equal(4620, startIndex);
    }
    [Fact]
    public void GetFirstCompleteSqlSentenceFromUpdateSys_From4620_ReturnsNext()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_update_sys_sql), out var errors);

        Assert.Empty(errors);

        int startIndex = 4620;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"  
  
";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count + 4620, startIndex);
        Assert.Equal(4624, startIndex);
    }
    [Fact]
    public void GetFirstCompleteSqlSentenceFromUpdateSys_From4624_ReturnsNext()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_update_sys_sql), out var errors);

        Assert.Empty(errors);

        int startIndex = 4624;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"IF NOT EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('hotel') AND name = 'customerStatus')  
BEGIN  
 ALTER TABLE hotel ADD customerStatus VARCHAR(2) NOT NULL DEFAULT '0'  
END";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count + 4624, startIndex);
        Assert.Equal(4686, startIndex);
    }
    [Fact]
    public void GetFirstCompleteSqlSentenceFromUpdateSys_From4686_ReturnsNext()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_update_sys_sql), out var errors);

        Assert.Empty(errors);

        int startIndex = 4686;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"  
  
--短信发送日志增加字段[account]账号 2018-11-29 10:56:26 李泽锐 134350  
";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count + 4686, startIndex);
        Assert.Equal(4692, startIndex);
    }
    [Fact]
    public void GetFirstCompleteSqlSentenceFromUpdateSys_From6114_ReturnsNext()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_update_sys_sql), out var errors);

        Assert.Empty(errors);

        int startIndex = 6114;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"if OBJECT_ID('AppUpdateHistory') is null  
CREATE TABLE [dbo].[AppUpdateHistory](  
 [ID] [uniqueidentifier] NOT NULL,  
 [AppUpdateID] [uniqueidentifier] NOT NULL,  
 [VersionDesc] [varchar](1000) NOT NULL,  
 [ApplicationStartName] [varchar](1000) NOT NULL,  
 [VersionSort] [bigint] NOT NULL,  
 [ReleaseVersion] [varchar](100) NOT NULL,  
 [ReleaseDate] [datetime] NOT NULL,  
 [ReleaseUrl] [varchar](1000) NOT NULL,  
 [UpdateMode] [varchar](100) NOT NULL,  
 [ServerlistID] [varchar](100) NOT NULL,  
 [Status] [int] NOT NULL,  
 [PublishDate] [datetime] NOT NULL,  
 [ModifiedDate] [datetime] NOT NULL,  
 [ModifiedUser] [varchar](100) NOT NULL,  
 [IsAll] [bit] NOT NULL,  
 [IsMust] [bit] NOT NULL,  
 CONSTRAINT [PK__AppUpdat__3214EC271EE485AA] PRIMARY KEY CLUSTERED   
(  
 [ID] ASC  
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]  
) ON [PRIMARY]";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count + 6114, startIndex);
        Assert.Equal(6403, startIndex);
    }
    [Fact]
    public void GetFirstCompleteSqlSentenceFromUpdateSys_From4371_ReturnsNext()
    {
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(_update_sys_sql), out var errors);

        Assert.Empty(errors);

        int startIndex = 4371;
        var tokens = fragment.GetFirstCompleteSqlTokens(ref startIndex);
        var expectedFirst = @"DELETE roleauth WHERE authcode NOT IN (SELECT authcode FROM authlist)";
        Assert.Equal(expectedFirst, string.Concat(tokens.Select(w => w.Text)));
        Assert.Equal(tokens.Count + 4371, startIndex);
        Assert.Equal(4392, startIndex);
    }

    #endregion
}
