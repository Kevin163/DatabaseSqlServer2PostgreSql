namespace DatabaseMigration.ScriptGenerator;

/// <summary>
/// 语句转换后的标识符类型，用于指示标识符是表、列、视图等类型
/// </summary>
public enum TokenItemIdentifierType
{
    /// <summary>
    /// 表名称
    /// </summary>
    TableName,
    /// <summary>
    /// 列名称
    /// </summary>
    ColumnName,
    /// <summary>
    /// 数据类型名称
    /// </summary>
    DataTypeName,
}
