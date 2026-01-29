using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigration.ScriptGenerator;

/// <summary>
/// 用于表示TSqlTokenType的项，用于标识作用类型和对应的TokenType
/// </summary>
public struct TSqlTokenTypeItem
{
    public TSqlTokenTypeItem(TSqlTokenType tokenType, TSqlTokenTypeAction action = TSqlTokenTypeAction.Check,string value = ""): this(new List<TSqlTokenType> { tokenType }, action, value)
    {
    }
    public TSqlTokenTypeItem(List<TSqlTokenType> tokenTypes, TSqlTokenTypeAction action = TSqlTokenTypeAction.Check, string value = "")
    {
        Action = action;
        TokenTypes = tokenTypes;
        CheckValue = value;
    }
    /// <summary>
    /// 操作类型
    /// </summary>
    public TSqlTokenTypeAction Action { get; private set; }
    /// <summary>
    /// TSqlTokenType类型
    /// </summary>
    public List<TSqlTokenType> TokenTypes { get; private set; }
    /// <summary>
    /// 要检查的值，仅在Action为Check时有效，为空表示不检查具体值
    /// </summary>
    public string CheckValue { get; private set; }
}
/// <summary>
/// 需要对TSqlTokenType进行的操作类型
/// </summary>
public enum TSqlTokenTypeAction
{
    /// <summary>
    /// 检查标记
    /// </summary>
    Check,
    /// <summary>
    /// 获取其值并进行返回,直接返回值不做任何额外处理
    /// </summary>
    OutValue,
    /// <summary>
    /// 获取其标识符形式并进行返回，需要处理dbo.XXX为XXX
    /// </summary>
    OutIdentifier,
    /// <summary>
    /// 获取列定义，比如列名+数据类型+约束等完整定义
    /// </summary>
    OutColumnDefinition,
}
