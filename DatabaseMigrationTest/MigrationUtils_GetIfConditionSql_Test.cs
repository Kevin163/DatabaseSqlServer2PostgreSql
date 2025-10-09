using DatabaseMigration.Migration;

namespace DatabaseMigrationTest
{
    /// <summary>
    /// 测试 MigrationUtils 与 IfConditionUtils 相关功能的单元测试集合。
    /// 每个测试包含简短注释说明被验证的行为。
    /// </summary>
    public class MigrationUtils_GetIfConditionSql_Test
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

            var (cond, other) = MigrationUtils.GetIfConditionSql(sql);
            var expectedCond = "IF NOT EXISTS(SELECT * FROM syscolumns WHERE ID = OBJECT_ID('HotelPos') AND name = 'Id')";
            Assert.Equal(expectedCond, cond);
            Assert.False(string.IsNullOrWhiteSpace(other));
            Assert.Contains("BEGIN", other);
            Assert.Contains("ALTER TABLE HotelPos ADD ID UNIQUEIDENTIFIER", other);
            Assert.Contains("END", other);
        }

        /// <summary>
        /// 验证当输入不包含 IF 时，GetIfConditionSql 返回空的 cond 且 other 为原始字符串。
        /// </summary>
        [Fact]
        public void GetIfConditionSql_NoIf_ReturnsEmptyAndOriginal()
        {
            var sql = "SELECT 1;\nSELECT 2;";
            var (cond, other) = MigrationUtils.GetIfConditionSql(sql);
            Assert.Equal(string.Empty, cond);
            Assert.Equal(sql, other);
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

            // 非匹配语句
            var s3 = "IF EXISTS(SELECT 1 FROM sysobjects)";
            var ok3 = IfConditionUtils.TryParseIsObjectIdNullCondition(s3, out var table3);
            Assert.False(ok3);
            Assert.Equal(string.Empty, table3);
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
    }
}
