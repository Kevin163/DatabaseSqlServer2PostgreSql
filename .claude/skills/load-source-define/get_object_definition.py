#!/usr/bin/env python3
"""
从 SQL Server 数据库获取存储过程、视图、函数的定义文本

使用方法:
    python get_object_definition.py --proc "usp_MyProcedure"
    python get_object_definition.py --view "vw_MyView"
    python get_object_definition.py --func "fn_MyFunction"

依赖:
    pip install pyodbc
"""

import argparse
import sys
import pyodbc


# 数据库连接配置（已硬编码）
DB_CONNECTION_STRING = (
    "Driver={ODBC Driver 17 for SQL Server};"
    "Server=192.168.1.111\\server2008;"
    "Database=pmsmasterdev;"
    "Uid=jxd;"
    "Pwd=jxd598;"
    "TrustServerCertificate=yes;"
)

ROW_LENGTH_FOR_SP_HELPTEXT = 255


def get_object_definition(object_name, object_type=None):
    """
    使用 sp_helptext 获取对象的定义文本

    Args:
        object_name: 对象名称（可以包含 schema，如 dbo.MyProc）
        object_type: 对象类型（可选，用于验证）

    Returns:
        str: 对象的定义文本，如果失败则返回空字符串
    """
    try:
        # 使用 pyodbc 建立连接
        conn = pyodbc.connect(DB_CONNECTION_STRING)
        cursor = conn.cursor()

        # 执行 sp_helptext（pyodbc 使用 execute 调用存储过程）
        cursor.execute("{call sp_helptext(?)}", object_name)

        # 获取结果并处理行合并
        definition = build_definition_from_rows(cursor)
        cursor.close()
        conn.close()

        return definition

    except Exception as e:
        print(f"错误: {e}", file=sys.stderr)
        return ""


def build_definition_from_rows(cursor):
    """
    从 sp_helptext 返回的结果集中构建完整的定义文本

    sp_helptext 会将超过 255 字符的行截断，需要智能合并这些行
    """
    lines = []
    is_split_line = False

    for row in cursor:
        if row and len(row) > 0:
            line = row[0]

            if line is None:
                continue

            line = line.rstrip('\r\n')

            # 处理被截断的行
            if is_split_line:
                # 如果上一行被截断，检查当前行是否需要合并
                # 规则：如果以 UNION 或 -- 开头，则不合并
                if line.strip():
                    trimmed = line.strip()
                    if (trimmed.upper().startswith('UNION') or
                            trimmed.startswith('--')):
                        # 不需要拼接，换行
                        lines.append('')

            # 如果当前行长度等于 255，可能是被截断的行
            if line.strip() and len(line) == ROW_LENGTH_FOR_SP_HELPTEXT:
                is_split_line = True
                # 追加到最后一行（不换行）
                if lines:
                    lines[-1] = lines[-1] + line
                else:
                    lines.append(line)
            else:
                is_split_line = False
                lines.append(line)

    return '\n'.join(lines)


def main():
    parser = argparse.ArgumentParser(
        description='从 SQL Server 获取存储过程、视图、函数的定义文本'
    )
    parser.add_argument('--proc', help='存储过程名称')
    parser.add_argument('--view', help='视图名称')
    parser.add_argument('--func', help='函数名称')
    parser.add_argument('--output', '-o', help='输出到文件')

    args = parser.parse_args()

    # 确定对象名称和类型
    object_name = None
    if args.proc:
        object_name = args.proc
    elif args.view:
        object_name = args.view
    elif args.func:
        object_name = args.func
    else:
        parser.error('必须指定 --proc, --view 或 --func 之一')

    # 获取定义（使用硬编码的数据库配置）
    definition = get_object_definition(object_name)

    if not definition:
        print(f"未找到对象 '{object_name}' 或对象已加密", file=sys.stderr)
        sys.exit(1)

    # 输出
    if args.output:
        with open(args.output, 'w', encoding='utf-8') as f:
            f.write(definition)
        print(f"定义已保存到: {args.output}")
    else:
        print(definition)


if __name__ == '__main__':
    main()
