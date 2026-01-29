using DatabaseMigration.Migration;
using DatabaseMigration.ScriptGenerator;

namespace DatabaseMigrationTest;

public class PostgreSqlScriptGenerator_CreateTable_Tests
{
    [Fact]
    public void Convert_CreateTable_HuiYiMapping_To_Postgres()
    {
        var sql = @"CREATE TABLE HuiYiMapping(
		[id] [uniqueidentifier] NOT NULL primary key,
		[hid] [char](6),
		[openId] [varchar](60)
	)";

        var frag = sql.ParseToFragment();
        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(frag.ScriptTokenStream);

        var expected = @"CREATE TABLE huiyimapping (
        id uuid NOT NULL PRIMARY KEY,
        hid char(6) NULL,
        openid varchar(60) NULL
);";
        Assert.Equal(expected, result);
    }
    [Fact]
    public void Convert_CreateTable_WeixinLongCustomerMapping_To_Postgres()
    {
        var sql = @"create table weixinLongCustomerMapping(  
   [mappingId] [int] IDENTITY(1,1) NOT NULL primary key,  
   [hid] [varchar](6) NOT NULL,  
   [customerWxOpenId] [varchar](60) NOT NULL,  
   [cdate] [datetime] NOT NULL,  
     )";

        var frag = sql.ParseToFragment();
        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(frag.ScriptTokenStream);

        var expected = @"CREATE TABLE weixinlongcustomermapping (
        mappingid serial NOT NULL PRIMARY KEY,
        hid varchar(6) NOT NULL,
        customerwxopenid varchar(60) NOT NULL,
        cdate timestamp NOT NULL
);";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Convert_CreateTable_WeixinLongCustomer2Mapping_To_Postgres()
    {
        var sql = @"create table weixinLongCustomerMapping(  
   [mappingId] [int] NOT NULL IDENTITY(1,1) primary key,  
   [hid] [varchar](6) NOT NULL,  
   [customerWxOpenId] [varchar](60) NOT NULL,  
   [cdate] [datetime] NOT NULL,  
     )";

        var frag = sql.ParseToFragment();
        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(frag.ScriptTokenStream);

        var expected = @"CREATE TABLE weixinlongcustomermapping (
        mappingid serial NOT NULL PRIMARY KEY,
        hid varchar(6) NOT NULL,
        customerwxopenid varchar(60) NOT NULL,
        cdate timestamp NOT NULL
);";
        Assert.Equal(expected, result);
    }
    [Fact]
    public void Convert_CreateTable_ColumnDefineHasRemarkInNextLine_To_Postgres()
    {
        var sql = @"if OBJECT_ID('HotelInterface') is null  
begin  
  create table HotelInterface  
  (  
   id uniqueidentifier not null primary key--主键ID  
  ,hid char(6) not null--酒店ID  
  ,typeCode varchar(100) not null--接口类型  
  ,code varchar(100) not null--接口值  
  );  
end  ";

        var frag = sql.ParseToFragment();
        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(frag.ScriptTokenStream);

        var expected = @"IF to_regclass('hotelinterface') IS NULL THEN 
  
  CREATE TABLE hotelinterface (
        id uuid NOT NULL PRIMARY KEY, --主键ID
        hid char(6) NOT NULL, --酒店ID
        typecode varchar(100) NOT NULL, --接口类型
        code varchar(100) NOT NULL --接口值
);
 END IF;
";
        Assert.Equal(expected, result);
    }
    [Fact]
    public void Convert_CreateTable_ColumnDefineHasRemarkInSameLine_To_Postgres()
    {
        var sql = @"if OBJECT_ID('sysOpLog') is null  
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
end ";

        var frag = sql.ParseToFragment();
        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(frag.ScriptTokenStream);

        var expected = @"IF to_regclass('sysoplog') IS NULL THEN 
  
     CREATE TABLE sysoplog (
        cdate timestamp NOT NULL, --操作时间
        cuser varchar(30) NOT NULL, --操作员
        ip varchar(30) NULL, --操作Ip
        xtype varchar(30) NULL, --操作类型
        ctext varchar(8000) NULL, --操作内容
        keys varchar(60) NULL, --操作数据关键字
        logid bigserial NOT NULL PRIMARY KEY
);
 END IF;
";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Convert_CreateTable_WxOpenAuthorization_To_Postgres()
    {
        var sql = @"if object_id('WxOpenAuthorization') is null  
begin  
 CREATE TABLE [dbo].[WxOpenAuthorization] (  
  [Id] int IDENTITY(1,1) NOT NULL ,  
  [ComponentAppid] varchar(256) NULL,  
  [AuthorizerAppId] varchar(256) NULL,  
  [AuthorizerRefreshToken] varchar(256) NULL,    
  [ModifiedDate] datetime NULL,  
  [ModifiedUser] varchar(56) NULL
 PRIMARY KEY CLUSTERED   
 (  
  [Id] ASC  
 )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]  
 ) ON [PRIMARY]  
end";

        var frag = sql.ParseToFragment();
        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(frag.ScriptTokenStream);

        var expected = @"IF to_regclass('wxopenauthorization') IS NULL THEN 
  
 CREATE TABLE wxopenauthorization (
        id serial NOT NULL,
        componentappid varchar(256) NULL,
        authorizerappid varchar(256) NULL,
        authorizerrefreshtoken varchar(256) NULL,
        modifieddate timestamp NULL,
        modifieduser varchar(56) NULL,
 PRIMARY KEY (id)
);
 END IF;
";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Convert_CreateTable_AppUpdateHistory_To_Postgres()
    {
        var sql = @"CREATE TABLE [dbo].[AppUpdateHistory](  
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

        var frag = sql.ParseToFragment();
        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(frag.ScriptTokenStream);

        var expected = @"CREATE TABLE appupdatehistory (
        id uuid NOT NULL,
        appupdateid uuid NOT NULL,
        versiondesc varchar(1000) NOT NULL,
        applicationstartname varchar(1000) NOT NULL,
        versionsort bigint NOT NULL,
        releaseversion varchar(100) NOT NULL,
        releasedate timestamp NOT NULL,
        releaseurl varchar(1000) NOT NULL,
        updatemode varchar(100) NOT NULL,
        serverlistid varchar(100) NOT NULL,
        status integer NOT NULL,
        publishdate timestamp NOT NULL,
        modifieddate timestamp NOT NULL,
        modifieduser varchar(100) NOT NULL,
        isall boolean NOT NULL,
        ismust boolean NOT NULL,
        PRIMARY KEY (id)
);";
        Assert.Equal(expected, result);
    }
}
