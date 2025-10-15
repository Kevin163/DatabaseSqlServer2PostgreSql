using DatabaseMigration.ScriptGenerator;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigrationTest;

/// <summary>
/// List&lt;TSqlParserToken&gt;的扩展方法测试类
/// </summary>
public class ListTSqlParserTokenExtensionTests
{
    /// <summary>
    /// 验证场景：全部是空白Token
    /// </summary>
    [Fact]
    public void GetFirstNotWhiteSpaceTokenType_AllWhitespace_ReturnsWhiteSpace()
    {
        var tokens = new List<TSqlParserToken>
        {
            new TSqlParserToken(TSqlTokenType.WhiteSpace, " "),
            new TSqlParserToken(TSqlTokenType.WhiteSpace, "\t"),
        };

        var result = tokens.GetFirstNotWhiteSpaceTokenType();

        Assert.Equal(TSqlTokenType.WhiteSpace, result);
    }
    /// <summary>
    /// 验证场景：第一个非空白Token
    /// </summary>
    [Fact]
    public void GetFirstNotWhiteSpaceTokenType_FirstNonWhitespace_ReturnsThatType()
    {
        var tokens = new List<TSqlParserToken>
        {
            new TSqlParserToken(TSqlTokenType.WhiteSpace, " "),
            new TSqlParserToken(TSqlTokenType.Identifier, "abc"),
            new TSqlParserToken(TSqlTokenType.SingleLineComment, "-- comment"),
        };

        var result = tokens.GetFirstNotWhiteSpaceTokenType();

        Assert.Equal(TSqlTokenType.Identifier, result);
    }
    /// <summary>
    /// 验证场景：空列表
    /// </summary>
    [Fact]
    public void GetFirstNotWhiteSpaceTokenType_EmptyList_ReturnsWhiteSpace()
    {
        var tokens = new List<TSqlParserToken>();

        var result = tokens.GetFirstNotWhiteSpaceTokenType();

        Assert.Equal(TSqlTokenType.WhiteSpace, result);
    }
}
