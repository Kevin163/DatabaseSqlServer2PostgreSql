---
name: load-source-define
description: Use when needing to retrieve the T-SQL definition text of stored procedures, views, or functions from the source SQL Server database for analyzing migration failures, creating test cases, or debugging conversion issues
---

# Load Source Object Definition

## Overview

Retrieves the original T-SQL definition text of database objects (stored procedures, views, functions) from the source SQL Server database using Python script, enabling analysis and troubleshooting of migration issues.

## When to Use

```
Need source object definition?
├── Is it a stored procedure, view, or function?
│   ├── Yes → Use this skill
│   └── No → Use other methods (tables, triggers, etc.)
└── Purpose?
    ├── Debug migration error → Use this skill
    ├── Create unit test → Use this skill
    ├── Compare before/after → Use this skill
    └── Just browsing → Consider SSMS instead
```

**Use cases:**
- Analyzing why a stored procedure migration failed (check log, get original T-SQL, compare with converted PostgreSQL)
- Creating unit test data for specific object conversions
- Understanding complex object logic before fixing conversion issues
- Extracting object definitions when troubleshooting sp_helptext-related migration errors
- Comparing original T-SQL with converted PostgreSQL script to identify problematic statements

**When NOT to use:**
- Getting table structures (use `INFORMATION_SCHEMA` or SSMS)
- Getting trigger definitions (not yet supported)
- Browsing database schema (use SSMS or Azure Data Studio)
- Creating new database objects (use the migration tool instead)

## Core Pattern

### Before (Manual Approach)
```bash
# Open SSMS, navigate to object, right-click, modify, copy script
# Issues: Slow, can't automate, doesn't integrate with workflow
```

### After (Using Script)
```bash
# Quick, scriptable, integrates with debugging workflow
python .claude/skills/load-source-define/get_object_definition.py \
    --proc usp_GetCustomerData \
    --output original_tsql.sql
```

## Quick Reference

| Task | Command Example |
|------|-----------------|
| Get stored procedure | `python ... --proc usp_MyProc` |
| Get view definition | `python ... --view vw_Summary` |
| Get function definition | `python ... --func ufn_Calculate` |
| Save to file | Add `--output filename.sql` |
| Display on screen | Omit `--output` |

## Implementation

### Script Location
```
.claude/skills/load-source-define/get_object_definition.py
```

### Dependencies
```bash
pip install pyodbc
```

