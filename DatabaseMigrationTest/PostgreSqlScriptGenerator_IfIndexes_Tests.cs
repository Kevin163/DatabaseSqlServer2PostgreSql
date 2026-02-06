using DatabaseMigration.Migration;
using DatabaseMigration.ScriptGenerator;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigrationTest;

public class PostgreSqlScriptGenerator_IfIndexes_Tests
{
    [Fact]
    public void Convert_IfNotExistsIndex_CreateIndex()
    {
        var sql = @"if not exists(select * from sys.indexes where name = 'ix_HotelVoiceQtys')
begin
 create index ix_HotelVoiceQtys on HotelVoiceQtys(Hid,QtyType)
end";

        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);
        Assert.Empty(errors);

        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(fragment.ScriptTokenStream);

        // Expect the converter to create index if not exists in PostgreSQL style
        var expected = @"IF NOT EXISTS ( select * from pg_class where relname = 'ix_hotelvoiceqtys' and relkind = 'i' LIMIT 1) THEN

 create index ix_hotelvoiceqtys on hotelvoiceqtys(hid,qtytype);
 END IF;
";

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Convert_CreateNonClusteredIndex_ShouldRemoveNonClusteredKeyword()
    {
        var sql = @"CREATE NONCLUSTERED INDEX IX_Table_UnionID ON QuickVoiceUser (UnionID)";

        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);
        Assert.Empty(errors);

        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(fragment.ScriptTokenStream);

        // Expect NONCLUSTERED keyword to be removed for PostgreSQL
        var expected = @"CREATE INDEX ix_table_unionid ON quickvoiceuser (unionid);";

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Convert_IfNotExistsNonClusteredIndex_ShouldRemoveNonClusteredKeyword()
    {
        var sql = @"if not exists(select * from sys.indexes where name = 'IX_Table_UnionID')
begin
 CREATE NONCLUSTERED INDEX IX_Table_UnionID ON QuickVoiceUser (UnionID)
end";

        var parser = new TSql170Parser(true);
        var fragment = parser.Parse(new System.IO.StringReader(sql), out var errors);
        Assert.Empty(errors);

        var generator = new PostgreSqlProcedureScriptGenerator();
        var result = generator.GenerateSqlScript(fragment.ScriptTokenStream);

        // Expect NONCLUSTERED keyword to be removed for PostgreSQL
        var expected = @"IF NOT EXISTS ( select * from pg_class where relname = 'ix_table_unionid' and relkind = 'i' LIMIT 1) THEN 

 CREATE INDEX ix_table_unionid ON quickvoiceuser (unionid);
 END IF;
";

        Assert.Equal(expected, result);
    }
}
