namespace DatabaseMigration.ScriptGenerator;

/// <summary>
/// 匹配指定的 TokenType 序列的结果
/// </summary>
public struct MatchTokenTypesSequenceResult
{
    private MatchTokenTypesSequenceResult(bool isMatch, List<string> outValues, int stopIndexOfToken, List<ColumnDefineItem> columnDefineItems)
    {
        IsMatch = isMatch;
        OutValues = outValues;
        StopIndexOfToken = stopIndexOfToken;
        OutColumnDefines = columnDefineItems;
    }
    /// <summary>
    /// 创建匹配成功的结果
    /// </summary>
    /// <param name="outValues">匹配成功的输出结果序列</param>
    /// <param name="stopIndexOfToken">停止匹配时的 Token 索引</param>
    /// <returns></returns>
    public static MatchTokenTypesSequenceResult CreateSuccess(List<string> outValues, int stopIndexOfToken, List<ColumnDefineItem> columnDefineItems)
    {
        return new MatchTokenTypesSequenceResult(true, outValues, stopIndexOfToken, columnDefineItems);
    }
    /// <summary>
    /// 创建匹配失败的结果
    /// </summary>
    /// <param name="stopIndexOfToken">停止匹配时的 Token 索引</param>
    /// <returns></returns>
    public static MatchTokenTypesSequenceResult CreateFail(int stopIndexOfToken = 0)
    {
        return new MatchTokenTypesSequenceResult(false, new List<string>(), stopIndexOfToken, new List<ColumnDefineItem>());
    }
    /// <summary>
    /// 是否匹配成功
    /// </summary>
    public bool IsMatch { get; private set; }
    /// <summary>
    /// 输出值列表,目前包含outValue和outIdentifier两种输出值
    /// </summary>
    public List<string> OutValues { get; private set; }
    /// <summary>
    /// 输出列定义列表，对应输出类型为 ColumnDefine 时使用
    /// </summary>
    public List<ColumnDefineItem> OutColumnDefines { get; private set; }
    /// <summary>
    /// 停止匹配时的 Token 索引
    /// </summary>
    public int StopIndexOfToken { get; private set; }
}
