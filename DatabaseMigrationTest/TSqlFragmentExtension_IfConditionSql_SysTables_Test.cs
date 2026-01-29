using DatabaseMigration.Migration;
using DatabaseMigration.ScriptGenerator;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigrationTest;

public class TSqlFragmentExtension_IfConditionSql_SysTables_Test
{
    [Fact]
    public void IsIfNotExistsSelectFromSysTablesWhereNameEqualCondition_Basic()
    {
        var sql = "if not exists(select * from sys.tables where name = 'HotelVoiceQtys')\r\nbegin\r\n create table HotelVoiceQtys( Id uniqueidentifier not null )\r\nend";
        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);
        Assert.Empty(errors);

        var ok = fragment.ScriptTokenStream.IsIfNotExistsSelectFromSysTablesWhereNameEqualCondition(out var name);
        Assert.True(ok);
        Assert.Equal("hotelvoiceqtys", name);
    }

    [Fact]
    public void Convert_IfNotExistsSysTables_CreateTable_To_Postgres()
    {
        var sql = @"if not exists(select * from sys.tables where name = 'HotelVoiceQtys')  
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
end";

        var frag = sql.ParseToFragment();
        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(frag.ScriptTokenStream);

        var expected = @"IF to_regclass('hotelvoiceqtys') IS NULL THEN 
  
 CREATE TABLE hotelvoiceqtys (
        id uuid NOT NULL, --主键值
        hid varchar(6) NOT NULL, --酒店id
        qtytype varchar(20) NOT NULL, --数量类型，目前支持Install,MessagePush，其中Install控制安装数量，MessagePush控制推送数量
        qty integer NOT NULL, --数量值
        qtyexpired timestamp NOT NULL, --数量失效时间
        checkcode varchar(6) NOT NULL, --验证码，由程序随机生成，只有在输入正确的hid+CheckCode的情况下，才允许扣减数量
        usedqty integer NULL, --已使用数量，冗余字段，避免每次计算使用数量都要从明细表中进行统计
        remark varchar(200) NULL, --备注
        creator varchar(30) NULL, --创建者
        cdate timestamp NULL, --创建时间
        PRIMARY KEY (id)
);
 END IF;
";
        Assert.Equal(expected, result);
    }
}
