using DatabaseMigration.Migration;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseMigration.ScriptGenerator;

/// <summary>
/// TSqlFragment的扩展方法，专门用于处理Alter语句
/// </summary>
public static class TSqlFragmentExtension_AlterSql
{
    /// <summary>
    /// 判断 tokens 是否为 "ALTER TABLE <name> ADD <column> <type> [NULL|NOT NULL]" 格式
    /// 并输出表名、列名、类型以及是否允许 NULL
    /// </summary>
    public static bool IsAlterTableAddColumn(this IList<TSqlParserToken> tokens, out string tableName, out List<ColumnDefineItem> columnDefins)
    {
        tableName = string.Empty;
        columnDefins = new List<ColumnDefineItem>();

        //需要检查的所有TokenType列表，其中的表示要取其中的值进行返回
        var tokenTypes = new List<TSqlTokenTypeItem>
        {
            new TSqlTokenTypeItem(TSqlTokenType.Alter),
            new TSqlTokenTypeItem(TSqlTokenType.Table),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier,TSqlTokenType.QuotedIdentifier,TSqlTokenType.AsciiStringOrQuotedIdentifier }, action:TSqlTokenTypeAction.OutIdentifier),
            new TSqlTokenTypeItem(TSqlTokenType.Add),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier,TSqlTokenType.QuotedIdentifier,TSqlTokenType.AsciiStringOrQuotedIdentifier }, action:TSqlTokenTypeAction.OutColumnDefinition),
        };
        //alter table可以同时增加多列，后面的重复列则由这里进行定义
        var repeatColumnTypes = new List<TSqlTokenTypeItem>
        {
            new TSqlTokenTypeItem(TSqlTokenType.Comma),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier,TSqlTokenType.QuotedIdentifier,TSqlTokenType.AsciiStringOrQuotedIdentifier }, action:TSqlTokenTypeAction.OutColumnDefinition),
        };
        var matchResult = tokens.IsMatchTokenTypesSequence(tokenTypes, repeatColumnTypes);
        if (matchResult.IsMatch && matchResult.OutValues.Count == 1 && matchResult.OutColumnDefines.Count >= 1)
        {
            tableName = matchResult.OutValues[0].ToPostgreSqlIdentifier();
            columnDefins = matchResult.OutColumnDefines;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 判断 tokens 是否为 "ALTER TABLE <name> ALTER COLUMN <column> <type> [NULL|NOT NULL]" 格式
    /// 并输出表名、列名、类型以及是否为 NOT NULL
    /// </summary>
    public static bool IsAlterTableAlterColumn(this IList<TSqlParserToken> tokens, out string tableName, out ColumnDefineItem columnDefine)
    {
        tableName = string.Empty;
        columnDefine = new ColumnDefineItem();

        //需要检查的所有TokenType列表，其中的表示要取其中的值进行返回
        var tokenTypes = new List<TSqlTokenTypeItem>
        {
            new TSqlTokenTypeItem(TSqlTokenType.Alter),
            new TSqlTokenTypeItem(TSqlTokenType.Table),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier,TSqlTokenType.QuotedIdentifier,TSqlTokenType.AsciiStringOrQuotedIdentifier }, action:TSqlTokenTypeAction.OutIdentifier),
            new TSqlTokenTypeItem(TSqlTokenType.Alter),
            new TSqlTokenTypeItem(TSqlTokenType.Column),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier,TSqlTokenType.QuotedIdentifier,TSqlTokenType.AsciiStringOrQuotedIdentifier }, action:TSqlTokenTypeAction.OutColumnDefinition),
        };
        var matchResult = tokens.IsMatchTokenTypesSequence(tokenTypes);
        if (matchResult.IsMatch && matchResult.OutValues.Count == 1 && matchResult.OutColumnDefines.Count >= 1)
        {
            tableName = matchResult.OutValues[0].ToPostgreSqlIdentifier();
            columnDefine = matchResult.OutColumnDefines[0];
            return true;
        }
        return false;
    }

    /// <summary>
    /// 判断 tokens 是否为 "ALTER TABLE <name> DROP CONSTRAINT <constraint>" 格式
    /// 并输出表名与约束名称
    /// </summary>
    public static bool IsAlterTableDropConstraint(this IList<TSqlParserToken> tokens, out string tableName, out string constraintName)
    {
        tableName = string.Empty;
        constraintName = string.Empty;
        if (tokens == null) return false;

        var tokenTypes = new List<TSqlTokenTypeItem>
        {
            new TSqlTokenTypeItem(TSqlTokenType.Alter),
            new TSqlTokenTypeItem(TSqlTokenType.Table),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier, TSqlTokenType.AsciiStringOrQuotedIdentifier}, action: TSqlTokenTypeAction.OutIdentifier),
            new TSqlTokenTypeItem(TSqlTokenType.Drop),
            new TSqlTokenTypeItem(TSqlTokenType.Constraint),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier, TSqlTokenType.AsciiStringOrQuotedIdentifier}, action: TSqlTokenTypeAction.OutValue),
        };

        var matchResult = tokens.IsMatchTokenTypesSequence(tokenTypes);
        if (matchResult.IsMatch && matchResult.OutValues.Count == 2)
        {
            tableName = matchResult.OutValues[0].ToPostgreSqlIdentifier();
            // normalize constraint name: remove quotes and lower-case to match Postgres identifier rules
            constraintName = matchResult.OutValues[1].TrimQuotes().ToPostgreSqlIdentifier();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 判断 tokens 是否为 "ALTER TABLE <name> ADD CONSTRAINT <constraint> PRIMARY KEY(<col1>[, <col2>...])" 格式
    /// 并输出表名、约束名称以及约束列（以逗号分隔的小写标识符）
    /// </summary>
    public static bool IsAlterTableAddConstraintPrimaryKey(this IList<TSqlParserToken> tokens, out string tableName, out string constraintName, out string constraintColumns)
    {
        tableName = string.Empty;
        constraintName = string.Empty;
        constraintColumns = string.Empty;
        if (tokens == null) return false;

        var tokenTypes = new List<TSqlTokenTypeItem>
        {
            new TSqlTokenTypeItem(TSqlTokenType.Alter),
            new TSqlTokenTypeItem(TSqlTokenType.Table),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier, TSqlTokenType.AsciiStringOrQuotedIdentifier}, action: TSqlTokenTypeAction.OutIdentifier),
            new TSqlTokenTypeItem(TSqlTokenType.Add),
            new TSqlTokenTypeItem(TSqlTokenType.Constraint),
            new TSqlTokenTypeItem(new List<TSqlTokenType>{TSqlTokenType.Identifier, TSqlTokenType.QuotedIdentifier, TSqlTokenType.AsciiStringOrQuotedIdentifier}, action: TSqlTokenTypeAction.OutValue),
            new TSqlTokenTypeItem(TSqlTokenType.Primary),
            new TSqlTokenTypeItem(TSqlTokenType.Key),
            new TSqlTokenTypeItem(TSqlTokenType.LeftParenthesis),
        };

        var matchResult = tokens.IsMatchTokenTypesSequence(tokenTypes);
        if (!matchResult.IsMatch || matchResult.OutValues.Count != 2) return false;

        tableName = matchResult.OutValues[0].ToPostgreSqlIdentifier();
        constraintName = matchResult.OutValues[1].TrimQuotes().ToPostgreSqlIdentifier();

        // parse columns inside parentheses starting from StopIndexOfToken
        var cols = new List<string>();
        int idx = matchResult.StopIndexOfToken;
        while (idx < tokens.Count)
        {
            var tk = tokens[idx];
            if (tk.TokenType == TSqlTokenType.RightParenthesis)
            {
                break;
            }
            if (tk.TokenType == TSqlTokenType.Identifier || tk.TokenType == TSqlTokenType.QuotedIdentifier || tk.TokenType == TSqlTokenType.AsciiStringOrQuotedIdentifier)
            {
                var col = tk.Text.TrimQuotes().ToPostgreSqlIdentifier();
                cols.Add(col);
            }
            idx++;
        }

        constraintColumns = string.Join(",", cols);
        return true;
    }
}
