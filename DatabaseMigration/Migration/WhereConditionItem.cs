namespace DatabaseMigration.Migration
{
    /// <summary>
    /// where 条件项
    /// </summary>
    public class WhereConditionItem
    {
        /// <summary>
        /// 列名
        /// </summary>
        public string ColumnName { get; set; } = string.Empty;
        /// <summary>
        /// 条件操作符，如 =, <>, IS, IS NOT 等
        /// </summary>
        public WhereConditionOperator Operator { get; set; }
        /// <summary>
        /// 值
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }
    /// <summary>
    /// Where 条件操作符
    /// </summary>
    public enum WhereConditionOperator
    {
        Equal,
    }
}
