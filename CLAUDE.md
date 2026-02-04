# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

这是一个 SQL Server 到 PostgreSQL 的数据库迁移工具，使用 WPF + C# .NET 9 开发。主要功能是自动解析和转换 SQL Server 的表结构、视图和存储过程，生成兼容 PostgreSQL 的 SQL 脚本。

## 常用命令

### 构建项目

```bash
dotnet build DatabaseSqlServer2PostgreSql.sln
```

### 运行所有单元测试

```bash
dotnet test DatabaseMigrationTest\DatabaseMigrationTest.csproj
```

### 运行单个测试文件

```bash
dotnet test --filter "FullyQualifiedName~MigrationUtils_ConvertToPostgresType_Tests"
```

### 运行特定测试方法

```bash
dotnet test --filter "FullyQualifiedName~TestMethodName"
```

## 核心架构

### 目录结构

```
DatabaseSqlServer2PostgreSql/
├── DatabaseMigration/           # 主项目（WPF 应用程序）
│   ├── Migration/              # 迁移器和服务层
│   │   ├── MigrationService.cs        # 迁移流程总控制器
│   │   ├── TableMigrator.cs           # 表结构迁移器
│   │   ├── ViewMigrator.cs            # 视图迁移器（支持依赖拓扑排序）
│   │   ├── StoredProcedureMigrator.cs # 存储过程迁移器
│   │   ├── MigrationUtils.cs          # 类型映射和工具方法
│   │   ├── FileLoggerService.cs       # 日志服务
│   │   ├── StringExtension.cs         # 字符串转换扩展
│   │   └── StringBuilderExtension.cs  # StringBuilder 扩展
│   └── ScriptGenerator/         # SQL 解析和转换引擎
│       ├── PostgreSqlScriptGenerator.cs           # 抽象基类
│       ├── PostgreSqlProcedureScriptGenerator.cs  # 存储过程脚本生成器
│       ├── PostgreSqlViewScriptGenerator.cs       # 视图脚本生成器
│       └── TSqlFragmentExtension_*.cs              # T-SQL 片段处理扩展
└── DatabaseMigrationTest/       # 单元测试项目（xUnit）
```

### 迁移流程

1. **MigrationService** 作为入口点，创建 SQL Server 和 PostgreSQL 连接
2. 按顺序调用各个 Migrator：
   - **TableMigrator**: 迁移表结构（CREATE TABLE），Development 模式下复制前 10 行数据
   - **ViewMigrator**: 使用拓扑排序处理视图依赖关系，支持迭代重试
   - **StoredProcedureMigrator**: 使用 ScriptDom 解析并转换为 PL/pgSQL

### 核心设计模式

#### ScriptGenerator 层级结构

- **PostgreSqlScriptGenerator**: 抽象基类，处理 T-SQL Token 流的通用转换
- 子类重写特定方法以实现对象特定的转换逻辑（如存储过程的 `$$` 定界符处理）

#### SQL 解析机制

使用 **Microsoft.SqlServer.TransactSql.ScriptDom** 将 T-SQL 解析为 Token 流，然后按 Token 类型进行转换：

```csharp
// 核心转换流程
TSqlFragment fragment = new TSql170Parser(true).Parse(scriptReader, out errors);
string postgresSql = new PostgreSqlXxxScriptGenerator().GenerateSqlScript(fragment);
```

#### 语句类型分发

`ConvertSingleCompleteSqlAndSqlBatch()` 方法根据第一个 Token 类型分发到不同转换器：

- `Create` → `ConvertCreateSqlAndSqlBatch()`
- `If` → `ConvertIfBlockSql()`
- `Alter` → `ConvertAlterSql()`
- `Delete`/`Insert`/`Update` → 各自的转换方法
- `Select` → `ConvertSelectSql()`

### 数据类型映射

**MigrationUtils.ConvertToPostgresType()** 实现完整的类型映射：

