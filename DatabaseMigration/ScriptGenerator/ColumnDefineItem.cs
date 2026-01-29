namespace DatabaseMigration.ScriptGenerator;

/// <summary>
/// 增加或修改列时的列定义项
/// </summary>
public struct ColumnDefineItem
{
    /// <summary>
    /// 列名称
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// 列的数据类型定义项
    /// </summary>
    public ColumnDataTypeDefineItem DataTypeDefine { get; set; }
}
/// <summary>
/// 列的数据类型定义项
/// </summary>
public struct ColumnDataTypeDefineItem
{
    public ColumnDataTypeDefineItem()
    {
        DataType = "";
        IsPrimaryKey = false;
        IsNullable = true;
        IsIdentity = false;
        DefaultValue = null;
    }
    /// <summary>
    /// 列数据类型
    /// </summary>
    public string DataType { get; set; }
    /// <summary>
    /// 列是否允许为空
    /// </summary>
    public bool IsNullable { get; set; }
    /// <summary>
    /// 列是否为主键
    /// </summary>
    public bool IsPrimaryKey { get; set; }
    /// <summary>
    /// 是否为自增列
    /// </summary>
    public bool IsIdentity { get; set; }
    /// <summary>
    /// 列的默认值，没有默认值则为 null
    /// </summary>
    public string? DefaultValue { get; set; }
}
