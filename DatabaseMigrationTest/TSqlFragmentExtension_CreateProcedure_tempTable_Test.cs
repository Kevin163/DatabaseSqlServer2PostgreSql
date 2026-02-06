//using DatabaseMigration.ScriptGenerator;
//using Microsoft.SqlServer.TransactSql.ScriptDom;

//namespace DatabaseMigrationTest;

//public class TSqlFragmentExtension_CreateProcedure_tempTable_Test
//{
//    [Fact]
//    public void Test_MigrateAuditExtendTimeStartProcedure()
//    {
//        // Arrange: SQL Server procedure script
//        string sql = @"
//            --夜审扩展定时执行存储过程  
//create proc auditExtend_timeStart 
//as  
//            begin  
//             create table #temp_list(id bigint, hid varchar(30), [dbid] uniqueidentifier, auditType varchar(60));  
               
//             --查找需要发送的未执行的业务库  
//             insert into #temp_list  
//             select top 5 id,hid,dbid,auditType from    
//             (  
//             select ROW_NUMBER() over(partition by dbid,auditType order by cdate) as rowIndex,* from auditExtend where status=0  
//             )a where rowIndex=1  
              
//             --排除掉正在发送的业务库，防止并发死锁  
//             delete a from #temp_list a  
//             inner join    
//             (  
//              select distinct dbid,auditType from auditExtend where status=1 and DateDiff(MI,cDate,Getdate())<30--超过30分钟的则不排除  
//             )b  
//             on a.dbid=b.dbid and a.auditType=b.auditType  
               
              
//             --重试一次，重新通知pmsnotify  
//             if( (select count(1) from #temp_list) <= 0 )  
//             begin  
//              declare @nowDate date = getdate();  
              
//              select top 1 id,hid,dbid,auditType  
//              into #temp_list_retry  
//              from auditExtend    
//              where    
//              cast(cDate as date) = @nowDate--当天  
//              and  
//              status = 1--状态还是1，没有变成2，2表示Notify站点收到了请求并且正在执行;  
//              and    
//              DateDiff(MI,cDate,Getdate()) > 45--超过45分钟  
//              and  
//              beginDate is null--只重试一次  
              
//              if( (select count(1) from #temp_list_retry) > 0)  
//              begin  
//               update a set a.beginDate = getdate()  
//               from auditExtend a  
//               inner join #temp_list_retry t on a.id = t.id  
//               where a.status = 1 and a.beginDate is null  
              
//               if exists (  
//                select a.id    
//                from auditExtend a    
//                inner join #temp_list_retry t on a.id = t.id  
//                where a.status = 1 and a.beginDate is not null  
//               )  
//               begin  
//                insert into #temp_list (id,hid,dbid,auditType)  
//                select id,hid,dbid,auditType from #temp_list_retry  
//               end  
              
//              end  
//             end  
              
              
//             --把要发送的夜审都改成正在执行  
//             update auditExtend set status=1 where id in (select id from #temp_list)  
               
//             --开始发送夜审扩展功能请求  
//             DECLARE @id varchar(50),@hid char(6),@type varchar(50)  
//             DECLARE  send_auditRequest CURSOR LOCAL FOR  
//             SELECT id,hid,auditType FROM #temp_list  
//             OPEN send_auditRequest     
//              FETCH send_auditRequest INTO @id,@hid,@type  
//              WHILE @@FETCH_STATUS = 0  
//              BEGIN  
//               DECLARE @auditExtendUrl VARCHAR(300);  
//               SET @auditExtendUrl = 'http://pmsnotify.gshis.com/Audit/Index?hid=' + @hid+'&type='+@type+'&id='+@id+'&sign=jxd598Audit';  
//               select @auditExtendUrl  
//               EXECUTE up_crsXml_httpSend_Asyn @text='',@url=@auditExtendUrl;  
                 
//               FETCH send_auditRequest INTO @id,@hid,@type  
//              END  
//              CLOSE send_auditRequest  
//              DEALLOCATE send_auditRequest  
//            end
//            ";