| SQL Server       | PostgreSQL | 特殊处理                 |
| ---------------- | ---------- | -------------------- |
| int              | integer    | IDENTITY → serial    |
| bigint           | bigint     | IDENTITY → bigserial |
| varchar(n)       | varchar(n) | 保持精度                 |
| datetime         | timestamp  | 自动转换                 |
| uniqueidentifier | uuid       | 默认值 NEWID() 需特殊处理    |

### 标识符处理规则

- **ToPostgreSqlIdentifier()**: 转换为小写，去除 `dbo.` 前缀，去除 `[]` 方括号
- **ToPostgreVariableName()**: `@variable` → `variable`（去掉 `@` 前缀）

### 视图依赖处理

ViewMigrator 使用拓扑排序算法处理视图依赖：

1. 构建依赖图（A 视图依赖 B 视图）
2. 执行拓扑排序确定创建顺序
3. 迭代重试机制：失败时重试剩余视图最多 3 次

### 存储过程转换要点

1. **参数处理**: 无参数存储过程添加 `()`
2. **语言声明**: 添加 `LANGUAGE plpgsql`
3. **定界符**: 使用 `$$ ... $$` 包裹函数体
4. **变量声明**: 提取所有 DECLARE 语句到块首
5. **BEGIN 块**: 有变量时添加 `BEGIN ... END`

## 重要开发注意事项

### TDD（测试驱动开发）工作流程

**重要：修改代码前必须先有测试！**

当需要修复错误或添加新功能时，必须遵循 TDD 流程：

1. **红（编写失败的测试）**
   - 针对要修复的错误或要添加的功能，先编写或更新单元测试
   - 运行测试确认其失败（红色）

2. **绿（编写最少代码使测试通过）**
   - 修改代码以使测试通过
   - 不要过度设计，只写能让测试通过的最少代码
   - 运行测试确认其通过（绿色）

3. **重构（优化代码质量）**
   - 在保持测试通过的前提下，重构和优化代码
   - 确保所有测试仍然通过

**原因**：
- 确保每个修改都有测试覆盖，防止回归
- 测试即文档，明确代码的预期行为
- 快速反馈，立即发现修改是否破坏了现有功能

### 修改迁移逻辑时的要点

1. **先理解 Token 流**: 使用 ScriptDom 解析后，通过 `ScriptTokenStream` 查看 Token 序列
2. **重写而非替换**: 优先重写基类虚方法，避免修改通用逻辑
3. **保留测试覆盖**: 修改转换逻辑时务必添加/更新单元测试
4. **日志记录**: 使用 `_logger.Log()` 记录转换失败的情况

### 添加新的语句类型支持

如需支持新的 T-SQL 语句类型：

1. 在 `PostgreSqlScriptGenerator.ConvertSingleCompleteSqlAndSqlBatch()` 中添加 Token 类型判断
2. 创建新的 `protected virtual string ConvertXxxSql()` 方法
3. 在子类中重写以实现特定对象的转换逻辑

### 测试策略

- 每个转换器都有对应的测试文件（命名模式：`XxxExtension_Yyy_Tests.cs`）
- 测试数据使用 `[Theory]` + `[InlineData]` 进行多场景覆盖
- 测试失败时使用 `_logger.LogError()` 输出转换后的 SQL 用于调试

## 当前迁移状态（根据 README.md）

- [x] 表结构迁移
- [x] 视图迁移
- [ ] 存储过程迁移（开发中）
- [ ] 函数迁移
- [ ] 触发器迁移

## 技术栈

- **前端**: WPF (.NET 9)
- **数据库驱动**: Microsoft.Data.SqlClient (SQL Server), Npgsql (PostgreSQL)
- **SQL 解析**: Microsoft.SqlServer.TransactSql.ScriptDom 170.128.0
- **日志**: Serilog
- **测试**: xUnit 2.5.3

# 修改流程

1. 运行程序进行迁移，检查日志文件中是否有记录到迁移错误

2. 如果有迁移错误，则提取存储过程名称

3. 从源sql server中获取存储过程原始定义

4. 从日志文件中获取迁移后的定义

5. 对比分析是哪一部分有问题

6. 将有问题的部分，先增加单元测试，然后再进行修复

7. 修复完成后再重复上述步骤，直到全部迁移成功
