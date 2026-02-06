# Load Source Object Definition - Quick Start

从源 SQL Server 数据库快速获取存储过程、视图、函数的定义文本。

## 安装依赖

```bash
pip install pyodbc
```

**注意**: 需要安装 [ODBC Driver 17 for SQL Server](https://learn.microsoft.com/en-us/sql/connect/odbc/download-odbc-driver-for-sql-server)

## 快速使用

### 1. 获取存储过程定义

```bash
python get_object_definition.py \
    --proc usp_MyProcedure \
    --output usp_MyProcedure_original.sql
```

### 2. 获取视图定义

```bash
python get_object_definition.py \
    --view vw_MyView \
    --output vw_MyView_original.sql
```

### 3. 获取函数定义（输出到屏幕）

```bash
python get_object_definition.py \
    --func fn_MyFunction
```

## 数据库连接

脚本使用硬编码的数据库连接信息：
- **Server**: `192.168.1.111\server2008`
- **Database**: `pmsmasterdev`
- **User**: `jxd`
- **Password**: `jxd598`

如需修改连接信息，请编辑 `get_object_definition.py` 中的 `DB_CONNECTION_STRING`。

## 典型工作流程

当迁移日志显示某个对象迁移失败时：

```bash
# 步骤 1: 获取原始 T-SQL 定义
python get_object_definition.py \
    --proc usp_FailedProcedure \
    --output original.sql

# 步骤 2: 从日志文件中获取转换后的 PostgreSQL 脚本
# 日志位置: DatabaseMigration/bin/Debug/net9.0-windows/logs/

# 步骤 3: 对比分析，找出问题所在
# 步骤 4: 创建单元测试并修复
```

## 支持的对象类型

- ✅ 存储过程 (Stored Procedures)
- ✅ 视图 (Views)
- ✅ 标量函数 (Scalar Functions)
- ✅ 表值函数 (Table-Valued Functions)
- ❌ 表 (Tables) - 请使用 SSMS 或 INFORMATION_SCHEMA
- ❌ 触发器 (Triggers) - 暂不支持

## 注意事项

- 如果对象不在 `dbo` schema 下，需要指定完整的 schema 名称，例如：`myschema.MyProc`
- 加密的对象（WITH ENCRYPTION）无法获取定义
- 需要对目标对象有 VIEW DEFINITION 权限
