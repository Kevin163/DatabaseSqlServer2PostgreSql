using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Text;

namespace DatabaseMigration.ScriptGenerator;

/// <summary>
/// postgreSQL脚本生成器
/// </summary>
public abstract class PostgreSqlScriptGenerator
{
    /// <summary>
    /// 由子类实现，创建SQL脚本生成访问器
    /// </summary>
    /// <param name="fragment"></param>
    /// <returns></returns>
    public abstract string GenerateSqlScript(TSqlFragment fragment);
}
