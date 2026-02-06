
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
		select distinct dbid,auditType from auditExtend where status=1 and DateDiff(MI,cDate,Getdate())<30--超过30分钟的则不排除
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
		cast(cDate as date) = @nowDate--当天
		and
		status = 1--状态还是1，没有变成2，2表示Notify站点收到了请求并且正在执行;
		and 
		DateDiff(MI,cDate,Getdate()) > 45--超过45分钟
		and
		beginDate is null--只重试一次

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