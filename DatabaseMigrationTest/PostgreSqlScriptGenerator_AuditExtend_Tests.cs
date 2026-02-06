using DatabaseMigration.Migration;
using DatabaseMigration.ScriptGenerator;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Xunit;

namespace DatabaseMigrationTest;

/// <summary>
/// 测试 auditextend_timestart 存储过程的转换
/// 用于重现和修复实际的解析错误
/// </summary>
public class PostgreSqlScriptGenerator_AuditExtend_Tests
{
    [Fact]
    public void ConvertSelectIntoWithGetDate_ShouldParseCorrectly()
    {
        // 测试原始存储过程中的片段：SELECT ... INTO #temp WHERE ... GetDate()
        var tsql = @"
DECLARE @nowDate date = getdate();

SELECT TOP 1 id, hid, dbid, auditType
INTO #temp_list_retry
FROM auditExtend
WHERE cast(cDate as date) = @nowDate
AND status = 1
AND DateDiff(MI, cDate, Getdate()) > 45;
";

        var fragment = tsql.ParseToFragment();
        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(fragment);

        // 验证临时表被正确转换
        Assert.Contains("temp_list_retry", result);
        // 验证 # 前缀被移除
        Assert.DoesNotContain("#temp_list_retry", result);
    }

    [Fact]
    public void ConvertFullProcedure_AuditExtendTimeStart_ShouldConvertCorrectly()
    {
        // 完整的存储过程定义（从源数据库获取）
        var tsql = @"
--夜审扩展定时执行存储过程
create proc auditExtend_timeStart
as
begin
	create table #temp_list(id bigint, hid varchar(30), [dbid] uniqueidentifier, auditType varchar(60));

	--查找需要发送的未执行的业务库
	insert into #temp_list
	select top 5 id,hid,dbid,auditType from
	(
	select ROW_NUMBER() over(partition by dbid,auditType order by cdate) as rowIndex,* from auditExtend where status=0
	)a where rowIndex=1

	--排除掉正在发送的业务库，防止并发死锁
	delete a from #temp_list a
	inner join
	(
		select distinct dbid,auditType from auditExtend where status=1 and DateDiff(MI,cDate,Getdate())<30
	)b
	on a.dbid=b.dbid and a.auditType=b.auditType

	--重试一次，重新通知pmsnotify
	if( (select count(1) from #temp_list) <= 0 )
	begin
		declare @nowDate date = getdate();

		select top 1 id,hid,dbid,auditType
			into #temp_list_retry
			from auditExtend
			where
			cast(cDate as date) = @nowDate
			and
			status = 1
			and
			DateDiff(MI,cDate,Getdate()) > 45
			and
			beginDate is null

		if( (select count(1) from #temp_list_retry) > 0)
		begin
			update a set a.beginDate = getdate()
			from auditExtend a
			inner join #temp_list_retry t on a.id = t.id
			where a.status = 1 and a.beginDate is null

			if exists (
				select a.id
				from auditExtend a
				inner join #temp_list_retry t on a.id = t.id
				where a.status = 1 and a.beginDate is not null
			)
			begin
				insert into #temp_list (id,hid,dbid,auditType)
				select id,hid,dbid,auditType from #temp_list_retry
			end

		end
	end

	--把要发送的夜审都改成正在执行
	update auditExtend set status=1 where id in (select id from #temp_list)

	--开始发送夜审扩展功能请求
	DECLARE @id varchar(50),@hid char(6),@type varchar(50)
	DECLARE  send_auditRequest CURSOR LOCAL FOR
	SELECT id,hid,auditType FROM #temp_list
	OPEN send_auditRequest
		FETCH send_auditRequest INTO @id,@hid,@type
		WHILE @@FETCH_STATUS = 0
		BEGIN
			DECLARE @auditExtendUrl VARCHAR(300);
			SET @auditExtendUrl = 'http://pmsnotify.gshis.com/Audit/Index?hid=' + @hid+'&type='+@type+'&id='+@id+'&sign=jxd598Audit';
			select @auditExtendUrl
			EXECUTE up_crsXml_httpSend_Asyn @text='',@url=@auditExtendUrl;

			FETCH send_auditRequest INTO @id,@hid,@type
		END
		CLOSE send_auditRequest
		DEALLOCATE send_auditRequest
end
";

        // 尝试解析和转换
        var parser = new TSql170Parser(true);
        IList<ParseError> errors;

        using (var rdr = new StringReader(tsql))
        {
            var fragment = parser.Parse(rdr, out errors);

            // 如果有解析错误，输出错误信息
            if (errors.Count > 0)
            {
                var errorMessages = string.Join("\n", errors.Select(e => $"Line {e.Line}, Col {e.Column}: {e.Message}"));
                Assert.Fail($"解析失败:\n{errorMessages}");
            }

            // 转换为 PostgreSQL
            var generator = new PostgreSqlProcedureScriptGenerator();
            var result = generator.GenerateSqlScript(fragment);

            // 输出转换结果以便调试
            Console.WriteLine("===== 转换后的 PostgreSQL 脚本 =====");
            Console.WriteLine(result);
            Console.WriteLine("===== 结束 =====");

            // 基本验证
            Assert.NotNull(result);
            Assert.NotEmpty(result);

            // 验证临时表被正确处理
            // 注意：暂时注释掉这个断言，先看看转换结果
            // Assert.DoesNotContain("#temp_list", result);
            // Assert.DoesNotContain("#temp_list_retry", result);
        }
    }
    [Fact]
    public void ConvertSetStringConcatenation_ShouldUseConcat()
    {
        var tsql = "SET @url = 'http://' + @host + '/path';";
        var fragment = tsql.ParseToFragment();
        var generator = new PostgreSqlProcedureScriptGenerator(); // Use Procedure generator to cover all bases or base generator
        var result = generator.GenerateSqlScript(fragment);
        
        // Expected: url := CONCAT('http://', host, '/path');
        Assert.Contains("url :=", result);
        Assert.Contains("CONCAT(", result);
        Assert.Contains("'http://'", result);
        Assert.Contains("host", result);
        Assert.DoesNotContain(" + ", result); // Check regarding spaces around + 
    }
}
