namespace DatabaseMigration.ScriptGenerator;


/// <summary>
/// Declare定义变量
/// </summary>
public struct DeclareItem
{
    /// <summary>
    /// 变量名称
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// 变量数据类型
    /// </summary>
    public string TypeText { get; set; }
}