//        // Act: Generate PostgreSQL script
//        var parser = new TSql170Parser(true);
//        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);

//        Assert.Empty(errors);

//        var converter = new PostgreSqlProcedureScriptGenerator();
//        var result = converter.GenerateSqlScript(fragment);

//        // Assert: Validate the generated script (basic validation)
//        string expectedPostgreSqlScript = @"
//            --夜审扩展定时执行存储过程  
//CREATE OR REPLACE procedure auditextend_timestart () 
//LANGUAGE plpgsql
//as $$
//DECLARE nowdate date;
//DECLARE id varchar(50);
//DECLARE hid char(6);
//DECLARE type varchar(50);
//DECLARE auditextendurl varchar(300);
//BEGIN
//    -- 创建临时表 temp_list
//    CREATE TEMP TABLE temp_list (
//        id BIGINT,
//        hid VARCHAR(30),
//        dbid UUID,
//        auditType VARCHAR(60)
//    ) ON COMMIT DROP;

//    -- 查找需要发送的未执行的业务库
//    INSERT INTO temp_list
//    SELECT id, hid, dbid, auditType
//    FROM (
//        SELECT ROW_NUMBER() OVER (PARTITION BY dbid, auditType ORDER BY cdate) AS rowIndex, *
//        FROM auditExtend
//        WHERE status = 0
//    ) a
//    WHERE rowIndex = 1;

//    -- 排除掉正在发送的业务库，防止并发死锁
//    DELETE FROM temp_list a
//    USING (
//        SELECT DISTINCT dbid, auditType
//        FROM auditExtend
//        WHERE status = 1 AND EXTRACT(MINUTE FROM (CURRENT_TIMESTAMP - cdate)) < 30
//    ) b
//    WHERE a.dbid = b.dbid AND a.auditType = b.auditType;

//    -- 重试一次，重新通知 pmsnotify
//    IF (SELECT COUNT(1) FROM temp_list) <= 0 THEN
//        CREATE TEMP TABLE temp_list_retry ON COMMIT DROP AS
//        SELECT id, hid, dbid, auditType
//        FROM auditExtend
//        WHERE cdate::DATE = nowDate
//          AND status = 1
//          AND EXTRACT(MINUTE FROM (CURRENT_TIMESTAMP - cdate)) > 45
//          AND beginDate IS NULL
//        LIMIT 1;

//        IF (SELECT COUNT(1) FROM temp_list_retry) > 0 THEN
//            UPDATE auditExtend
//            SET beginDate = CURRENT_TIMESTAMP
//            FROM temp_list_retry t
//            WHERE auditExtend.id = t.id AND auditExtend.status = 1 AND auditExtend.beginDate IS NULL;

//            IF EXISTS (
//                SELECT a.id
//                FROM auditExtend a
//                INNER JOIN temp_list_retry t ON a.id = t.id
//                WHERE a.status = 1 AND a.beginDate IS NOT NULL
//            ) THEN
//                INSERT INTO temp_list (id, hid, dbid, auditType)
//                SELECT id, hid, dbid, auditType FROM temp_list_retry;
//            END IF;
//        END IF;
//    END IF;

//    -- 把要发送的夜审都改成正在执行
//    UPDATE auditExtend
//    SET status = 1
//    WHERE id IN (SELECT id FROM temp_list);

//    -- 开始发送夜审扩展功能请求
//    FOR id, hid, type IN
//        SELECT id, hid, auditType FROM temp_list
//    LOOP
//        auditExtendUrl := 'http://pmsnotify.gshis.com/Audit/Index?hid=' || hid || '&type=' || type || '&id=' || id || '&sign=jxd598Audit';
//        RAISE NOTICE 'Audit Extend URL: %', auditExtendUrl;
//        PERFORM up_crsXml_httpSend_Asyn('', auditExtendUrl);
//    END LOOP;
//END;
//$$;";

//        Assert.Equal(expectedPostgreSqlScript, result);
//    }
//}
