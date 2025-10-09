using DatabaseMigration.Migration;

namespace DatabaseMigrationTest
{
    /// <summary>
    /// MigrationUtils获取第一个SQL语句单元的测试类，专注于IF块场景。
    /// </summary>
    public class MigrationUtils_GetFirstSqlSentence_IfBlock_Test
    {

        /// <summary>
        /// 场景：IF NOT EXISTS (...) BEGIN ... END; 且后面还有其他 IF 块。
        /// 期望：应返回第一个 IF ... BEGIN ... END 块作为 firstSql。
        /// </summary>
        [Fact]
        public void GetFirstCompleteSqlSentence_FirstIfBeginEndBlock_ReturnsFirstIfBlock()
        {
            var sql = @"IF NOT EXISTS(SELECT * FROM syscolumns WHERE  id = OBJECT_ID('dbList') AND name = 'readonlyDbServer')

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

END";

            var (first, other) = MigrationUtils.GetFirstCompleteSqlSentence(sql);
            var expectedFirst = @"IF NOT EXISTS(SELECT * FROM syscolumns WHERE  id = OBJECT_ID('dbList') AND name = 'readonlyDbServer')

BEGIN

    ALTER TABLE dbList ADD readonlyDbServer VARCHAR(200)

    ALTER TABLE dbList ADD readonlyDbName VARCHAR(200)

    ALTER TABLE dbList ADD readonlyLogId VARCHAR(30)

    ALTER TABLE dbList ADD readonlyLogPwd VARCHAR(30)

END
";
            Assert.Equal(expectedFirst, (first ?? string.Empty));
            Assert.False(string.IsNullOrWhiteSpace(other));
            Assert.Contains("HotelPos", other);
        }
    }
}