**Note**: Requires [ODBC Driver 17 for SQL Server](https://learn.microsoft.com/en-us/sql/connect/odbc/download-odbc-driver-for-sql-server) to be installed.

### Database Connection
The script uses hardcoded database connection information configured in `get_object_definition.py`:
- **Server**: `192.168.1.111\server2008`
- **Database**: `pmsmasterdev`
- **User**: `jxd`
- **Password**: `jxd598`

To modify the connection, edit the `DB_CONNECTION_STRING` in the script.

### Usage Examples

**Example 1: Get stored procedure definition**
```bash
python .claude/skills/load-source-define/get_object_definition.py \
    --proc usp_GetCustomerData \
    --output usp_GetCustomerData_original.sql
```

**Example 2: Get view definition**
```bash
python .claude/skills/load-source-define/get_object_definition.py \
    --view vw_SalesSummary \
    --output vw_SalesSummary_original.sql
```

**Example 3: Get function definition and display on screen**
```bash
python .claude/skills/load-source-define/get_object_definition.py \
    --func fn_CalculateTax
```

### Command-Line Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| `--proc` | No* | Stored procedure name |
| `--view` | No* | View name |
| `--func` | No* | Function name |
| `--output`, `-o` | No | Output file path (if omitted, prints to stdout) |

*One of `--proc`, `--view`, or `--func` is required

## How It Works

### Technical Details

1. **Connects using pyodbc**
   - Uses ODBC Driver 17 for SQL Server
   - SQL Server Authentication with user credentials
   - Hardcoded connection string (see `DB_CONNECTION_STRING` in script)

2. **Executes sp_helptext system stored procedure**
   ```sql
   EXEC sp_helptext 'dbo.usp_MyProcedure'
   ```
   - Preserves original formatting better than sys.sql_modules
   - Returns one row per line of T-SQL code

3. **Handles line splitting at 255 characters**
   - SQL Server's `sp_helptext` splits long lines at exactly 255 characters
   - Script detects and rejoins split lines intelligently
   - Exception: Lines starting with "UNION" or "--" are never joined (false positive prevention)

4. **Output options**
   - Default: Prints to console (stdout)
   - With `--output`: Saves to file with UTF-8 encoding

## Real-World Scenarios

### Scenario 1: Debugging Migration Failure

**Context:** Migration log shows "存储过程 usp_CalculateTax 迁移失败"

**Workflow:**
```bash
# Step 1: Get original T-SQL from source database
python .claude/skills/load-source-define/get_object_definition.py \
    --proc usp_CalculateTax \
    --output usp_CalculateTax_original.sql

# Step 2: Check log file for converted PostgreSQL script
# (通常在 DatabaseMigration/bin/Debug/net9.0-windows/logs/ 目录下)

# Step 3: Compare and identify the problematic statement
# 原始 T-SQL 可能包含不支持的语法，如：
# - 临时表的特殊用法
# - 特定的 T-SQL 函数
# - 复杂的 CTE 或子查询
```

### Scenario 2: Creating Unit Tests

**Context:** Adding support for new T-SQL syntax feature (e.g., CTE with UNION)

**Workflow:**
```bash
# Step 1: Find real objects using the feature
python .claude/skills/load-source-define/get_object_definition.py \
    --view vw_WithUnion \
    --output test_data_cte_union.sql

# Step 2: Create unit test using real definition
# Edit: DatabaseMigrationTest/TSqlFragmentExtension_Xxx_Tests.cs
[Theory]
[InlineData("vw_WithUnion")]  // Test data from real object
public void ConvertSelectWithUnion_Tests(string viewName)
{
    // Load real definition from test data file
    string tsql = File.ReadAllText($"test_data_{viewName}.sql");
    // Test conversion...
}
```

### Scenario 3: Analyzing Complex Objects

**Context:** Need to understand how an object works before fixing conversion logic

**Workflow:**
```bash
# Get definition and analyze patterns
python .claude/skills/load-source-define/get_object_definition.py \
    --proc usp_ComplexReport \
    --output analysis.sql

# Analyze specific patterns
grep -i "tempdb..#" analysis.sql  # Uses temp tables?
grep -i "DECLARE CURSOR" analysis.sql  # Uses cursors?
grep -i "WITH" analysis.sql  # Uses CTE?
```

## Common Mistakes

### Mistake 1: Not installing pyodbc
```bash
# ❌ Error: ModuleNotFoundError: No module named 'pyodbc'
python get_object_definition.py --proc MyProc

# ✅ Fix: Install dependency first
pip install pyodbc
python get_object_definition.py --proc MyProc
```

**Note**: Also ensure ODBC Driver 17 for SQL Server is installed on your system.

### Mistake 2: Object not in dbo schema
```bash
# ❌ Wrong: Assumes dbo schema, but object is in custom schema
python get_object_definition.py --proc MyProc
# Returns: 未找到对象 'MyProc'

# ✅ Correct: Specify full schema name
python get_object_definition.py --proc "myschema.MyProc"
```

### Mistake 3: Using this for tables
```bash
# ❌ Wrong: sp_helptext doesn't work for tables
python get_object_definition.py --proc Customers
# Returns: 未找到对象 'Customers'

# ✅ Correct: Use other methods for tables
# Use SSMS or query INFORMATION_SCHEMA.COLUMNS
```

### Mistake 4: Not using quotes for object names with spaces
```bash
# ❌ Wrong: Object name with spaces breaks command parsing
python get_object_definition.py --proc My Proc

# ✅ Correct: Use quotes
python get_object_definition.py --proc "My Proc"
```

### Mistake 5: Database connection is outdated
```bash
# ❌ Wrong: Connection fails with outdated credentials
python get_object_definition.py --proc MyProc
# Returns: Login failed for user 'jxd'

# ✅ Fix: Update DB_CONNECTION_STRING in get_object_definition.py
# Edit the file and modify the connection parameters
```

## Integration with Project Workflow

This skill integrates with the existing migration workflow described in CLAUDE.md:

1. **Run migration** → Find errors in log
2. **Extract procedure name** from error message
3. **Get original definition** using this skill
4. **Get converted definition** from log file
5. **Compare and analyze** to identify the issue
6. **Create unit test** with the problematic case
7. **Fix the conversion logic**
8. **Repeat** until all migrations succeed

## Related Code

### Current Usage in Codebase
- `DatabaseMigration/Migration/StoredProcedureMigrator.cs:53` - Uses `MigrationUtils.GetObjectDefinition()` (C# equivalent)
- `DatabaseMigration/Migration/ViewMigrator.cs:80` - Uses `MigrationUtils.GetObjectDefinition()` (C# equivalent)
- Both handle migration errors by logging both original and converted SQL

### C# Equivalent
The Python script provides the same functionality as:
```csharp
// C# code in MigrationUtils.cs
string definition = MigrationUtils.GetObjectDefinition(connection, "MyProc");
```

But offers advantages:
- No compilation required
- Can be run from command line independently
- Useful for quick debugging without rebuilding the project
- Hardcoded connection configuration for faster access

## Notes

- **Encryption**: Returns empty string for encrypted objects (WITH ENCRYPTION option)
- **Permissions**: Requires VIEW DEFINITION permission on the target object
- **Performance**: Very fast - single stored procedure call per object
- **Output encoding**: UTF-8 when saving to file
- **Line breaks**: Preserves original line breaks from T-SQL definition
- **Connection configuration**: Hardcoded in `DB_CONNECTION_STRING` within the script
