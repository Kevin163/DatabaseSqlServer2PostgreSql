using DatabaseMigration.Migration;
using DatabaseMigration.ScriptGenerator;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigrationTest
{
    /// <summary>
    /// 测试 MigrationUtils 与 IfConditionUtils 相关功能的单元测试集合。
    /// 每个测试包含简短注释说明被验证的行为。
    /// </summary>
    public class TSqlFragmentExtension_GetIfConditionSql_Test
    {
        /// <summary>
        /// 验证当存在 IF ... BEGIN ... END 块时，
        /// MigrationUtils.GetIfConditionSql 能正确分离出 IF 条件行（cond）和随后的其他内容（other）。
        /// 断言：
        ///  - cond 为预期的 IF 条件
        ///  - other 不为空且包含 BEGIN、ALTER TABLE、END 等关键内容
        /// </summary>
        [Fact]
        public void GetIfConditionSql_BasicIfBeginEnd_ReturnsConditionAndOther()
        {
            var sql = @"IF NOT EXISTS(SELECT * FROM syscolumns WHERE ID = OBJECT_ID('HotelPos') AND name = 'Id')

BEGIN

    ALTER TABLE HotelPos ADD ID UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()

    ALTER TABLE HotelPos DROP CONSTRAINT pk_hotelPos

    ALTER TABLE hotelPos ADD CONSTRAINT pk_hotelPos PRIMARY KEY(ID)

END";

            var parser = new TSql170Parser(true);
            var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);   

            Assert.Empty(errors);

            int startIndex = 0;
            var tokens = fragment.GetIfConditionOnly(ref startIndex);
            var expectedCond = "IF NOT EXISTS(SELECT * FROM syscolumns WHERE ID = OBJECT_ID('HotelPos') AND name = 'Id')\r\n\r\n";

            Assert.NotEmpty(tokens);
            Assert.Equal(expectedCond, string.Concat(tokens.Select(w=>w.Text)));
            Assert.Equal(tokens.Count, startIndex); // 索引应指向下一个语句的起始位置
        }

        /// <summary>
        /// 验证当输入不包含 IF 时，GetIfConditionSql 返回空的 cond 且 other 为原始字符串。
        /// </summary>
        [Fact]
        public void GetIfConditionSql_NoIf_ReturnsEmptyAndOriginal()
        {
            var sql = "SELECT 1;\nSELECT 2;";
            var parser = new TSql170Parser(true);
            var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);

            Assert.Empty(errors);

            int startIndex = 0;
            var tokens = fragment.GetIfConditionOnly(ref startIndex);

            Assert.Empty(tokens);
            Assert.Equal(0, startIndex); // 索引应保持不变
        }

        /// <summary>
        /// 验证 IfConditionUtils.TryParseNotExistsSysColumnsCondition 能识别类似
        /// IF NOT EXISTS(SELECT * FROM syscolumns ... ) 这种检查 syscolumns 的条件语句。
        /// </summary>
        [Fact]
        public void GetIfConditionSql_IfWithoutBeginEnd_ReturnsConditionAndOther()
        {
            var sql = @"IF NOT EXISTS(SELECT * FROM syscolumns WHERE ID = OBJECT_ID('HotelPos') AND name = 'Id')";

            var actual = IfConditionUtils.TryParseNotExistsSysColumnsCondition(sql, out var table, out var column);

            Assert.True(actual);
            Assert.Equal("HotelPos", table);
            Assert.Equal("Id", column);
        }

        /// <summary>
        /// 验证 IfConditionUtils.TryParseNotExistsSysColumnsCondition 能识别 SELECT 1 FROM syscolumns 这类变体并解析出表名与列名。
        /// </summary>
        [Fact]
        public void TryParseNotExistsSysColumnsCondition_Select1Form_ReturnsTableAndColumn()
        {
            var sql = "if(not exists(select 1 from syscolumns where id=object_id('helpFiles') and name='language')) ";
            var ok = IfConditionUtils.TryParseNotExistsSysColumnsCondition(sql, out var table, out var column);
            Assert.True(ok);
            Assert.Equal("helpFiles", table);
            Assert.Equal("language", column);
        }

        /// <summary>
        /// 验证从 syscolumns 类型的 IF 条件中正确解析出表名和列名。
        /// </summary>
        [Fact]
        public void GetSysColumnsTableNameAndColumnName_ValidInput_ReturnsTableAndColumn()
        {
            var ifConditionSql = "IF NOT EXISTS(SELECT * FROM syscolumns WHERE ID = OBJECT_ID('HotelPos') AND name = 'Id')";
            var parsed = IfConditionUtils.TryParseNotExistsSysColumnsCondition(ifConditionSql, out var table, out var column);
            Assert.True(parsed);
            Assert.Equal("HotelPos", table);
            Assert.Equal("Id", column);
        }

        /// <summary>
        /// 验证 IfConditionUtils.GetSqlsInBeginAndEnd 的行为：
        /// 1) 当 SQL 被最外层的 BEGIN/END 包裹（且之后没有其它有效代码）时，返回剥离后的内部语句；
        /// 2) 当 SQL 无最外层 BEGIN/END 包裹时，直接返回原始 SQL。
        /// </summary>
        [Fact]
        public void GetSqlsInBeginAndEnd_BeginEndWrapped_ReturnsInnerSql()
        {
            // 示例：被 BEGIN/END 包裹的块（包含空行与缩进）
            var wrapped = @"
BEGIN

    -- inline comment
    ALTER TABLE dbo.MyTable ADD NewCol VARCHAR(50) NOT NULL DEFAULT 'x'

END

-- trailing comment";

            var inner = IfConditionUtils.GetSqlsInBeginAndEnd(wrapped);

            // 内部应包含 ALTER TABLE 的语句，并且不应包含 BEGIN/END
            Assert.Contains("ALTER TABLE dbo.MyTable ADD NewCol", inner);
            Assert.DoesNotContain("BEGIN", inner, System.StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("END", inner, System.StringComparison.OrdinalIgnoreCase);

            // 未被包裹的 SQL 应直接返回原文
            var notWrapped = "ALTER TABLE dbo.MyTable ADD NewCol VARCHAR(50)";
            var result = IfConditionUtils.GetSqlsInBeginAndEnd(notWrapped);
            Assert.Equal(notWrapped, result);
        }

        /// <summary>
        /// 验证 IfConditionUtils.GetSqlsInBeginAndEnd 在遇到嵌套的 BEGIN/END 时行为正确：
        /// - 外层 BEGIN/END 被剥离
        /// - 内层的嵌套 BEGIN/END 保留并作为内部内容返回
        /// </summary>
        [Fact]
        public void GetSqlsInBeginAndEnd_NestedBeginEnd_ReturnsInnerWithNestedPreserved()
        {
            var wrappedNested = @"BEGIN
    ALTER TABLE A ADD Col1 INT
    BEGIN
        EXEC dbo.SomeProc
    END
    ALTER TABLE A ADD Col2 INT
END";

            var inner = IfConditionUtils.GetSqlsInBeginAndEnd(wrappedNested);

            // 外层 BEGIN/END 应被移除
            Assert.False(inner.TrimStart().StartsWith("BEGIN", System.StringComparison.OrdinalIgnoreCase));
            Assert.False(inner.TrimEnd().EndsWith("END", System.StringComparison.OrdinalIgnoreCase));

            // 嵌套的 BEGIN/END 应当保留
            Assert.Contains("BEGIN", inner);
            Assert.Contains("END", inner);
            Assert.Contains("ALTER TABLE A ADD Col1", inner);
            Assert.Contains("EXEC dbo.SomeProc", inner);
            Assert.Contains("ALTER TABLE A ADD Col2", inner);
        }

        /// <summary>
        /// 验证 TryParseIsObjectIdNullCondition 能正确识别 IF OBJECT_ID('Table') IS NULL 的情况，
        /// 并能从带 schema 或不带 schema 的 OBJECT_ID 提取未限定表名；对非匹配语句返回 false。
        /// </summary>
        [Fact]
        public void TryParseIsObjectIdNullCondition_ValidAndSchemaAndInvalidCases()
        {
            // 简单表名
            var s1 = "IF OBJECT_ID('HuiYiMapping') IS NULL";
            var ok1 = IfConditionUtils.TryParseIsObjectIdNullCondition(s1, out var table1);
            Assert.True(ok1);
            Assert.Equal("HuiYiMapping", table1);

            // 带 schema 的表名
            var s2 = "IF OBJECT_ID('dbo.HuiYiMapping') IS NULL";
            var ok2 = IfConditionUtils.TryParseIsObjectIdNullCondition(s2, out var table2);
            Assert.True(ok2);
            Assert.Equal("dbo.HuiYiMapping", table2);

            // 小写且带括号的形式
            var s4 = "if(object_id('versionParas') is null)";
            var ok4 = IfConditionUtils.TryParseIsObjectIdNullCondition(s4, out var table4);
            Assert.True(ok4);
            Assert.Equal("versionParas", table4);

            // 带外层空格与圆括号的形式
            var s5 = "  IF ( OBJECT_ID('X') IS NULL );";
            var ok5 = IfConditionUtils.TryParseIsObjectIdNullCondition(s5, out var table5);
            Assert.True(ok5);
            Assert.Equal("X", table5);
        }

        /// <summary>
        /// 新增单元测试：验证 TryParseSelectFromTableWhenWhereOneEqualCondition 能正确解析简单的
        /// IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'TryHotelIdForGroup') 形式，
        /// 并提取表名、列名和值（支持带 N 前缀、方括号、引号等常见变体）。
        /// </summary>
        [Fact]
        public void TryParseSelectFromTableWhenWhereOneEqualCondition_BasicAndVariants()
        {
            // 基本形式
            var s1 = "IF NOT EXISTS(SELECT * FROM sysPara WHERE code = 'TryHotelIdForGroup')";
            var ok1 = IfConditionUtils.TryParseSelectFromTableWhenWhereOneEqualCondition(s1, out var table1, out var where1);
            Assert.True(ok1);
            Assert.Equal("sysPara", table1);
            Assert.NotNull(where1);
            Assert.Equal("code", where1!.ColumnName);
            Assert.Equal(WhereConditionOperator.Equal, where1.Operator);
            Assert.Equal("TryHotelIdForGroup", where1.Value);

            // 带 N 前缀和值用双引号
            var s2 = "IF NOT EXISTS(SELECT * FROM [sysPara] WHERE [code] = N'ValueWithN')";
            var ok2 = IfConditionUtils.TryParseSelectFromTableWhenWhereOneEqualCondition(s2, out var table2, out var where2);
            Assert.True(ok2);
            Assert.Equal("sysPara", table2);
            Assert.NotNull(where2);
            Assert.Equal("code", where2!.ColumnName);
            Assert.Equal(WhereConditionOperator.Equal, where2.Operator);
            Assert.Equal("ValueWithN", where2.Value);

            // 带分号和空白换行
            var s3 = "  IF NOT EXISTS(  SELECT * FROM dbo.sysPara  WHERE  dbo.sysPara.code = \"abc\"  );";
            var ok3 = IfConditionUtils.TryParseSelectFromTableWhenWhereOneEqualCondition(s3, out var table3, out var where3);
            Assert.True(ok3);
            Assert.Equal("sysPara", table3);
            Assert.NotNull(where3);
            Assert.Equal("dbo.sysPara.code", where3!.ColumnName);
            Assert.Equal("abc", where3.Value);

            // 紧挨括号的形式以及小写变体
            var s4 = "IF(NOT EXISTS(SELECT 1 FROM sysPara WHERE code = 'ISPAWeiXinTemplateIDQuitSelect'))";
            var ok4 = IfConditionUtils.TryParseSelectFromTableWhenWhereOneEqualCondition(s4, out var table4, out var where4);
            Assert.True(ok4);
            Assert.Equal("sysPara", table4);
            Assert.NotNull(where4);
            Assert.Equal("code", where4!.ColumnName);
            Assert.Equal("ISPAWeiXinTemplateIDQuitSelect", where4.Value);

            var s5 = "if(not exists(select 1 from sysPara where code='lowercase'))";
            var ok5 = IfConditionUtils.TryParseSelectFromTableWhenWhereOneEqualCondition(s5, out var table5, out var where5);
            Assert.True(ok5);
            Assert.Equal("sysPara", table5);
            Assert.NotNull(where5);
            Assert.Equal("code", where5!.ColumnName);
            Assert.Equal("lowercase", where5.Value);
        }

        /// <summary>
        /// 新增单元测试：验证 TryParseNotExistsSelectFromSysObjectsCondition 能正确解析
        /// IF NOT EXISTS(SELECT * FROM sysobjects WHERE name = '...') 这类语句，支持 N 前缀和双引号/分号变体。
        /// </summary>
        [Fact]
        public void TryParseNotExistsSelectFromSysObjectsCondition_BasicAndVariants()
        {
            var s1 = "IF NOT EXISTS(SELECT * FROM sysobjects WHERE name = 'ImeiMappingHid')";
            var ok1 = IfConditionUtils.TryParseNotExistsSelectFromSysObjectsCondition(s1, out var obj1);
            Assert.True(ok1);
            Assert.Equal("ImeiMappingHid", obj1);

            var s2 = "IF NOT EXISTS(SELECT * FROM sysobjects WHERE name = N'ImeiMappingHid')";
            var ok2 = IfConditionUtils.TryParseNotExistsSelectFromSysObjectsCondition(s2, out var obj2);
            Assert.True(ok2);
            Assert.Equal("ImeiMappingHid", obj2);

            var s3 = "IF NOT EXISTS(SELECT * FROM sysobjects WHERE name = \"SomeObject\");";
            var ok3 = IfConditionUtils.TryParseNotExistsSelectFromSysObjectsCondition(s3, out var obj3);
            Assert.True(ok3);
            Assert.Equal("SomeObject", obj3);

            var s4 = "  IF NOT EXISTS ( SELECT * FROM sysobjects WHERE name = 'X' );";
            var ok4 = IfConditionUtils.TryParseNotExistsSelectFromSysObjectsCondition(s4, out var obj4);
            Assert.True(ok4);
            Assert.Equal("X", obj4);
        }

        /// <summary>
        /// 新增单元测试：验证 TryParseNotExistsSelectFromSysIndexCondition 能正确解析
        /// 含有内层 SELECT TOP 1 FROM sys.indexes 的语句，并提取出表名和索引名。
        /// </summary>
        [Fact]
        public void TryParseNotExistsSelectFromSysIndexCondition_ExtractsTableAndIndexName()
        {
            var s1 = "IF NOT EXISTS( SELECT * from sysobjects where name =( SELECT TOP 1 name FROM sys.indexes  WHERE is_primary_key = 1   AND object_id  = Object_Id('posSmMappingHid') AND name='PK_posSm_20190808912' ) )";
            var ok1 = IfConditionUtils.TryParseNotExistsSelectFromSysIndexCondition(s1, out var table1, out var index1);
            Assert.True(ok1);
            Assert.Equal("posSmMappingHid", table1);
            Assert.Equal("PK_posSm_20190808912", index1);

            // 变体：多余空格与 N 前缀（对索引名），并带分号
            var s2 = "  IF NOT EXISTS(SELECT * FROM sysobjects WHERE name = (SELECT TOP 1 name FROM sys.indexes WHERE is_primary_key=1 AND object_id=OBJECT_ID('dbo.posSmMappingHid') AND name = N'PK_posSm_20190808912'));";
            var ok2 = IfConditionUtils.TryParseNotExistsSelectFromSysIndexCondition(s2, out var table2, out var index2);
            Assert.True(ok2);
            Assert.Equal("posSmMappingHid", table2);
            Assert.Equal("PK_posSm_20190808912", index2);
        }

        /// <summary>
        /// 新增单元测试：验证 TryParseNotExistsInformationSchemaColumnsCondition 能正确解析
        /// 从 INFORMATION_SCHEMA.columns 查询列存在性的语句，并提取出表名和列名。
        /// </summary>
        [Fact]
        public void TryParseNotExistsInformationSchemaColumnsCondition_ExtractsTableAndColumn()
        {
            var s1 = "if not exists(select * from INFORMATION_SCHEMA.columns where table_name='posSmMappingHid' and column_name = 'memberVersion')";
            var ok1 = IfConditionUtils.TryParseNotExistsInformationSchemaColumnsCondition(s1, out var table1, out var column1);
            Assert.True(ok1);
            Assert.Equal("posSmMappingHid", table1);
            Assert.Equal("memberVersion", column1);

            // 变体：列顺序不同、双引号和分号
            var s2 = " IF NOT EXISTS ( SELECT * FROM information_schema.columns WHERE column_name = \"memberVersion\" AND table_name = N'posSmMappingHid' );";
            var ok2 = IfConditionUtils.TryParseNotExistsInformationSchemaColumnsCondition(s2, out var table2, out var column2);
            Assert.True(ok2);
            Assert.Equal("posSmMappingHid", table2);
            Assert.Equal("memberVersion", column2);
        }

        /// <summary>
        /// 新增单元测试：验证 TryParseExistsSysColumnsCondition 能正确解析
        /// IF EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('HotelUserWxInfo') AND name = 'NickName' AND length = 28)
        /// 并提取出表名、列名与长度。
        /// </summary>
        [Fact]
        public void TryParseExistsSysColumnsCondition_ExtractsTableColumnAndLength()
        {
            var s1 = "IF EXISTS(SELECT * FROM syscolumns WHERE id=OBJECT_ID('HotelUserWxInfo') AND name = 'NickName' AND length = 28)";
            var ok1 = IfConditionUtils.TryParseExistsSysColumnsCondition(s1, out var table1, out var col1, out var len1);
            Assert.True(ok1);
            Assert.Equal("HotelUserWxInfo", table1);
            Assert.Equal("NickName", col1);
            Assert.Equal(28, len1);

            // 变体：带括号与多余空格，N 前缀和分号
            var s2 = "  IF ( EXISTS ( SELECT 1 FROM syscolumns WHERE id = OBJECT_ID('dbo.HotelUserWxInfo') AND name = N'NickName' AND length=28 ) );";
            var ok2 = IfConditionUtils.TryParseExistsSysColumnsCondition(s2, out var table2, out var col2, out var len2);
            Assert.True(ok2);
            Assert.Equal("HotelUserWxInfo", table2);
            Assert.Equal("NickName", col2);
            Assert.Equal(28, len2);
        }

        /// <summary>
        /// 新增单元测试：验证 TryParseNotExistsSelectFromAuthButtonsCondition 能正确解析
        /// IF NOT EXISTS (SELECT * FROM AuthButtons WHERE AuthButtonId='SetHotelLevel' AND AuthButtonValue='524288' AND Seqid='101')
        /// 并提取出 buttonId、buttonValue 与 seqid。
        /// </summary>
        [Fact]
        public void TryParseNotExistsSelectFromAuthButtonsCondition_ExtractsFields()
        {
            var s1 = "IF NOT EXISTS (SELECT * FROM AuthButtons WHERE AuthButtonId='SetHotelLevel' AND AuthButtonValue='524288' AND Seqid='101')";
            var ok1 = IfConditionUtils.TryParseNotExistsSelectFromAuthButtonsCondition(s1, out var buttonId1, out var buttonValue1, out var seqid1);
            Assert.True(ok1);
            Assert.Equal("SetHotelLevel", buttonId1);
            Assert.Equal("524288", buttonValue1);
            Assert.Equal("101", seqid1);

            // 变体：顺序不同，N 前缀和双引号
            var s2 = " IF NOT EXISTS(SELECT * FROM AuthButtons WHERE Seqid = N'101' AND AuthButtonValue = \"524288\" AND AuthButtonId = N'SetHotelLevel');";
            var ok2 = IfConditionUtils.TryParseNotExistsSelectFromAuthButtonsCondition(s2, out var buttonId2, out var buttonValue2, out var seqid2);
            Assert.True(ok2);
            Assert.Equal("SetHotelLevel", buttonId2);
            Assert.Equal("524288", buttonValue2);
            Assert.Equal("101", seqid2);
        }

        /// <summary>
        /// 新增单元测试：验证 TryParseNotExistsSelectFromAllObjectsCondition 能正确解析
        /// IF NOT EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'dbo.commonInvoiceInfo') AND type IN ('U'))
        /// 并提取出 dbo.commonInvoiceInfo
        /// </summary>
        [Fact]
        public void TryParseNotExistsSelectFromAllObjectsCondition_ExtractsTableName()
        {
            var s1 = "IF NOT EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'dbo.commonInvoiceInfo') AND type IN ('U'))";
            var ok1 = IfConditionUtils.TryParseNotExistsSelectFromAllObjectsCondition(s1, out var table1);
            Assert.True(ok1);
            Assert.Equal("dbo.commonInvoiceInfo", table1);

            var s2 = " IF NOT EXISTS(SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID('commonInvoiceInfo') AND type IN ('U') );";
            var ok2 = IfConditionUtils.TryParseNotExistsSelectFromAllObjectsCondition(s2, out var table2);
            Assert.True(ok2);
            Assert.Equal("commonInvoiceInfo", table2);
        }

        /// <summary>
        /// 验证 MigrationUtils.GetIfConditionSql 能正确分离复杂的 IF EXISTS 包含子查询/UNION 的情况：
        /// - cond 为 IF ... ) 这一行
        /// - other 为随后的 BEGIN ... END 块
        /// </summary>
        [Fact]
        public void GetIfConditionSql_ComplexIfExistsWithSubquery_ReturnsConditionAndOther()
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
end  ";

            var (cond, other) = MigrationUtils.GetIfConditionSql(sql);

            var expectedCond = @"if exists(select distinct * from (  
    select hotelCode as hid from dbo.posSmMappingHid  
    union all  
    select groupid from dbo.posSmMappingHid)a  
    where ISNULL(a.hid,'')!='' and hid not in(select hid from dbo.hotelProducts where productCode='ipos'))  ";

            var expectedOther = @"begin  
    insert into hotelProducts(hid,productCode)  
    select distinct a.hid,'ipos' from (  
    select hotelCode as hid from dbo.posSmMappingHid  
    union all  
    select groupid from dbo.posSmMappingHid)a  
    where ISNULL(a.hid,'')!='' and hid not in(select hid from dbo.hotelProducts where productCode='ipos')  
