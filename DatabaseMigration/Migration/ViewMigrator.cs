using Microsoft.Data.SqlClient;
using Npgsql;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace DatabaseMigration.Migration
{
    /// <summary>
    /// 视图迁移器：将 SQL Server 视图转换为 PostgreSQL 视图
    /// </summary>
    public class ViewMigrator
    {
        private readonly FileLoggerService _logger;

        // Reuse compiled regex across methods
        private static readonly Regex ReCreateView = new(@"^\s*(CREATE|ALTER)\s+VIEW\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ReSelect = new(@"^\s*SELECT\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ReUnion = new(@"^\s*UNION(\s+ALL)?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ReLineComment = new(@"^\s*--", RegexOptions.Compiled);
        private static readonly Regex ReUnionTail = new(@"\bUNION(\s+ALL)?\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ReSelectHead = new(@"^\s*SELECT\s+(.*)$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

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
        /// 核心方法，将 SQL Server 视图定义转换为 PostgreSQL 语法
        /// </summary>
        private static string ConvertViewToPostgres(string viewDefinition, string viewName)
        {
            if (string.IsNullOrEmpty(viewDefinition))
                return string.Empty;

            // 在非字符串/注释中移除 dbo 前缀（例如dbo.Table 或dbo.fn）
            viewDefinition = MigrationUtils.RemoveSchemaPrefix(viewDefinition);

            // 1) 先按物理行切分，再把“字符串字面量中的换行”合并为同一逻辑行
            var lines = viewDefinition.Replace("\r\n", "\n").Split('\n');

            var outSb = new StringBuilder();
            bool inBlockComment = false;
            bool headerEmitted = false;
            bool firstSelectDone = false;
            bool pendingUnion = false;
            List<string> expectedAliases = null;
            List<bool> expectedStringFlags = null; // 对应第一条 SELECT 中的列是否为字符串类型

            foreach (var raw in lines)
            {
                string line = raw;

                //如果当前是在块注释中，则直接输出该行，并且判断块注释是否结束
                if (inBlockComment)
                {
                    outSb.AppendLine(line);
                    if (MigrationUtils.IsEndWithBlockComment(line)) inBlockComment = false;
                    continue;
                }
                // 块注释开始，则直接输出该行，并根据当前行是否有结束符判断是否继续处于块注释中
                if (MigrationUtils.IsStartWithBlockComment(line))
                {
                    inBlockComment = !MigrationUtils.IsEndWithBlockComment(line);
                    outSb.AppendLine(line);
                    continue;
                }

                // 行注释或空行，则原样输出
                if (ReLineComment.IsMatch(line) || string.IsNullOrWhiteSpace(line))
                {
                    outSb.AppendLine(line);
                    continue;
                }

                // 忽略头部无关行
                if (!headerEmitted && MigrationUtils.IsIgnorableHeader(line))
                    continue;

                // CREATE/ALTER VIEW -> 条件 DROP 后 CREATE
                if (ReCreateView.IsMatch(line))
                {
                    outSb.Append(BuildConditionalDrop(viewName));
                    bool isLineContainsAs = Regex.IsMatch(line, @"\bAS\b", RegexOptions.IgnoreCase);
                    outSb.Append($"CREATE VIEW \"{viewName}\"")
                        .AppendLine(isLineContainsAs ? " as " : " ");
                    headerEmitted = true;
                    continue;
                }

                // 若上行是独占 UNION，本行是 SELECT：按别名规则重建（保留行内块注释）
                if (pendingUnion && ReSelect.IsMatch(line) && firstSelectDone)
                {
                    var (code, comment, unclosed) = MigrationUtils.SplitInlineBlockCommentStart(line);
                    string rebuilt = BuildSelectWithAliases(code, expectedAliases, expectedStringFlags);
                    outSb.AppendLine(comment != null ? $"{rebuilt} {comment}" : rebuilt);
                    if (unclosed) inBlockComment = true;
                    pendingUnion = false;
                    continue;
                }

                // 第一条 SELECT：输出头并记录别名（保留行内块注释）
                if (ReSelect.IsMatch(line) && !firstSelectDone)
                {
                    if (!headerEmitted)
                    {
                        outSb.Append(BuildConditionalDrop(viewName));
                        outSb.AppendLine($"CREATE VIEW \"{viewName}\" AS");
                        headerEmitted = true;
                    }

                    var split = MigrationUtils.SplitInlineBlockCommentStart(line);
                    string firstSelect = BuildSelectWithAliases(split.code, null, null);
                    outSb.AppendLine(split.comment != null ? $"{firstSelect} {split.comment}" : firstSelect);
                    if (split.unclosed) inBlockComment = true;

                    // 抽取投影并记录别名和类型
                    var (projOnly, _) = SplitSelectProjectionAndFrom(firstSelect);
                    var fields = SplitFields(projOnly);
                    expectedAliases = new List<string>(fields.Count);
                    expectedStringFlags = new List<bool>(fields.Count);
                    foreach (var f in fields)
                    {
                        var (expr, alias, _) = SplitExprAndAliasSafe(f);
                        expectedAliases.Add(alias);
                        expectedStringFlags.Add(IsStringExpression(expr));
                    }

                    firstSelectDone = true;
                    continue;
                }

                // UNION 分支
                if (ReUnion.IsMatch(line) && firstSelectDone)
                {
                    var mUnion = ReUnion.Match(line);
                    string sep = mUnion.Groups[1].Success ? $"UNION{mUnion.Groups[1].Value.ToUpper()}" : "UNION";
                    string rest = line.Substring(mUnion.Length);

                    if (!ReSelect.IsMatch(rest))
                    {
                        outSb.AppendLine(sep);
                        pendingUnion = true;
                        continue;
                    }

                    var split = MigrationUtils.SplitInlineBlockCommentStart(rest);
                    string rebuilt = BuildSelectWithAliases(split.code, expectedAliases, expectedStringFlags);
                    outSb.AppendLine(split.comment != null ? $"{sep} {rebuilt} {split.comment}" : $"{sep} {rebuilt}");
                    if (split.unclosed) inBlockComment = true;
                    pendingUnion = false;
                    continue;
                }

                // 其他行：做基础规范化（去方括号），并保留行内块注释
                {
                    var (codeOnly, comment, unclosed) = MigrationUtils.SplitInlineBlockCommentStart(line);
                    string normalized = MigrationUtils.ReplaceBrackets(codeOnly);
                    outSb.AppendLine(comment != null ? $"{normalized} {comment}" : normalized);
                    if (unclosed) inBlockComment = true;
                }
            }

            return outSb.ToString();
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
        /// 将一条 SQL Server 视图的 SELECT 投影部分拆分为投影和 FROM 及其后续部分
        /// 例如 "SELECT a, b FROM t WHERE x" -> ("a, b", "FROM t WHERE x")
        /// </summary>
        /// <param name="selectLine">原select语句</param>
        /// <returns>拆分后的投影 projectionOnly：select语句内容，fromAndRest：from以及其他部分 </returns>
        private static (string projectionOnly, string fromAndRest) SplitSelectProjectionAndFrom(string selectLine)
        {
            var m = ReSelectHead.Match(selectLine);
            if (!m.Success) return (selectLine, null);
            string projectionAndRest = m.Groups[1].Value;

            int fromIdx = FindTopLevelKeywordIndex(projectionAndRest, "from");
            if (fromIdx < 0)
                return (MigrationUtils.ReplaceBrackets(projectionAndRest).TrimEnd(), null);

            string projection = MigrationUtils.ReplaceBrackets(projectionAndRest.Substring(0, fromIdx).TrimEnd());
            string fromAndRest = MigrationUtils.ReplaceBrackets(projectionAndRest.Substring(fromIdx).Trim());
            return (projection, fromAndRest);
        }

        /// <summary>
        /// 查找指定关键字在顶层的位置（不在引号或括号内）
        /// 用于定位 FROM、WHERE、GROUP BY 等关键字
        /// </summary>
        /// <param name="text">包含关键字的文本</param>
        /// <param name="keyword">待搜索的关键字</param>
        /// <returns>如果在顶层中存在，则返回其位置，否则返回-1</returns>
        private static int FindTopLevelKeywordIndex(string text, string keyword)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(keyword)) return -1;
            int len = text.Length;
            int klen = keyword.Length;
            bool inSQ = false, inDQ = false; int depth = 0;
            for (int i = 0; i <= len - klen; i++)
            {
                char c = text[i];
                if (c == '\'' && !inDQ) { inSQ = !inSQ; continue; }
                if (c == '"' && !inSQ) { inDQ = !inDQ; continue; }
                if (inSQ || inDQ) continue;
                if (c == '(') { depth++; continue; }
                if (c == ')') { depth = Math.Max(0, depth - 1); continue; }
                if (depth == 0)
                {
                    if (string.Compare(text, i, keyword, 0, klen, true) == 0)
                    {
                        bool prevOk = i == 0 || !char.IsLetterOrDigit(text[i - 1]);
                        bool nextOk = (i + klen >= len) || !char.IsLetterOrDigit(text[i + klen]);
                        if (prevOk && nextOk) return i;
                    }
                }
            }
            return -1;
        }
        /// <summary>
        /// 构建select语句，补齐别名
        /// </summary>
        /// <param name="selectLine">当前select语句</param>
        /// <param name="expected">从第一行select语句提取出来的列名列表</param>
        /// <param name="expectedStringKinds">从第一行select语句提取出来的列是否是string类型的列表</param>
        /// <returns>转换后的select语句，含列名</returns>
        static string BuildSelectWithAliases(string selectLine, List<string> expected, List<bool> expectedStringKinds)
        {
            var (projection, fromAndRest) = SplitSelectProjectionAndFrom(selectLine);
            if (string.IsNullOrEmpty(projection)) return selectLine;

            projection = ConvertConvertToCast(projection);

            var fields = SplitFields(projection);
            for (int i = 0; i < fields.Count; i++)
            {
                bool forceString = expectedStringKinds != null && i < expectedStringKinds.Count && expectedStringKinds[i];
                fields[i] = NormalizeField(fields[i], forceString);
            }

            if (expected != null && expected.Count == fields.Count)
            {
                for (int i = 0; i < fields.Count; i++)
                {
                    var (expr, alias, tail) = SplitExprAndAliasSafe(fields[i]);
                    var target = expected[i];
                    if (!string.IsNullOrEmpty(target))
                    {
                        if (alias == null || !alias.Equals(target, StringComparison.OrdinalIgnoreCase))
                            fields[i] = $"{expr.Trim()} AS {target}{tail}";
                        else
                            fields[i] = $"{expr}{(alias != null ? $" AS {alias}" : "")}{tail}";
                    }
                }
            }

            var result = new StringBuilder();
            result.Append("SELECT ")
                  .Append(string.Join(", ", fields));

            // 如果原来行尾有union all，则保留
            if (ReUnionTail.IsMatch(selectLine))
            {
                result.Append(" union all ");
            }

            // 若存在从句，把它附回
            if (!string.IsNullOrEmpty(fromAndRest))
            {
                result.Append(' ').Append(fromAndRest);
            }

            return result.ToString();
        }
        /// <summary>
        /// 将select语句体拆分为字段列表，按,逗号分隔
        /// </summary>
        /// <param name="projection"></param>
        /// <returns></returns>
        static List<string> SplitFields(string projection)
        {
            var list = new List<string>();
            int start = 0, depth = 0;
            bool inSQ = false, inDQ = false;
            for (int i = 0; i < projection.Length; i++)
            {
                char c = projection[i];
                if (c == '\'' && !inDQ) inSQ = !inSQ;
                else if (c == '"' && !inSQ) inDQ = !inDQ;
                else if (!inSQ && !inDQ)
                {
                    if (c == '(') depth++;
                    else if (c == ')') depth = Math.Max(0, depth - 1);
                    else if (c == ',' && depth == 0)
                    {
                        list.Add(projection.Substring(start, i - start));
                        start = i + 1;
                    }
                }
            }
            list.Add(projection.Substring(start));
            return list;
        }
        /// <summary>
        /// 从指定的字段表达式中，取出其中的值，别名，和尾部注释
        /// 例如 "a + b AS col -- comment" -> ("a + b", "col", "-- comment")
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        static (string expr, string alias, string commentTail) SplitExprAndAliasSafe(string field)
        {
            string s = field;
            int commentIdx = -1;
            bool inSQ = false, inDQ = false;
            int depth = 0;
            for (int i = 0; i < s.Length - 1; i++)
            {
                char c = s[i];
                if (c == '\'' && !inDQ) { inSQ = !inSQ; continue; }
                if (c == '"' && !inSQ) { inDQ = !inDQ; continue; }
                if (!inSQ && !inDQ)
                {
                    if (c == '(') depth++;
                    else if (c == ')') depth = Math.Max(0, depth - 1);
                    else if (depth == 0 && c == '-' && s[i + 1] == '-')
                    {
                        commentIdx = i;
                        break;
                    }
                }
            }
            string commentTail = commentIdx >= 0 ? s.Substring(commentIdx) : "";
            string head = commentIdx >= 0 ? s.Substring(0, commentIdx) : s;

            int lastAs = -1;
            depth = 0; inSQ = false; inDQ = false;
            for (int i = 0; i < head.Length; i++)
            {
                char c = head[i];
                if (c == '\'' && !inDQ) { inSQ = !inSQ; continue; }
                if (c == '"' && !inSQ) { inDQ = !inDQ; continue; }
                if (inSQ || inDQ) continue;

                if (c == '(') { depth++; continue; }
                if (c == ')') { depth = Math.Max(0, depth - 1); continue; }

                if (depth == 0)
                {
                    if ((i + 1 < head.Length) &&
                        (head[i] == 'A' || head[i] == 'a') &&
                        (head[i + 1] == 'S' || head[i + 1] == 's'))
                    {
                        bool prevOk = i == 0 || !char.IsLetterOrDigit(head[i - 1]);
                        bool nextOk = (i + 2 >= head.Length) || !char.IsLetterOrDigit(head[i + 2]);
                        if (prevOk && nextOk)
                            lastAs = i;
                    }
                }
            }

            if (lastAs < 0)
            {
                depth = 0; inSQ = false; inDQ = false;
                int eqIdx = -1;
                for (int i = 0; i < head.Length; i++)
                {
                    char c = head[i];
                    if (c == '\'' && !inDQ) { inSQ = !inSQ; continue; }
                    if (c == '"' && !inSQ) { inDQ = !inDQ; continue; }
                    if (inSQ || inDQ) continue;

                    if (c == '(') { depth++; continue; }
                    if (c == ')') { depth = Math.Max(0, depth - 1); continue; }
                    if (depth == 0 && c == '=') { eqIdx = i; break; }
                }
                if (eqIdx > 0)
                {
                    var left = head.Substring(0, eqIdx).Trim();
                    var right = head.Substring(eqIdx + 1).Trim();
                    if (Regex.IsMatch(left, @"^[A-Za-z_][A-Za-z0-9_]*$"))
                    {
                        return (right, left, commentTail);
                    }
                }

                // 额外处理：SQL Server 支持以空格分隔的别名（无 AS）
                int end = head.Length - 1;
                while (end >= 0 && char.IsWhiteSpace(head[end])) end--;
                if (end >= 0)
                {
                    int aliasStart = -1; string aliasTok = null; bool isBracket = false; bool isQuoted = false;
                    if (head[end] == ']')
                    {
                        int j = end - 1; while (j >= 0 && head[j] != '[') j--; if (j >= 0) { aliasStart = j; aliasTok = head.Substring(j + 1, end - j - 1); isBracket = true; }
                    }
                    else if (head[end] == '"')
                    {
                        int j = end - 1; while (j >= 0 && head[j] != '"') j--; if (j >= 0) { aliasStart = j; aliasTok = head.Substring(j + 1, end - j - 1); isQuoted = true; }
                    }
                    else
                    {
                        int j = end; while (j >= 0 && (char.IsLetterOrDigit(head[j]) || head[j] == '_')) j--; int start = j + 1; if (start <= end) { aliasStart = start; aliasTok = head.Substring(start, end - start + 1); }
                    }

                    if (!string.IsNullOrWhiteSpace(aliasTok) && aliasStart > 0)
                    {
                        int k = aliasStart - 1;
                        if (k >= 0 && char.IsWhiteSpace(head[k]))
                        {
                            while (k >= 0 && char.IsWhiteSpace(head[k])) k--;
                            bool ok = (k < 0 || head[k] != '.');
                            string exprPart = head.Substring(0, aliasStart).TrimEnd();
                            if (ok && exprPart.Trim().Length > 0)
                            {
                                bool acceptAlias = isBracket || isQuoted ? aliasTok.Length > 0 : Regex.IsMatch(aliasTok, @"^[A-Za-z_][A-Za-z0-9_]*$");
                                if (acceptAlias)
                                    return (exprPart, aliasTok, commentTail);
                            }
                        }
                    }
                }

                return (head.Trim(), null, commentTail);
            }

            string expr = head.Substring(0, lastAs).TrimEnd();
            string aliasPart = head.Substring(lastAs + 2).Trim();
            string alias;
            if (aliasPart.StartsWith("\""))
            {
                int end = aliasPart.IndexOf('"', 1);
                alias = end > 0 ? aliasPart.Substring(1, end - 1) : aliasPart.Trim('"');
            }
            else if (aliasPart.StartsWith("'"))
            {
                int end = aliasPart.IndexOf('\'', 1);
                alias = end > 0 ? aliasPart.Substring(1, end - 1) : aliasPart.Trim('\'');
            }
            else if (aliasPart.StartsWith("["))
            {
                int end = aliasPart.IndexOf(']', 1);
                alias = end > 0 ? aliasPart.Substring(1, end - 1) : aliasPart.Trim('[', ']');
            }
            else
            {
                int end = 0; while (end < aliasPart.Length && (char.IsLetterOrDigit(aliasPart[end]) || aliasPart[end] == '_' || aliasPart[end] == '.')) end++; alias = aliasPart.Substring(0, end);
            }

            // 规范化：空字符串视为 null，便于上层判断是否需要补齐别名
            if (string.IsNullOrWhiteSpace(alias)) alias = null;
            return (expr, alias, commentTail);
        }
        /// <summary>
        /// 处理sql server中的convert函数，转换为postgresql的cast语法
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        static string ConvertConvertToCast(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            var sb = new StringBuilder();
            int i = 0;
            while (i < s.Length)
            {
                int idx = IndexOfWordIgnoreCase(s, "convert", i);
                if (idx < 0)
                {
                    sb.Append(s, i, s.Length - i);
                    break;
                }

                sb.Append(s, i, idx - i);
                int j = idx + "convert".Length;

                while (j < s.Length && char.IsWhiteSpace(s[j])) j++;
                if (j >= s.Length || s[j] != '(')
                {
                    sb.Append(s[idx]);
                    i = idx + 1;
                    continue;
                }

                int startParen = j;
                j++;
                int depth = 1;
                bool inSQ = false, inDQ = false;

                SkipSpaces();
                int typeStart = j;
                while (j < s.Length && (char.IsLetterOrDigit(s[j]) || s[j] == '_'))
                    j++;
                string typeName = s.Substring(typeStart, j - typeStart);

                SkipSpaces();
                string typeLen = null;
                if (j < s.Length && s[j] == '(')
                {
                    j++;
                    int lenStart = j;
                    while (j < s.Length && char.IsDigit(s[j])) j++;
                    typeLen = s.Substring(lenStart, j - lenStart);
                    SkipSpaces();
                    if (j < s.Length && s[j] == ')') j++;
                }

                SkipSpaces();
                if (j >= s.Length || s[j] != ',')
                {
                    sb.Append(s, idx, (startParen - idx) + 1);
                    i = startParen + 1;
                    continue;
                }
                j++;

                SkipSpaces();
                int exprStart = j;
                for (; j < s.Length; j++)
                {
                    char c = s[j];
                    if (c == '\'' && !inDQ) { inSQ = !inSQ; continue; }
                    if (c == '"' && !inSQ) { inDQ = !inDQ; continue; }
                    if (inSQ || inDQ) continue;

                    if (c == '(') depth++;
                    else if (c == ')')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            string expr = s.Substring(exprStart, j - exprStart).Trim();
                            sb.Append("CAST(");
                            sb.Append(expr);
                            sb.Append(" AS ");
                            sb.Append(typeName);
                            if (!string.IsNullOrEmpty(typeLen))
                            {
                                sb.Append('(');
                                sb.Append(typeLen);
                                sb.Append(')');
                            }
                            sb.Append(')');

                            i = j + 1;
                            break;
                        }
                    }
                }

                if (j >= s.Length)
                {
                    sb.Append(s, idx, s.Length - idx);
                    i = s.Length;
                }

                void SkipSpaces()
                {
                    while (j < s.Length && char.IsWhiteSpace(s[j])) j++;
                }
            }

            return sb.ToString();

            static int IndexOfWordIgnoreCase(string text, string word, int start)
            {
                for (int k = start; k <= text.Length - word.Length; k++)
                {
                    if (char.ToLowerInvariant(text[k]) == char.ToLowerInvariant(word[0]) &&
                        string.Compare(text, k, word, 0, word.Length, true) == 0)
                    {
                        return k;
                    }
                }
                return -1;
            }
        }
        /// <summary>
        /// 判断表达式是否为字符串类型（字面量或 CAST(... AS {char types}）
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        static bool IsStringExpression(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr)) return false;
            string e = expr.Trim();
            // 去掉包裹的括号
            while (e.Length >= 2 && e[0] == '(' && e[e.Length - 1] == ')') e = e.Substring(1, e.Length - 2).Trim();
            // 字符串字面量
            if (e.StartsWith("'")) return true;
            // CAST(... AS {char types})
            if (Regex.IsMatch(e, @"(?is)\bCAST\s*\([^)]*\bAS\s+(n?varchar|n?char|text|bpchar|citext|name)\b")) return true;
            return false;
        }
        /// <summary>
        /// 判断表达式是否为数值字面量
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        static bool IsNumericLiteral(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr)) return false;
            string e = expr.Trim();
            while (e.Length >= 2 && e[0] == '(' && e[e.Length - 1] == ')') e = e.Substring(1, e.Length - 2).Trim();
            return Regex.IsMatch(e, @"^[+-]?\d+(\.\d+)?([eE][+-]?\d+)?$");
        }
        /// <summary>
        /// 将字段表达式规范化为 PostgreSQL 语法，并根据需要强制转换为字符串类型
        /// </summary>
        /// <param name="field"></param>
        /// <param name="forceString"></param>
        /// <returns></returns>
        static string NormalizeField(string field, bool forceString = false)
        {
            string f = MigrationUtils.ReplaceBrackets(field);
            f = ConvertConvertToCast(f);
            var (expr, alias, tail) = SplitExprAndAliasSafe(f);

            if (forceString)
            {
                var trimmed = expr?.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    if (!IsStringExpression(trimmed) && !Regex.IsMatch(trimmed, @"(?is)^NULL\b") && IsNumericLiteral(trimmed))
                    {
                        expr = $"'{trimmed}'";
                    }
                }
            }

            if (alias == null)
            {
                int eqTop = -1; int depth = 0; bool inSQ = false, inDQ = false;
                for (int i = 0; i < expr.Length; i++)
                {
                    char c = expr[i];
                    if (c == '\'' && !inDQ) { inSQ = !inSQ; continue; }
                    if (c == '"' && !inSQ) { inDQ = !inDQ; continue; }
                    if (inSQ || inDQ) continue;
                    if (c == '(') { depth++; continue; }
                    if (c == ')') { depth = Math.Max(0, depth - 1); continue; }
                    if (depth == 0 && c == '=') { eqTop = i; break; }
                }
                if (eqTop > 0)
                {
                    var left = expr.Substring(0, eqTop).Trim();
                    var right = expr.Substring(eqTop + 1).Trim();
                    if (Regex.IsMatch(left, @"^[A-Za-z_][A-Za-z0-9_]*$"))
                        return $"{right} AS {left}{tail}";
                }
                return f;
            }
            return $"{expr} AS {alias}{tail}";
        }

        /// <summary>
        /// 生成条件删除视图的 PL/pgSQL 代码（已存在则 DROP 再 CREATE）
        /// </summary>
        static string BuildConditionalDrop(string name)
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
}