using DatabaseMigration.ScriptGenerator;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Npgsql;
using System.Data;
using System.IO;
using System.Text;

namespace DatabaseMigration.Migration;

/// <summary>
/// 视图迁移器：将 SQL Server 视图转换为 PostgreSQL 视图
/// </summary>
public class ViewMigrator
{
    private readonly FileLoggerService _logger;

    public ViewMigrator(FileLoggerService logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 执行所有视图的迁移
    /// </summary>
    /// <param name="sourceConnection"></param>
    /// <param name="targetConnection"></param>
    public void Migrate(SqlConnection sourceConnection, NpgsqlConnection targetConnection)
    {
        _logger.Log("迁移视图依赖的函数...");
        ConvertFunction_Uf_Get_Mask(targetConnection);
        _logger.Log("视图依赖的函数迁移完成.");

        _logger.Log("开始迁移视图...");
        DataTable views = sourceConnection.GetSchema("Views");
        _logger.Log($"发现 {views.Rows.Count} 个视图需要迁移。");

        // 规范化视图名集合（不含架构名，假设无重名）
        var viewNames = new List<string>(views.Rows.Count);
        var viewNameSet = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (DataRow row in views.Rows)
        {
            string name = (string)row["TABLE_NAME"];
            //将视图名称也和表名称一样，转换为小写，因为postgresql是区分大小写的，如果名称中包含大写字母，而后续使用的时候，又没有使用双引号括起来，就会找不到视图
            name = name.ToLowerInvariant();
            if (viewNameSet.Add(name)) viewNames.Add(name);
        }

        // 确保 SQL Server 连接打开以读取依赖
        if (sourceConnection.State == ConnectionState.Closed)
            sourceConnection.Open();

        // 构建依赖图并拓扑排序
        var depGraph = GetViewDependencyGraph(sourceConnection, viewNameSet);
        var sorted = TopologicalSortViews(viewNames, depGraph);
        _logger.Log($"按依赖排序后迁移顺序：{string.Join(" -> ", sorted)}");

        // 确保目标库连接打开
        if (targetConnection.State == ConnectionState.Closed)
            targetConnection.Open();

        // 迭代重试（处理残余依赖、函数缺失等导致的暂时失败）
        var pending = new List<string>(sorted);
        var lastErrors = new Dictionary<string, Exception>(System.StringComparer.OrdinalIgnoreCase);
        int pass = 0;
        int maxPass = pending.Count + 5; // 上限，避免死循环

        while (pending.Count > 0 && pass < maxPass)
        {
            pass++;
            _logger.Log($"迁移视图第 {pass} 轮，剩余 {pending.Count} 个待迁移视图。");

            var nextPending = new List<string>();
            foreach (var viewName in pending)
            {
                string convertedDefinition = null;
                try
                {
                    // 拉取与转换定义
                    string viewDefinition = MigrationUtils.GetObjectDefinition(sourceConnection, viewName);
                    convertedDefinition = ConvertViewToPostgres(viewDefinition, viewName);

                    if (!string.IsNullOrWhiteSpace(convertedDefinition))
                    {
                        using (var cmd = new NpgsqlCommand(convertedDefinition, targetConnection))
                            cmd.ExecuteNonQuery();
                    }

                    _logger.Log($"视图 {viewName} 迁移成功.");
                    if (lastErrors.ContainsKey(viewName)) lastErrors.Remove(viewName);
                }
                catch (PostgresException pex)
                {
                    if (IsDependencyMissing(pex))
                    {
                        // 依赖未就绪，延后到下一轮
                        nextPending.Add(viewName);
                        _logger.Log($"延后视图 {viewName}（等待依赖）：{pex.SqlState} {pex.MessageText}");
                    }
                    else
                    {
                        // 记录非依赖类错误，稍后最终汇报
                        lastErrors[viewName] = pex;
                        _logger.Log($"视图 {viewName} 迁移错误（将稍后重试或最终报告）：{pex.SqlState} {pex.MessageText}");
                        // 可选择继续重试一次：这里直接加入下一轮，若无进展会在最终阶段报告
                        nextPending.Add(viewName);
                    }
                }
                catch (Exception ex)
                {
                    lastErrors[viewName] = ex;
                    _logger.Log($"视图 {viewName} 迁移异常（将稍后重试或最终报告）：{ex.Message}");
                    nextPending.Add(viewName);
                }
            }

            // 若无进展（上一轮和下一轮数量相同且集合相同），停止重试
            if (nextPending.Count == pending.Count)
            {
                bool same = true;
                var setA = new HashSet<string>(pending, System.StringComparer.OrdinalIgnoreCase);
                foreach (var n in nextPending) if (!setA.Contains(n)) { same = false; break; }
                if (same) break;
            }

            pending = nextPending;
        }

        // 最终报告未能迁移的视图
        if (pending.Count > 0)
        {
            foreach (var viewName in pending)
            {
                Exception ex = lastErrors.ContainsKey(viewName) ? lastErrors[viewName] : null;
                _logger.Log($"迁移视图 {viewName} 失败: {ex}");
                try
                {
                    string viewDefinition = MigrationUtils.GetObjectDefinition(sourceConnection, viewName);
                    string convertedDefinition = ConvertViewToPostgres(viewDefinition, viewName);
                    if (!string.IsNullOrEmpty(convertedDefinition))
                        _logger.Log($"出错的视图定义: {convertedDefinition}");
                }
                catch { /* 忽略日志阶段的异常 */ }
            }
        }

        _logger.Log("视图迁移完成.");
    }
    /// <summary>
    /// 核心方法，将 SQL Server 视图定义转换为 PostgreSQL 语法
    /// 使用 ScriptDom 首先尝试解析并生成规范 SQL，再做若干文本级别的替换以转换为 Postgres-friendly 语法。
    /// 若解析失败，则回退到原有的逐行处理逻辑（兼容旧行为）。
    /// </summary>
    private static string ConvertViewToPostgres(string viewDefinition, string viewName)
    {
        if (string.IsNullOrEmpty(viewDefinition))
            return string.Empty;

        var parser = new TSql170Parser(true);
        IList<ParseError> errors;
        using (var rdr = new StringReader(viewDefinition))
        {
            var fragment = parser.Parse(rdr, out errors);
            if (errors.Count > 0)
            {
                var errorMsgs = string.Join("; ", errors.Select(e => $"{Environment.NewLine}Line {e.Line}, Col {e.Column}: {e.Message}"));
                throw new Exception($"解析视图脚本错误：{errorMsgs}");
            }
            var convertedSql = new PostgreSqlViewScriptGenerator().GenerateSqlScript(fragment);
            var deleteSql = BuildConditionalDrop(viewName.ToPostgreSqlIdentifier());
            return $"{deleteSql}{Environment.NewLine}{convertedSql}";
        }
    }
    /// <summary>
    /// 依赖图：view -> 它依赖的视图集合（只保留本次迁移内的视图）
    /// </summary>
    private static Dictionary<string, HashSet<string>> GetViewDependencyGraph(SqlConnection conn, HashSet<string> knownViews)
    {
        var graph = new Dictionary<string, HashSet<string>>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var v in knownViews)
            graph[v] = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        const string sql = @"
SELECT v.name AS referencing_view, rv.name AS referenced_view
FROM sys.views v
JOIN sys.sql_expression_dependencies d ON d.referencing_id = v.object_id
JOIN sys.views rv ON rv.object_id = d.referenced_id
";
        using (var cmd = new SqlCommand(sql, conn))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                string from = reader.GetString(0);
                string to = reader.GetString(1);
                if (!knownViews.Contains(from) || !knownViews.Contains(to)) continue;
                graph[from].Add(to);
            }
        }
        return graph;
    }

    /// <summary>
    /// 拓扑排序：确保依赖在前。若存在环或未识别的依赖，保留原顺序附加在末尾
    /// </summary>
    private static List<string> TopologicalSortViews(List<string> views, Dictionary<string, HashSet<string>> deps)
    {
        var indeg = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
        var adj = new Dictionary<string, HashSet<string>>(System.StringComparer.OrdinalIgnoreCase);

        // 初始化
        for (int i = 0; i < views.Count; i++)
        {
            string v = views[i];
            indeg[v] = 0;
            adj[v] = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        }

        // 构造邻接表（dep -> list of views that depend on dep），并计算入度
        foreach (var kv in deps)
        {
            string view = kv.Key;
            foreach (var dep in kv.Value)
            {
                if (!indeg.ContainsKey(dep)) { indeg[dep] = 0; adj[dep] = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase); }
                if (!adj[dep].Contains(view))
                {
                    adj[dep].Add(view);
                    indeg[view] = indeg.ContainsKey(view) ? indeg[view] + 1 : 1;
                }
            }
        }

        // Kahn 算法
        var queue = new Queue<string>();
        foreach (var kv in indeg)
            if (kv.Value == 0) queue.Enqueue(kv.Key);

        var result = new List<string>(views.Count);
        while (queue.Count > 0)
        {
            var u = queue.Dequeue();
            result.Add(u);
            foreach (var v in adj[u])
            {
                indeg[v]--;
                if (indeg[v] == 0) queue.Enqueue(v);
            }
        }

        // 未被输出的（可能在环中或依赖缺失），按原始顺序附加
        if (result.Count < views.Count)
        {
            var inResult = new HashSet<string>(result, System.StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < views.Count; i++)
                if (!inResult.Contains(views[i])) result.Add(views[i]);
        }

        return result;
    }

    /// <summary>
    /// 判断是否“依赖未就绪”类错误，适合延后重试
    /// </summary>
    private static bool IsDependencyMissing(PostgresException ex)
    {
        // 42P01: undefined_table; 42703: undefined_column; 42704: undefined_object; 42883: undefined_function
        return ex != null && (ex.SqlState == "42P01" || ex.SqlState == "42703" || ex.SqlState == "42704" || ex.SqlState == "42883");
    }

    /// <summary>
    /// 迁移所需的辅助函数：uf_getMask，否则会在迁移某些视图时失败。
    /// </summary>
    private void ConvertFunction_Uf_Get_Mask(NpgsqlConnection targetConnection)
    {
        var cmd = @"
CREATE OR REPLACE FUNCTION uf_getMask(positions text, mask_char text DEFAULT '1')
RETURNS varchar(30)
LANGUAGE plpgsql
AS $$
DECLARE
    result_length CONSTANT int := 30;
    result_text   text;
    default_char  text;
    i             int;
    comma_pos     int;
    temp          text;
    j             int;
    work          text;
BEGIN
    mask_char := left(coalesce(mask_char,'1'),1);

    IF mask_char = '1' THEN
        default_char := '0';
    ELSE
        default_char := '1';
        mask_char    := '0';
    END IF;

    IF positions = 'all' THEN
        RETURN repeat(mask_char, result_length)::varchar(30);
    END IF;

    result_text := repeat(default_char, result_length);

    IF positions IS NULL OR btrim(positions) = '' THEN
        RETURN result_text::varchar(30);
    END IF;

    work := btrim(positions) || ',';  -- 模拟 T-SQL 末尾追加逗号
    comma_pos := position(',' IN work);

    WHILE comma_pos > 0 LOOP
        temp := left(work, comma_pos - 1);
        IF temp ~ '^[0-9]+$' THEN
            j := temp::int;
            IF j BETWEEN 1 AND result_length THEN
                result_text := overlay(result_text placing mask_char from j for 1);
            END IF;
        END IF;
        work := substring(work FROM comma_pos + 1);
        comma_pos := position(',' IN work);
    END LOOP;

    RETURN result_text::varchar(30);
END;
$$;";
        using (var cmdObj = new NpgsqlCommand(cmd, targetConnection))
        {
            cmdObj.ExecuteNonQuery();
        }
        _logger.Log($"辅助函数 uf_getMask 已创建.");
    }

    /// <summary>
    /// 生成条件删除视图的 PL/pgSQL 代码（已存在则 DROP 再 CREATE）
    /// </summary>
    private static string BuildConditionalDrop(string name)
    {
        var escaped = name.Replace("'", "''");
        var sb = new StringBuilder();
        sb.AppendLine("DO $$");
        sb.AppendLine("BEGIN");
        sb.AppendLine("  IF EXISTS (SELECT 1");
        sb.AppendLine("              FROM pg_class c");
        sb.AppendLine("              JOIN pg_namespace n ON n.oid = c.relnamespace");
        sb.AppendLine($"             WHERE c.relname = '{escaped}'");
        sb.AppendLine("               AND n.nspname = ANY(current_schemas(true))");
        sb.AppendLine("               AND c.relkind = 'm') THEN");
        sb.AppendLine($"    EXECUTE 'DROP MATERIALIZED VIEW IF EXISTS \"{name}\" CASCADE';");
        sb.AppendLine("  ELSIF EXISTS (SELECT 1");
        sb.AppendLine("                 FROM pg_class c");
        sb.AppendLine("                 JOIN pg_namespace n ON n.oid = c.relnamespace");
        sb.AppendLine($"                WHERE c.relname = '{escaped}'");
        sb.AppendLine("                  AND n.nspname = ANY(current_schemas(true))");
        sb.AppendLine("                  AND c.relkind = 'v') THEN");
        sb.AppendLine($"    EXECUTE 'DROP VIEW IF EXISTS \"{name}\" CASCADE';");
        sb.AppendLine("  END IF;");
        sb.AppendLine("END $$;");
        return sb.ToString();
    }
}