end  ";

            Assert.Equal(expectedCond.NormalizeLineEndings(), cond.NormalizeLineEndings());
            Assert.Equal(expectedOther.NormalizeLineEndings(), other.NormalizeLineEndings());
        }

        /// <summary>
        /// 新增单元测试：验证 MigrationUtils.GetIfConditionSql 提取 CREATE TABLE 块中的 IF OBJECT_ID 条件。
        /// 确保在 BEGIN/END 包裹的情况下，条件仍然能够正确提取。
        /// </summary>
        [Fact]
        public void GetIfConditionSql_CreateTableBeginEnd_ReturnsCondition()
        {
            var sql = @"IF OBJECT_ID('HuiYiMapping') IS NULL  
BEGIN  
 CREATE TABLE HuiYiMapping(  
  [id] [uniqueidentifier] NOT NULL primary key,  
  [hid] [char](6),    
  [openId] [varchar](60)  
 )  
END  ";

            var (cond, other) = MigrationUtils.GetIfConditionSql(sql);

            var expectedCond = "IF OBJECT_ID('HuiYiMapping') IS NULL  ";

            Assert.Equal(expectedCond.NormalizeLineEndings(), cond.NormalizeLineEndings());
            Assert.False(string.IsNullOrWhiteSpace(other));
            Assert.Contains("CREATE TABLE HuiYiMapping", other, System.StringComparison.OrdinalIgnoreCase);
        }

        // Add unit tests for IfConditionUtils.IsExistsWithSubqueryFormat

        [Fact]
        public void IsExistsWithSubqueryFormat_PositiveAndNegativeCases()
        {
            var positive = @"if exists(select distinct * from (
    select hotelCode as hid from posSmMappingHid
    union all
    select groupid from posSmMappingHid)a
    where ISNULL(a.hid,'')!='' and hid not in(select hid from hotelProducts where productCode='ipos'))";

            Assert.True(IfConditionUtils.IsExistsPosSMMappingHidInHotelProductsWithSubqueryFormat(positive));

            // Missing posSmMappingHid
            var negative1 = @"if exists(select distinct * from (
    select hotelCode as hid from otherTable
    union all
    select groupid from otherTable)a
    where ISNULL(a.hid,'')!='' and hid not in(select hid from hotelProducts where productCode='ipos'))";
            Assert.False(IfConditionUtils.IsExistsPosSMMappingHidInHotelProductsWithSubqueryFormat(negative1));

            // Missing not in clause
            var negative2 = @"if exists(select distinct * from (
    select hotelCode as hid from posSmMappingHid
    union all
    select groupid from posSmMappingHid)a
    where ISNULL(a.hid,'')!='')";
            Assert.False(IfConditionUtils.IsExistsPosSMMappingHidInHotelProductsWithSubqueryFormat(negative2));

            // Different productCode value
            var negative3 = @"if exists(select distinct * from (
    select hotelCode as hid from posSmMappingHid
    union all
    select groupid from posSmMappingHid)a
    where ISNULL(a.hid,'')!='' and hid not in(select hid from hotelProducts where productCode='other'))";
            Assert.False(IfConditionUtils.IsExistsPosSMMappingHidInHotelProductsWithSubqueryFormat(negative3));
        }
    }
}
