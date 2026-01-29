using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace DatabaseMigration.Migration;

/// <summary>
/// 提供从 SQL Server 到 PostgreSQL 的迁移转换工具方法集合。
/// 包含数据类型映射、对象定义抓取（保留原始换行）以及视图定义从 T-SQL 到 PostgreSQL 的基础语法转换。
/// </summary>
public static class MigrationUtils
{
    /// <summary>
    /// sp_helptext 每行返回的最大长度（含行尾），超过该长度的行会被截断。
    /// </summary>
    private const int RowLengthForSpHelpText = 255;
    /// <summary>
    /// 将 SQL Server 的列数据类型映射为 PostgreSQL 的兼容类型字符串。
    /// </summary>
    /// <param name="sqlServerType">SQL Server 的类型名称（来自 INFORMATION_SCHEMA.COLUMNS.DATA_TYPE）。</param>
    /// <param name="column">列的架构行（来自 GetSchema("Columns") 或 INFORMATION_SCHEMA），用于读取长度/精度/标度等附加信息。</param>
    /// <returns>PostgreSQL 的类型声明（可能包含长度/精度/标度）。</returns>
    /// <remarks>
    /// - varchar/nvarchar 当 CHARACTER_MAXIMUM_LENGTH 为 -1（即 SQL Server 的 MAX）时，将映射为 PostgreSQL 的 text。
    /// - datetime/datetime2/smalldatetime 映射为 timestamp；datetimeoffset 映射为 timestamptz。
    /// - binary/varbinary/image/timestamp 映射为 bytea。
    /// - tinyint 映射为 smallint。
    /// - 未识别类型将回退为 text。
    /// </remarks>
    public static string ConvertToPostgresType(string sqlServerType, DataRow column)
    {
        if (string.IsNullOrWhiteSpace(sqlServerType)) return "text";

        // 如果传入的类型包含括号参数（例如 varchar(200) 或 decimal(10,2)），优先解析并直接返回对应类型
        var typeText = sqlServerType.Trim();
        var match = Regex.Match(typeText, "^([a-zA-Z0-9_]+)\\s*\\(\\s*(.+)\\s*\\)$");
        if (match.Success)
        {
            var baseType = match.Groups[1].Value.ToLower();
            var inner = match.Groups[2].Value;
            switch (baseType)
            {
                case "varchar":
                case "nvarchar":
                    // 只取第一个参数（长度），忽略后续可能的无关参数
                    var lenPart = inner.Split(',')[0].Trim();
                    if (int.TryParse(lenPart, out var parsedLen))
                    {
                        if (parsedLen > 0) return $"varchar({parsedLen})";
                        return "text"; // SQL Server MAX 或类似表示
                    }
                    break;
                case "char":
                case "nchar":
                    if (int.TryParse(inner.Trim(), out var cLen))
                        return $"char({cLen})";
                    break;
                case "decimal":
                case "numeric":
                    var parts = inner.Split(',');
                    if (parts.Length >= 2 && int.TryParse(parts[0].Trim(), out var p) && int.TryParse(parts[1].Trim(), out var s))
                        return $"numeric({p}, {s})";
                    break;
                default:
                    // 对于其他带参数的类型，保留原始处理逻辑（会在后续 switch 中降级或处理）
                    break;
            }
        }

        switch (sqlServerType.ToLower())
        {
            case "bigint": return "bigint";
            case "binary":
            case "varbinary":
            case "image":
            case "timestamp": return "bytea";
            case "bit": return "boolean";
            case "char":
            case "nchar":
                if (column != null && column["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value)
                    return $"char({column["CHARACTER_MAXIMUM_LENGTH"]})";
                return "char";
            case "varchar":
            case "nvarchar":
                if (column != null && column["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value)
                {
                    int maxLen = Convert.ToInt32(column["CHARACTER_MAXIMUM_LENGTH"]);
                    if (maxLen > 0)
                        return $"varchar({maxLen})";
                    else
                        return "text";
                }
                return "varchar";
            case "text":
            case "ntext":
            case "xml":
            case "sql_variant": return "text";
            case "date": return "date";
            case "datetime":
            case "datetime2":
            case "smalldatetime": return "timestamp";
            case "datetimeoffset": return "timestamptz";
            case "decimal":
            case "numeric":
                if (column != null && column["NUMERIC_PRECISION"] != DBNull.Value && column["NUMERIC_SCALE"] != DBNull.Value)
                    return $"numeric({column["NUMERIC_PRECISION"]}, {column["NUMERIC_SCALE"]})";
                return "numeric";
            case "float": return "double precision";
            case "int": return "integer";
            case "money":
            case "smallmoney": return "money";
            case "real": return "real";
            case "smallint":
            case "tinyint": return "smallint";
            case "time": return "time";
            case "uniqueidentifier": return "uuid";
            default: return "text";
        }
    }

    /// <summary>
    /// 将 SQL Server 的函数定义脚本转换为 PostgreSQL 的函数定义脚本。
    /// </summary>
    /// <param name="sqlServerFunction"></param>
    /// <returns></returns>
    public static string ConvertToPostgresFunction(string sqlServerFunction)
    {
        if (sqlServerFunction.Equals("getdate()", StringComparison.OrdinalIgnoreCase))
        {
            return "CURRENT_TIMESTAMP";
        }
        return sqlServerFunction;
    }
    /// <summary>
    /// 获取指定对象（视图/存储过程/函数/触发器等）的 T-SQL 定义脚本（保留原始换行）。
    /// </summary>
    /// <param name="connection">已打开的 SQL Server 连接。</param>
    /// <param name="objectName">对象名称，可以是不带架构名的简单名（将尝试 dbo.{name} 兜底）或带架构的完全限定名（如 dbo.ViewName）。</param>
    /// <returns>
    /// 对象的定义脚本文本（包含原始换行与格式）；若对象不存在、无权限或不可脚本化（例如被加密）则返回空字符串。
    /// </returns>
    /// <remarks>
    /// 本方法仅使用 <c>sp_helptext</c> 获取定义，以确保尽最大可能保留原始换行和格式，便于后续“逐行”处理。
    /// </remarks>
    public static string GetObjectDefinition(SqlConnection connection, string objectName)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));
        if (string.IsNullOrWhiteSpace(objectName)) return string.Empty;

        // 先按传入名尝试
        var text = TrySpHelpText(connection, objectName);
        if (!string.IsNullOrEmpty(text)) return text;

        // 未带架构名则再尝试 dbo.{name}
        if (!objectName.Contains("."))
        {
            text = TrySpHelpText(connection, $"dbo.{objectName}");
            if (!string.IsNullOrEmpty(text)) return text;
        }

        return string.Empty;
    }
    /// <summary>
    /// 执行sp_helptext来获取对象定义，同时处理一行被sp_helptext截断的情况
    /// 由于sp_helptext返回的每行都会带有\r\n，为了方便，全部都替换为空后再进行拼接，由程序判断是否合并行和换行
    /// </summary>
    /// <param name="connection">已打开的 SQL Server 连接。</param>
    /// <param name="name">对象名称</param>
    /// <returns>对象的定义脚本文本（包含原始换行与格式）；若对象不存在、无权限或不可脚本化（例如被加密）则返回空字符串</returns>
    private static string TrySpHelpText(SqlConnection connection, string name)
    {
        var sb = new StringBuilder();
        try
        {
            var isSplitedLine = false; // 标志上一行是否被截断
            using var cmd = new SqlCommand("sp_helptext", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@objname", name);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var line = reader.GetString(0);
                line = line.TrimEnd('\r', '\n'); // 先将尾部的\r\n统一去掉，方便后续处理
                // 处理sp_helptext返回的行被截断的情况（超过255字符的行会被截断）
                if (isSplitedLine)
                {
                    //如果上一行被截断，则需要判断当前行是否需要拼接
                    //目前的规则是，如果当前行是以union开头，或是以--开头，则认为是不需要和上一行合并的
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        var noStartSpace = line.TrimStart();
                        if (noStartSpace.StartsWith("union", StringComparison.OrdinalIgnoreCase) || noStartSpace.StartsWith("--"))
                        {
                            //不需要拼接的话，直接换行
                            sb.AppendLine();
                        }
                    }
                }
                // 如果当前行的长度等于255，则先拼接内容，但不换行，并且设置一个标志表示下一行是续行
                if (!string.IsNullOrWhiteSpace(line) && line.Length == RowLengthForSpHelpText)
                {
                    isSplitedLine = true;
                    sb.Append(line);
                }
                else
                {
                    isSplitedLine = false;
                    sb.AppendLine(line);
                }
            }
        }
        catch
        {
            // 忽略异常（对象不存在/无权限/加密等），返回空字符串
        }
        return sb.ToString();
    }
}