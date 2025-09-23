
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UAManagedCore;

namespace TestProject1
{
    public class SQLiteHelper : IDisposable
    {
        #region 常量
        // 数据库连接对象
        private SQLiteConnection _connection;
        // 数据库连接字符串
        private readonly string _connectionString;
        // 事务对象
        private SQLiteTransaction _transaction;
        #endregion
        public SQLiteHelper(string databasePath)
        {
            _connectionString = $"Data Source={databasePath};";
            _connection = new SQLiteConnection(_connectionString);
            _connection.Open(); // 打开连接（事务需要保持连接打开）

        }
        /// <summary>
        /// 获取连接字符串
        /// </summary>
        public SQLiteConnection connection { get { return _connection; } }
        #region 无实体查询功能 
        /// <summary>
        /// 无实体查询入口，用于动态表查询
        /// </summary>
        public DynamicQueryBuilder Query(string tableName)
        {
            return new DynamicQueryBuilder(_connectionString, EscapeSqlIdentifier(tableName));
        }

        /// <summary>
        /// 执行原始SQL查询，返回动态结果集
        /// </summary>
        /// <param name="sql">SQL查询语句</param>
        /// <param name="parameters">查询参数</param>
        /// <returns>动态对象列表</returns>
        public List<dynamic> ExecuteDynamicQuery(string sql, params SQLiteParameter[] parameters)
        {
            var result = new List<dynamic>();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(sql, connection))
                {
                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dynamic item = new ExpandoObject();
                            var properties = (IDictionary<string, object>)item;

                            // 遍历所有列并添加到动态对象
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string columnName = reader.GetName(i);
                                object value = reader.GetValue(i);

                                // 处理DBNull值
                                properties[columnName] = value == DBNull.Value ? null : value;
                            }

                            result.Add(item);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 动态查询构建器，支持链式调用
        /// </summary>
        public class DynamicQueryBuilder
        {
            private readonly string _connectionString;
            private readonly string _tableName;
            private string _whereClause;
            private List<SQLiteParameter> _parameters = new List<SQLiteParameter>();
            private string _orderBy;
            private int? _skip;
            private int? _take;

            public DynamicQueryBuilder(string connectionString, string tableName)
            {
                _connectionString = connectionString;
                _tableName = tableName;
            }

            /// <summary>
            /// 添加WHERE条件
            /// </summary>
            public DynamicQueryBuilder Where(string condition, params object[] values)
            {
                _whereClause = condition;

                // 解析参数并添加
                if (values != null && values.Length > 0)
                {
                    for (int i = 0; i < values.Length; i++)
                    {
                        string paramName = $"@p{i}";
                        _parameters.Add(new SQLiteParameter(paramName, values[i] ?? DBNull.Value));
                    }
                }

                return this;
            }

            /// <summary>
            /// 添加排序条件
            /// </summary>
            public DynamicQueryBuilder OrderBy(string orderByClause)
            {
                _orderBy = orderByClause;
                return this;
            }

            /// <summary>
            /// 分页：跳过指定数量的记录
            /// </summary>
            public DynamicQueryBuilder Skip(int count)
            {
                _skip = count;
                return this;
            }

            /// <summary>
            /// 分页：获取指定数量的记录
            /// </summary>
            public DynamicQueryBuilder Take(int count)
            {
                _take = count;
                return this;
            }

            /// <summary>
            /// 执行查询并返回动态结果集
            /// </summary>
            public List<dynamic> ToList()
            {
                var sqlBuilder = new StringBuilder($"SELECT * FROM {_tableName}");

                // 添加WHERE条件
                if (!string.IsNullOrEmpty(_whereClause))
                {
                    sqlBuilder.Append($" WHERE {_whereClause}");
                }

                // 添加排序
                if (!string.IsNullOrEmpty(_orderBy))
                {
                    sqlBuilder.Append($" ORDER BY {_orderBy}");
                }

                // 添加分页
                if (_skip.HasValue)
                {
                    sqlBuilder.Append($" LIMIT {_take ?? int.MaxValue} OFFSET {_skip}");
                }
                else if (_take.HasValue)
                {
                    sqlBuilder.Append($" LIMIT {_take}");
                }

                // 执行查询
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand(sqlBuilder.ToString(), connection))
                    {
                        command.Parameters.AddRange(_parameters.ToArray());

                        using (var reader = command.ExecuteReader())
                        {
                            var result = new List<dynamic>();
                            while (reader.Read())
                            {
                                dynamic item = new ExpandoObject();
                                var properties = (IDictionary<string, object>)item;

                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string columnName = reader.GetName(i);
                                    object value = reader.GetValue(i);
                                    properties[columnName] = value == DBNull.Value ? null : value;
                                }

                                result.Add(item);
                            }
                            return result;
                        }
                    }
                }
            }

            /// <summary>
            /// 执行查询并返回第一行数据
            /// </summary>
            public dynamic FirstOrDefault()
            {
                return Take(1).ToList().FirstOrDefault();
            }

            /// <summary>
            /// 计算查询结果总数
            /// </summary>
            public int Count()
            {
                var sqlBuilder = new StringBuilder($"SELECT COUNT(*) FROM {_tableName}");

                if (!string.IsNullOrEmpty(_whereClause))
                {
                    sqlBuilder.Append($" WHERE {_whereClause}");
                }

                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand(sqlBuilder.ToString(), connection))
                    {
                        command.Parameters.AddRange(_parameters.ToArray());
                        return Convert.ToInt32(command.ExecuteScalar());
                    }
                }
            }
        }
        #endregion
        #region SQLite 模拟 db.Queryable<T>() 风格
        /// <summary>
        /// 获取实体的查询对象
        /// </summary>
        public SqliteQueryable<T> Queryable<T>() where T : new()
        {
            return new SqliteQueryable<T>(_connectionString);
        }

        /// <summary>
        /// 连表查询信息
        /// </summary>
        public class JoinInfo
        {
            /// <summary>
            /// 连接的实体类型
            /// </summary>
            public Type JoinEntityType { get; set; }

            /// <summary>
            /// 连接类型（INNER JOIN / LEFT JOIN）
            /// </summary>
            public JoinType JoinType { get; set; }

            /// <summary>
            /// 连接条件表达式
            /// </summary>
            public LambdaExpression OnExpression { get; set; }

            /// <summary>
            /// 连接表别名（如 "c" 代表 Class 表）
            /// </summary>
            public string Alias { get; set; }
        }
        /// <summary>
        /// SQLite 查询对象，支持链式调用
        /// </summary>
        public class SqliteQueryable<T> where T : new()
        {
            private readonly string _connectionString;
            //where条件
            private Expression<Func<T, bool>> _whereExpression;
            private string _orderBy;
            private bool _isDescending;
            // 新增：存储连表信息
            private List<JoinInfo> _joinInfos = new List<JoinInfo>();
            private string _mainAlias = "t1";
            // 新增：投影表达式（用于连表查询的结果选择）
            private LambdaExpression _selectExpression;

            public SqliteQueryable(string connectionString)
            {
                _connectionString = connectionString;
            }

            /// <summary>
            /// 添加查询条件
            /// </summary>
            public SqliteQueryable<T> Where(Expression<Func<T, bool>> whereExpression)
            {
                _whereExpression = whereExpression;
                return this;
            }

            /// <summary>
            /// 添加排序条件
            /// </summary>
            public SqliteQueryable<T> OrderBy(string orderBy, bool isDescending = false)
            {
                _orderBy = orderBy;
                _isDescending = isDescending;
                return this;
            }
            /// <summary>
            ///  执行内连接查询（Inner Join）
            /// </summary>
            /// <typeparam name="TJoin"></typeparam>
            /// <param name="onExpression"></param>
            /// <param name="alias"></param>
            /// <returns></returns>
            public SqliteQueryable<T> InnerJoin<TJoin>(
                Expression<Func<T, TJoin, bool>> onExpression,
                string alias = "t2") where TJoin : new()
            {
                _joinInfos.Add(new JoinInfo
                {
                    JoinEntityType = typeof(TJoin),
                    JoinType = JoinType.Inner,
                    OnExpression = onExpression,
                    Alias = alias
                });
                return this;
            }
            /// <summary>
            ///  执行左连接查询（Left Join）
            /// </summary>
            /// <typeparam name="TJoin"></typeparam>
            /// <param name="onExpression"></param>
            /// <param name="alias"></param>
            /// <returns></returns>
            public SqliteQueryable<T> LeftJoin<TJoin>(
                Expression<Func<T, TJoin, bool>> onExpression,
                string alias = "t2") where TJoin : new()
            {
                _joinInfos.Add(new JoinInfo
                {
                    JoinEntityType = typeof(TJoin),
                    JoinType = JoinType.Left,
                    OnExpression = onExpression,
                    Alias = alias
                });
                return this;
            }
        /// <summary>
        /// 新增：连表查询结果投影（支持多表字段选择）
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="selectExpression"></param>
        /// <returns></returns>
    public SqliteQueryable<T> Select<T1, TResult>(
        Expression<Func<T, T1, TResult>> selectExpression)
    {
        // 注：object 占位，实际会根据连表数量动态匹配
        _selectExpression = selectExpression;
        return this;
    }
            /// <summary>
            /// 改造 ToList 方法，支持连表 SQL 生成
            /// </summary>
            /// <typeparam name="TResult"></typeparam>
            /// <returns></returns>
            public List<TResult> ToList<TResult>()
            {
                // 1. 获取主表和连表的表名
                string mainTableName = GetTableName(typeof(T));
                var joinTables = new Dictionary<string, string>(); // 别名 -> 表名
                foreach (var join in _joinInfos)
                {
                    joinTables[join.Alias] = GetTableName(join.JoinEntityType);
                }

                // 2. 构建 FROM 子句（含连表）
                string fromClause = $"FROM {mainTableName} AS {_mainAlias}";
                foreach (var join in _joinInfos)
                {
                    string joinType = join.JoinType == JoinType.Inner ? "INNER JOIN" : "LEFT JOIN";
                    fromClause += $" {joinType} {joinTables[join.Alias]} AS {join.Alias} ON {ParseJoinCondition(join)}";
                }
                // 3. 构建 SELECT 子句（支持去重和多表字段）
                string selectClause = "SELECT *"; // 默认查所有字段，可通过 Select 方法自定义
                if (_selectExpression != null)
                {
                    selectClause = $"SELECT {ExpressionToSqlConverter.ConvertSelect(_selectExpression, _mainAlias, _joinInfos)}";
                }

                // 4. 构建完整 SQL（拼接 WHERE、ORDER BY）
                string sql = $"{selectClause} {fromClause}";
                var parameters = new List<SQLiteParameter>();

                // 处理 WHERE 条件（支持多表字段筛选）
                if (_whereExpression != null)
                {
                    var whereClause = ExpressionToSqlConverter.ConvertWhere(_whereExpression, parameters, _mainAlias);
                    sql += $" WHERE {whereClause}";
                }
       

                // 处理排序（支持连表字段排序）
                if (!string.IsNullOrEmpty(_orderBy))
                {
                    sql += $" ORDER BY {_orderBy} {(_isDescending ? "DESC" : "ASC")}";
                }
                // 处理分组
    

                // 5. 执行查询（使用新的结果映射方法）
                return ExecuteJoinQuery<TResult>(sql, parameters.ToArray());
            }


            // 新增：解析连表条件表达式（如 s.ClassId == c.Id）
            private string ParseJoinCondition(JoinInfo join)
            {
                return ExpressionToSqlConverter.ConvertJoinCondition(join.OnExpression, _mainAlias, join.Alias);
            }
            // 新增：执行连表查询并映射结果
            private List<TResult> ExecuteJoinQuery<TResult>(string sql, SQLiteParameter[] parameters)
            {
                var result = new List<TResult>();
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddRange(parameters);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // 动态映射多表查询结果到 TResult（支持匿名类型或自定义类）
                                var entity = Activator.CreateInstance<TResult>();
                                MapJoinResultToEntity(reader, entity);
                                result.Add(entity);
                            }
                        }
                    }
                }
                return result;
            }
            private void MapJoinResultToEntity<TResult>(SQLiteDataReader reader, TResult entity)
            {
                foreach (var property in typeof(TResult).GetProperties())
                {
                    try
                    {
                        int ordinal = reader.GetOrdinal(property.Name);
                        if (!reader.IsDBNull(ordinal))
                        {
                            var value = reader.GetValue(ordinal);
                            property.SetValue(entity, Convert.ChangeType(value, property.PropertyType));
                        }
                    }
                    catch (IndexOutOfRangeException)
                    {
                        continue; // 忽略不存在的字段
                    }
                }
            }



            /// <summary>
            /// 表达式转 SQL 工具类
            /// </summary>
            public static class ExpressionToSqlConverter
            {

                // 新增：解析连表条件（如 s.ClassId == c.Id -> t1.ClassId = t2.Id）
                public static string ConvertJoinCondition(LambdaExpression expression, string mainAlias, string joinAlias)
                {
                    var binaryExpr = expression.Body as BinaryExpression;
                    if (binaryExpr == null) return "1=1";

                    // 左侧为主表字段（如 s.ClassId -> t1.ClassId）
                    string left = GetJoinExpressionText(binaryExpr.Left, mainAlias, expression.Parameters[0].Type);
                    // 右侧为连表字段（如 c.Id -> t2.Id）
                    string right = GetJoinExpressionText(binaryExpr.Right, joinAlias, expression.Parameters[1].Type);

                    return $"{left} {GetOperator(binaryExpr.NodeType)} {right}";
                }
                // 新增：解析多表字段表达式（带别名）
                private static string GetJoinExpressionText(Expression expr, string alias, Type targetType)
                {
                    if (expr is MemberExpression memberExpr && memberExpr.Member.DeclaringType == targetType)
                    {
                        // 字段带表别名（如 t1.ClassId）
                        return $"{alias}.{memberExpr.Member.Name}";
                    }
                    return expr.ToString();
                }
                // 新增：解析连表查询的 SELECT 投影（如 (s,c)=>new{ s.Name, c.Grade }）
                public static string ConvertSelect(LambdaExpression selectExpression, string mainAlias, List<JoinInfo> joins)
                {
                    // 情况1：处理 new TwoTableResult() 无参构造（NewExpression）
                    if (selectExpression.Body is NewExpression newExpr && newExpr.Arguments.Count == 0)
                    {
                        return GetAllFieldsSql(mainAlias, joins);
                    }

                    // 情况2：处理 new TwoTableResult() {} 空初始化器（MemberInitExpression）
                    if (selectExpression.Body is MemberInitExpression memberInits &&
                        memberInits.Bindings.Count == 0) // 没有任何属性赋值
                    {
                        return GetAllFieldsSql(mainAlias, joins);
                    }

                    // 简化实现：提取投影字段并添加表别名
                    var memberInit = selectExpression.Body as MemberInitExpression;
                    if (memberInit == null) return "*";

                    var columns = new List<string>();
                    foreach (var binding in memberInit.Bindings.Cast<MemberAssignment>())
                    {
                        // 解析字段所属表并添加别名（如 s.Name -> t1.Name）
                        var memberExpr = binding.Expression as MemberExpression;
                        if (memberExpr == null) continue;

                        // 匹配主表或连表
                        string alias = mainAlias; // 默认主表
                        foreach (var join in joins)
                        {
                            if (memberExpr.Member.DeclaringType == join.JoinEntityType)
                            {
                                alias = join.Alias;
                                break;
                            }
                        }

                        columns.Add($"{alias}.{memberExpr.Member.Name} AS {binding.Member.Name}");
                    }
         
                    return string.Join(", ", columns);
                }
                // 提取公共方法：生成所有表的*字段SQL
                private static string GetAllFieldsSql(string mainAlias, List<JoinInfo> joins)
                {
                    var allTables = new List<string> { mainAlias };
                    allTables.AddRange(joins.Select(j => j.Alias));
                    return string.Join(", ", allTables.Select(alias => $"{alias}.*"));
                }
                // 新增：解析多表 WHERE 条件（支持主表和连表字段）
                public static string ConvertWhere<T>(Expression<Func<T, bool>> expression, List<SQLiteParameter> parameters, string mainAlias)
                {
                    // 逻辑类似单表解析，但字段需带主表别名（如 t1.Id > 0）
                    var binaryExpr = expression.Body as BinaryExpression;
                    if (binaryExpr == null) return "1=1";

                    string left = GetWhereExpressionText(binaryExpr.Left, mainAlias);
                    string right = GetWhereExpressionText(binaryExpr.Right, mainAlias);
                    string op = GetOperator(binaryExpr.NodeType);

                    // 处理常量参数化
                    if (binaryExpr.Right is ConstantExpression constant)
                    {
                        string paramName = $"@p{parameters.Count}";
                        parameters.Add(new SQLiteParameter(paramName, constant.Value));
                        return $"{left} {op} {paramName}";
                    }

                    return $"{left} {op} {right}";
                }
                private static string GetWhereExpressionText(Expression expr, string alias)
                {
                    if (expr is MemberExpression memberExpr)
                    {
                        return $"{alias}.{memberExpr.Member.Name}"; // 带主表别名
                    }
                    return expr.ToString();
                }
                private static string ProcessBinaryExpression(BinaryExpression expr, List<SQLiteParameter> parameters)
                {
                    string left = GetExpressionText(expr.Left);
                    string right = GetExpressionText(expr.Right);
                    string op = GetOperator(expr.NodeType);

                    // 处理常量值，使用参数化查询
                    if (expr.Right is ConstantExpression constant)
                    {
                        string paramName = $"@p{parameters.Count}";
                        parameters.Add(new SQLiteParameter(paramName, constant.Value ?? DBNull.Value));
                        return $"{left} {op} {paramName}";
                    }

                    // 处理复杂表达式（如 AndAlso、OrElse）
                    if (expr.NodeType == ExpressionType.AndAlso)
                    {
                        return $"({ProcessBinaryExpression((BinaryExpression)expr.Left, parameters)} AND {ProcessBinaryExpression((BinaryExpression)expr.Right, parameters)})";
                    }
                    if (expr.NodeType == ExpressionType.OrElse)
                    {
                        return $"({ProcessBinaryExpression((BinaryExpression)expr.Left, parameters)} OR {ProcessBinaryExpression((BinaryExpression)expr.Right, parameters)})";
                    }

                    return $"{left} {op} {right}";
                }

                private static string GetExpressionText(Expression expr)
                {
                    if (expr is MemberExpression memberExpr)
                    {
                        // 检查是否有 ColumnAttribute
                        var columnAttr = memberExpr.Member.GetCustomAttribute<ColumnAttribute>();
                        return columnAttr?.Name ?? memberExpr.Member.Name;
                    }
                    return expr.ToString();
                }

                private static string GetOperator(ExpressionType type)
                {
                    switch (type)
                    {
                        case ExpressionType.Equal: return "=";
                        case ExpressionType.NotEqual: return "<>";
                        case ExpressionType.GreaterThan: return ">";
                        case ExpressionType.GreaterThanOrEqual: return ">=";
                        case ExpressionType.LessThan: return "<";
                        case ExpressionType.LessThanOrEqual: return "<=";
                        case ExpressionType.AndAlso: return "AND";
                        case ExpressionType.OrElse: return "OR";
                        default: return "=";
                    }
                }
            }
        }
            #endregion
           #region 数据库操作
            /// <summary>
            /// 创建数据库
            /// </summary>
            /// <param name="binPath"></param>
            public static void CreateNewDatabase(string binPath)
        {
            try
            {

                string dbPath = Path.Combine(binPath, "MyDatabase.db");
                // 确保目录存在（如果路径包含子目录，需先创建）
                string dbDirectory = Path.GetDirectoryName(dbPath);
                if (!Directory.Exists(dbDirectory))
                {
                    Directory.CreateDirectory(dbDirectory);
                }
                SQLiteConnection.CreateFile(dbPath);
                Console.WriteLine("数据库创建成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建失败：{ex.Message}");
                // 查看内部异常（如果有）
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"内部错误：{ex.InnerException.Message}");
                }
            }
        }
        /// <summary>
        /// 自动创建表（基于实体类属性）
        /// </summary>
        /// <param name="_tableName"></param>
        /// <param name="_properties"></param>
        public void EnsureTableExists(string _tableName, PropertyInfo[] _properties)
        {
            if (string.IsNullOrEmpty(_tableName)) return;

            var columnDefinitions = new List<string>();
            foreach (var property in _properties)
            {
                string columnType = GetSqliteType(property.PropertyType, false);
                columnDefinitions.Add($"[{property.Name}] {columnType}");
            }

            string createTableSql = $"CREATE TABLE IF NOT EXISTS [{_tableName}] (" +
                                   string.Join(", ", columnDefinitions) +
                                   ");";

            ExecuteNonQuery(createTableSql);
        }
        /// <summary>
        /// 查询所有记录(包括列注释)
        /// </summary>
        /// <param name="sql"></param> 
        /// <returns></returns>
        public DataTable GetAllGrades(string sql, params SQLiteParameter[] parameters)
        {
            var dataTable = new DataTable();
            using (var connection = new SQLiteConnection(_connectionString))
            using (var command = new SQLiteCommand(sql, connection))
            {
                connection.Open();
                command.Parameters.AddRange(parameters);

                using (var adapter = new SQLiteDataAdapter(command))
                {
                    adapter.Fill(dataTable);
                }
                // 2. 查询表的创建语句（提取注释）
                string createSql = "";
                //获取from后的表名（单表查询）
                string pattern = @"(?i)\bfrom\s+(\w+)"; Match match = Regex.Match(sql, pattern);
                string tableName = match.Groups[1].Value;
                using (SQLiteCommand cmd = new SQLiteCommand("SELECT sql FROM sqlite_master WHERE type='table' AND name=@TableName", connection))
                {
                    cmd.Parameters.AddWithValue("@TableName", tableName);
                    createSql = cmd.ExecuteScalar()?.ToString() ?? "";
                }
                var columnComments = ParseSqliteColumnComments(createSql);
                // 4. 关联注释到 DataTable 列
                foreach (var kvp in columnComments)
                {
                    if (dataTable.Columns.Contains(kvp.Key))
                    {
                        dataTable.Columns[kvp.Key].Caption = kvp.Value;
                    }
                }
            }
            return dataTable;

        }
        /// <summary>
        /// 执行查询并返回 DataTable
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public DataTable ExecuteQuery(string sql, params SQLiteParameter[] parameters)
        {
            var dataTable = new DataTable();
            using (var connection = new SQLiteConnection(_connectionString))
            using (var command = new SQLiteCommand(sql, connection))
            {
                connection.Open();
                command.Parameters.AddRange(parameters);

                using (var adapter = new SQLiteDataAdapter(command))
                {
                    adapter.Fill(dataTable);
                }

            }
            return dataTable;
        }
        /// <summary>
        /// 非查询(增删改)
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public int ExecuteNonQuery(string sql, params SQLiteParameter[] parameters)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            using (var command = new SQLiteCommand(sql, connection))
            {
                connection.Open();
                command.Parameters.AddRange(parameters);
                return command.ExecuteNonQuery();
            }
        }
        /// <summary>
        /// 批量生成实体类代码自带特性Table
        /// </summary>
        /// <param name="tables"></param>
        /// <param name="namespaceName"></param>
        /// <param name="outputDirectory"></param>
        public static void GenerateEntitiesFromDataTables(
          Dictionary<string, DataTable> tables,
          string namespaceName,
          string outputDirectory)
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }
            foreach (var kvp in tables)
            {
                string tableName = kvp.Key;
                DataTable dataTable = kvp.Value;

                string entityCode = GenerateEntityCode(dataTable, tableName, namespaceName);
                ;
                string fileName = $"{CamelToPascalCase((SnakeCaseToCamelCase(tableName)))}.cs";
                string filePath = Path.Combine(outputDirectory, fileName);

                File.WriteAllText(filePath, entityCode, Encoding.UTF8);
                Console.WriteLine($"生成实体类: {fileName}");
            }
        }
        /// <summary>
        ///  获取数据库中所有表名(去掉系统表)
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, DataTable> GetAllTableNames()
        {
            var tableNames = new List<string>();
            tableNames = ExecuteQuery("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'").AsEnumerable().Select(row => row.Field<string>(0)).ToList();
            //var dataTable = new DataTable();
            var tableNamesDict = new Dictionary<string, DataTable>();
            foreach (var tableName in tableNames)
            {
                var dataTable = ExecuteQuery($"SELECT * FROM {tableName}");
                // 2. 查询表的创建语句（提取注释）
                string createSql = "";
                //获取from后的表名
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand("SELECT sql FROM sqlite_master WHERE type='table' AND name=@TableName", connection))
                    {
                        cmd.Parameters.AddWithValue("@TableName", tableName);
                        createSql = cmd.ExecuteScalar()?.ToString() ?? "";
                    }

                }



                var columnComments = ParseSqliteColumnComments(createSql);
                // 4. 关联注释到 DataTable 列
                foreach (var kvp in columnComments)
                {
                    if (dataTable.Columns.Contains(kvp.Key))
                    {
                        dataTable.Columns[kvp.Key].Caption = kvp.Value;
                    }
                }
                tableNamesDict.Add(tableName, dataTable);
            }
            return tableNamesDict;
        }

        /// <summary>
        /// 根据DataTable结构生成实体类代码并保存到指定路径
        /// </summary>
        /// <param name="table">数据表</param>
        /// <param name="className">实体类名称</param>
        /// <param name="namespaceName">命名空间名称</param>
        /// <param name="outputDirectory">输出目录路径</param>
        /// <returns>生成的文件路径</returns>
        public string GenerateEntityFromDataTable(
        DataTable table,
        string className,
        string namespaceName,
        string outputDirectory)
        {
            // 确保输出目录存在
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // 生成实体类代码
            string entityCode = GenerateEntityCode(table, className, namespaceName);

            // 构建完整文件路径
            string fileName = $"{CamelToPascalCase((SnakeCaseToCamelCase(className)))}.cs";
            string filePath = Path.Combine(outputDirectory, fileName);

            // 将代码写入文件
            File.WriteAllText(filePath, entityCode, Encoding.UTF8);

            return filePath;
        }
        /// <summary>
        /// 根据DataTable结构生成实体类代码
        /// </summary>
        /// <param name="table">数据表</param>
        /// <param name="className">实体类名称</param>
        /// <param name="namespaceName">命名空间名称</param>
        /// <returns>生成的C#代码</returns>
        public static string GenerateEntityCode(DataTable table, string className, string namespaceName)
        {
            var code = new StringBuilder();

            // 生成引用命名空间
            code.AppendLine("using System;");
            code.AppendLine("using System.Text;");
            code.AppendLine("using static SocketTest.Helper.Utilities;");
            code.AppendLine(" using SQLite;");
            code.AppendLine();

            // 生成命名空间
            code.AppendLine($"namespace {namespaceName}");
            code.AppendLine("{");

            // 生成类定义
            code.AppendLine($"    [Table(\"{className}\")] ");
            code.AppendLine($"    public class {CamelToPascalCase((SnakeCaseToCamelCase(className)))}");
            code.AppendLine("    {");

            // 遍历所有列生成属性
            foreach (DataColumn column in table.Columns)
            {
                // 转换数据类型（DataTable类型 -> C#类型）
                string csType = GetSqliteType(column.DataType, true);
                bool hasChinese = ContainsChinese(column.Caption);
                if (hasChinese)
                {
                    // 字符串类型需要添加长度限制
                    // 生成属性注释
                    // 正确转义双引号，确保生成的特性参数包含引号
                    // 添加 $ 启用字符串插值，确保 {column.Caption} 被解析为变量
                    code.AppendLine($"[ExcelColumnDescription(\"{column.Caption}\")]     ");
                    code.AppendLine("        /// <summary>");
                    code.AppendLine($"        /// {column.ColumnName}{column.Caption}");
                    code.AppendLine("        /// </summary>");

                    // 为主键添加Key特性
                    if (table.PrimaryKey != null && table.PrimaryKey.Contains(column))
                    {
                        code.AppendLine("        [Key]");
                    }

                    // 为字符串类型添加长度限制（如果有）
                    if (csType == "string" && column.MaxLength > 0 && column.MaxLength != int.MaxValue)
                    {
                        code.AppendLine($"        [MaxLength({column.MaxLength})]");
                    }

                    // 生成属性
                    code.AppendLine($"        public {csType} {column.ColumnName} {{ get; set; }}");
                    code.AppendLine();
                }
                else
                {
                    // 生成属性注释
                    code.AppendLine("        /// <summary>");
                    code.AppendLine($"        /// {column.ColumnName}");
                    code.AppendLine("        /// </summary>");

                    // 为主键添加Key特性
                    if (table.PrimaryKey != null && table.PrimaryKey.Contains(column))
                    {
                        code.AppendLine("        [Key]");
                    }

                    // 为字符串类型添加长度限制（如果有）
                    if (csType == "string" && column.MaxLength > 0 && column.MaxLength != int.MaxValue)
                    {
                        code.AppendLine($"        [MaxLength({column.MaxLength})]");
                    }

                    // 生成属性
                    code.AppendLine($"        public {csType} {column.ColumnName} {{ get; set; }}");
                    code.AppendLine();
                }

            }

            code.AppendLine("    }");
            code.AppendLine("}");

            return code.ToString();
        }
        #endregion
        #region 实体管理通用方法
        /// <summary>
        /// 带参数的通用查询方法（返回符合条件的实体列表）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="condition">查询条件（如 "Name = @Name AND Age > @Age"）</param>
        /// <param name="parameters">参数对象（属性名对应条件中的@参数名，如 new { Name = "张三", Age = 20 }）</param>
        /// <returns>实体列表</returns>
        public List<T> QueryEntities<T>(string condition = "", object parameters = null) where T : class, new()
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    // 1. 获取类型和表名（类名小写）
                    Type entityType = typeof(T);
                    string tableName = GetTableName(entityType);
                    // 2. 获取实体所有可写属性（用于映射结果）
                    PropertyInfo[] properties = entityType.GetProperties(
                        BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.CanWrite) // 只处理可写属性
                        .ToArray();
                    // 处理包含order的列：添加单引号
                    var columnNames = properties.Select(p => p.Name.ToLower()).ToList();
                    var escapedColumns = columnNames.Select(col =>
                        col.Equals("order", StringComparison.OrdinalIgnoreCase)
                            ? $"'{col}'"  // 对order列添加单引号
                            : col
                    );
                    // 3. 构建查询SQL（查询所有属性对应的列）
                    string columns = string.Join(", ", escapedColumns); // 列名小写，与表字段对应
                    string selectQuery = $"SELECT {columns} FROM {tableName}";
                    // 4. 添加查询条件（如果有）
                    if (!string.IsNullOrEmpty(condition))
                    {
                        selectQuery += $" WHERE {condition}";
                    }

                    // 5. 执行查询
                    using (SQLiteCommand command = new SQLiteCommand(selectQuery, connection))
                    {
                        // 6. 添加参数（处理参数对象）
                        if (parameters != null)
                        {
                            foreach (PropertyInfo paramProp in parameters.GetType().GetProperties())
                            {
                                string paramName = $"@{paramProp.Name}"; // 参数名格式：@属性名
                                object paramValue = paramProp.GetValue(parameters) ?? DBNull.Value; // 处理null
                                command.Parameters.AddWithValue(paramName, paramValue);
                            }
                        }

                        // 7. 读取结果并映射到实体
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            List<T> entities = new List<T>();

                            while (reader.Read())
                            {
                                T entity = new T(); // 实例化实体

                                // 遍历实体属性，映射查询结果
                                foreach (PropertyInfo prop in properties)
                                {
                                    string columnName = prop.Name; // 列名小写，与查询的columns对应
                                    int columnIndex;

                                    try
                                    {
                                        columnIndex = reader.GetOrdinal(columnName); // 获取列索引
                                    }
                                    catch (IndexOutOfRangeException)
                                    {
                                        // 表中无此列，跳过（避免查询的列与实体属性不匹配）
                                        continue;
                                    }

                                    // 处理空值
                                    if (reader.IsDBNull(columnIndex))
                                    {
                                        prop.SetValue(entity, null);
                                        continue;
                                    }

                                    // 读取数据库值并处理类型转换（解决Int64→Int32等问题）
                                    object dbValue = reader.GetValue(columnIndex);
                                    Type targetType = prop.PropertyType;

                                    // 处理可空类型（如 int? → int）
                                    if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                    {
                                        targetType = Nullable.GetUnderlyingType(targetType);
                                    }

                                    // 特殊类型转换
                                    object convertedValue = ConvertDbValue(dbValue, targetType);
                                    prop.SetValue(entity, convertedValue);
                                }

                                entities.Add(entity);
                            }

                            return entities;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"查询实体异常：条件={condition}，参数={parameters}", ex.Message);
                throw; // 可根据需要决定是否抛出或吞掉异常
            }
        }

        /// <summary>
        /// 插入实体用特[AutoIncrement]标记自增主键
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        public void InsertEntity<T>(T entity, bool isTransactionActive = false)
        {
            try
            {
                Type entityType = typeof(T);
                string tableName = GetTableName(entityType);// 表名（可结合TableAttribute优化）

                // 获取所有属性，并筛选出非自增主键的属性
                PropertyInfo[] allProperties = entityType.GetProperties();
                List<PropertyInfo> insertProperties = new List<PropertyInfo>();

                foreach (var prop in allProperties)
                {
                    // 检查属性是否标记了自增特性，若是则排除
                    if (prop.GetCustomAttribute<SQLite.AutoIncrementAttribute>() == null)
                    {
                        insertProperties.Add(prop);
                    }
                }

                // 构建插入SQL（仅包含非自增字段）
                string columns = string.Join(", ", insertProperties.Select(p => p.Name.ToLower()));
                string placeholders = string.Join(", ", insertProperties.Select(p => $"@{p.Name}"));
                string insertQuery = $"INSERT INTO {tableName} ({columns}) VALUES ({placeholders});";

                using (SQLiteCommand command = new SQLiteCommand(insertQuery, _connection))
                {
                    // 仅添加非自增字段的参数
                    foreach (PropertyInfo property in insertProperties)
                    {
                        object value = property.GetValue(entity);
                        // 处理null值
                        command.Parameters.AddWithValue($"@{property.Name}", value ?? DBNull.Value);
                    }

                    // 绑定事务
                    if (isTransactionActive == true)
                    {
                        if (_transaction == null)
                            throw new InvalidOperationException("请先开启事务");
                        command.Transaction = _transaction;
                    }

                    // 执行插入
                    int rowsAffected = command.ExecuteNonQuery();
                    Log.Info($"{rowsAffected} 行受影响");


                }
            }
            catch (Exception ex)
            {
                Log.Error("插入数据异常:" + ex.Message);
                throw; // 抛出异常，确保事务能回滚
            }
        }
        /// <summary>
        /// 批量插入实体（List集合）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entities"></param>
        public void InsertEntities<T>(List<T> entities) where T : class
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    using (var transaction = connection.BeginTransaction())
                    {
                        Type entityType= typeof(T);
                        string tableName = GetTableName(entityType);// 表名（可结合TableAttribute优化）

                        // 获取所有公共实例属性
                        var properties = typeof(T).GetProperties(
                            BindingFlags.Public | BindingFlags.Instance);

                        // 检测主键属性（使用更全面的逻辑）
                        var primaryKeyProperty = properties
                            .FirstOrDefault(p =>
                                p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                                p.GetCustomAttributes(false).OfType<KeyAttribute>().Any());

                        // 排除主键属性
                        var propertiesToInsert = primaryKeyProperty != null
                            ? properties.Where(p => p != primaryKeyProperty).ToList()
                            : properties.ToList();

                        // 构建SQL语句
                        string columns = string.Join(", ", propertiesToInsert.Select(p => p.Name));
                        string parameters = string.Join(", ", propertiesToInsert.Select(p => "@" + p.Name));
                        string sql = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";

                        using (var command = new SQLiteCommand(sql, connection, transaction))
                        {
                            // 创建参数集合
                            var sqlParameters = propertiesToInsert.ToDictionary(
                                p => p.Name,
                                p => command.CreateParameter());

                            foreach (var param in sqlParameters.Values)
                            {
                                command.Parameters.Add(param);
                            }

                            foreach (var entity in entities)
                            {
                                // 设置参数值
                                foreach (var property in propertiesToInsert)
                                {
                                    sqlParameters[property.Name].ParameterName = "@" + property.Name;
                                    sqlParameters[property.Name].Value = property.GetValue(entity) ?? DBNull.Value;
                                }

                                command.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {

                Log.Error("批量插入数据异常:" + ex.Message);
            }
        }
        /// <summary>
        /// 删除实体 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">删除参数</param>
        /// <param name="primaryKeyName">一般为主键（也可以是条件）</param>
        public void DeleteEntity<T>(object primaryKeyNameValue = null, string primaryKeyName = null, bool deleteTable = false)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    // 获取类型信息
                    Type entityType = typeof(T);
                    // 获取表名（假设表名与实体类名相同）
                    string tableName = GetTableName(entityType);
                    if (deleteTable == true || primaryKeyNameValue == null || primaryKeyName == null)
                    {
                        string sql = $"DELETE FROM {tableName} ";
                        using (SQLiteCommand command = new SQLiteCommand(sql, connection))
                        {
                            int result = command.ExecuteNonQuery();
                            Log.Info($"表 {tableName} 删除操作执行完成，结果: {result}");
                        }
                    }
                    else
                    {
                        // 构建删除SQL语句
                        string deleteQuery = $"DELETE FROM {tableName} WHERE {primaryKeyName} = @primaryKeyNameValue";

                        using (SQLiteCommand command = new SQLiteCommand(deleteQuery, connection))
                        {
                            // 添加参数
                            command.Parameters.AddWithValue("@primaryKeyNameValue", primaryKeyNameValue);

                            // 执行删除操作
                            int rowsAffected = command.ExecuteNonQuery();
                            Log.Info($"{rowsAffected} 行受影响");
                        }
                    }



                }
            }
            catch (Exception ex)
            {
                Log.Error("删除数据异常:" + ex.Message);
            }
        }
        /// <summary>
        /// 修改实体 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        public void UpdateEntity<T>(
    T entity,
    bool isTransactionActive = false,
    string whereClause = null,
    Dictionary<string, object> whereParameters = null
)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity), "实体不能为null");

                Type entityType = typeof(T);
                string tableName = GetTableName(entityType);

                // 1. 获取主键属性
                var primaryKeyProp = GetPrimaryKeyProperty(entityType)
                    ?? entityType.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);

                if (primaryKeyProp == null && string.IsNullOrEmpty(whereClause))
                    throw new InvalidOperationException($"实体 {entityType.Name} 未找到主键，且未提供自定义条件");

                // 2. 关键：获取实体中“显式赋值的字段”（排除主键、未映射字段、默认值字段）
                var explicitlySetProps = GetExplicitlySetProperties(entity, primaryKeyProp);
                if (explicitlySetProps.Count == 0)
                {
                    Log.Warning("没有显式赋值的字段，跳过更新");
                    return;
                }

                // 3. 构建SET子句（只包含显式赋值的字段）
                string setClause = string.Join(", ", explicitlySetProps.Select(p =>
                    $"{EscapeSqlIdentifier(p.Name.ToLower())} = @{p.Name}"));

                // 4. 构建WHERE子句
                string whereCondition = !string.IsNullOrEmpty(whereClause)
                    ? whereClause
                    : $"{EscapeSqlIdentifier(primaryKeyProp.Name.ToLower())} = @{primaryKeyProp.Name}";

                // 5. 完整SQL（此时SET子句只有 max_num）
                string updateQuery = $"UPDATE {EscapeSqlIdentifier(tableName)} SET {setClause} WHERE {whereCondition}";
                Log.Debug($"执行的SQL: {updateQuery}");

                using (SQLiteCommand command = new SQLiteCommand(updateQuery, _connection))
                {
                    // 6. 添加SET子句参数（只加显式赋值的字段）
                    foreach (var prop in explicitlySetProps)
                    {
                        string paramName = $"@{prop.Name}";
                        object value = prop.GetValue(entity) ?? DBNull.Value;
                        command.Parameters.AddWithValue(paramName, value);
                    }

                    // 7. 添加WHERE子句参数（避免与SET参数冲突）
                    if (whereParameters != null)
                    {
                        foreach (var param in whereParameters)
                        {
                            string paramName = $"@{param.Key}";
                            if (command.Parameters.Contains(paramName))
                                throw new InvalidOperationException($"参数名冲突：{paramName} 同时存在于SET和WHERE子句");
                            command.Parameters.AddWithValue(paramName, param.Value ?? DBNull.Value);
                        }
                    }
                    // 主键条件参数（兼容原有逻辑）
                    else if (primaryKeyProp != null)
                    {
                        command.Parameters.AddWithValue($"@{primaryKeyProp.Name}", primaryKeyProp.GetValue(entity) ?? DBNull.Value);
                    }

                    // 8. 事务绑定
                    if (isTransactionActive)
                    {
                        if (_transaction == null)
                            throw new InvalidOperationException("请先开启事务");
                        command.Transaction = _transaction;
                    }

                    // 9. 执行（只更新 max_num）
                    int rowsAffected = command.ExecuteNonQuery();
                    Log.Info($"{rowsAffected} 行受影响");
                }
            }
            catch (Exception ex)
            {
                Log.Error("更新数据异常:" + ex.Message);
                throw;
            }
        }
        #endregion
        #region 辅助方法

        // 判断字符串是否包含中文字符
        public static bool ContainsChinese(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            // 正则表达式匹配中文字符（\u4e00-\u9fa5 是基本汉字范围）
            return Regex.IsMatch(input, @"[\u4e00-\u9fa5]");
        }
        // 重点优化：转义SQL标识符的方法（针对SQLite）
        private static string EscapeSqlIdentifier(string identifier)
        {
            // SQLite要求用双引号包裹关键字作为标识符
            // 例如将 order 转为 "order"
            return $"\"{identifier}\"";
        }
        /// <summary>
        /// 将下划线命名法（snake_case）转换为驼峰命名法（camelCase）
        /// </summary>
        /// <param name="snakeCaseName">下划线分隔的名称（如corner_flip_rule）</param>
        /// <returns>驼峰命名的结果（如cornerFlipRule）</returns>
        public static string SnakeCaseToCamelCase(string snakeCaseName)
        {
            // 处理空值或空字符串
            if (string.IsNullOrWhiteSpace(snakeCaseName))
                return snakeCaseName;

            // 分割下划线，过滤空项（处理连续下划线的情况）
            var parts = snakeCaseName.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);

            // 如果没有下划线，直接返回原字符串（首字母小写）
            if (parts.Length == 1)
                return parts[0].Length > 0
                    ? char.ToLowerInvariant(parts[0][0]) + parts[0].Substring(1)
                    : parts[0];

            // 第一个部分首字母小写，后续部分首字母大写，其余字母保持原样
            return parts[0].ToLowerInvariant() +
                   string.Concat(parts.Skip(1).Select(part =>
                       part.Length > 0
                           ? char.ToUpperInvariant(part[0]) + part.Substring(1)
                           : part));
        }
        /// <summary>
        /// 获取实体类的主键属性
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public static PropertyInfo GetPrimaryKeyProperty(Type entityType)
        {
            // 1. 优先查找带有[PrimaryKey]特性的公共实例属性
            var primaryKeyProp = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(prop =>
                    prop.IsDefined(typeof(SQLite.PrimaryKeyAttribute), inherit: false) // 检查是否有PrimaryKey特性
                );

            // 2. 如果没找到带特性的属性，查找名为"Id"的公共实例属性（不区分大小写）
            if (primaryKeyProp == null)
            {
                primaryKeyProp = entityType.GetProperty(
                    "Id",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase // 忽略大小写
                );
            }

            return primaryKeyProp;
        }
        /// <summary>
        /// 将驼峰命名法（camelCase）转换为帕斯卡命名法（PascalCase，首字母大写）
        /// </summary>
        /// <param name="camelCase">驼峰命名的字符串（如 cornerFlipRule）</param>
        /// <returns>首字母大写的字符串（如 CornerFlipRule）</returns>
        public static string CamelToPascalCase(string camelCase)
        {
            if (string.IsNullOrWhiteSpace(camelCase))
                return camelCase;

            // 如果字符串长度为1，直接大写
            if (camelCase.Length == 1)
                return camelCase.ToUpperInvariant();

            // 首字母大写，拼接剩余部分
            return char.ToUpperInvariant(camelCase[0]) + camelCase.Substring(1);
        }
        /// <summary>
        /// 获取实体类的表名（优先使用Table特性，否则用类名）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string GetTableName(Type entityType)
        {
         

            // 1. 明确获取 SQLite.TableAttribute 类型的特性
            // 2. 转换时也明确指定 SQLite.TableAttribute，避免命名空间冲突
            SQLite.TableAttribute tableAttribute = entityType
                .GetCustomAttributes(typeof(SQLite.TableAttribute), inherit: false)
                .FirstOrDefault() as SQLite.TableAttribute; // 关键：显式指定完整类型

            // 如果有 [Table] 特性，则返回特性中指定的表名；否则返回类名
            return tableAttribute?.Name ?? entityType.Name;
        }
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            //1. 回滚未完成的事务（避免事务长期占用锁）
            if (_transaction != null)
            {
                _transaction.Rollback(); // 未提交的事务必须回滚
                _transaction.Dispose();
                _transaction = null;
            }
            // 2. 释放非托管资源（数据库连接等）
            if (_connection != null)
            {
                if (_connection.State == System.Data.ConnectionState.Open)
                {
                    _connection.Close(); // 关闭连接
                }
                _connection.Dispose(); // 释放连接资源
                _connection = null;
            }

            // 3. 调用 GC.SuppressFinalize(this) 防止派生类重复调用终结器
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 处理数据库值到目标类型的转换（解决常见类型不匹配问题）
        /// </summary>
        private static object ConvertDbValue(object dbValue, Type targetType)
        {
            if (dbValue == null || dbValue == DBNull.Value)
                return null;

            // SQLite整数默认返回long，转换为int/short等
            if (dbValue is long longValue)
            {
                if (targetType == typeof(int))
                    return Convert.ToInt32(longValue);
                if (targetType == typeof(short))
                    return Convert.ToInt16(longValue);
                if (targetType == typeof(byte))
                    return Convert.ToByte(longValue);
                if (targetType == typeof(bool))
                    return longValue != 0; // SQLite用0/1表示bool
            }

            // SQLite浮点数默认返回double，转换为float
            if (dbValue is double doubleValue && targetType == typeof(float))
                return Convert.ToSingle(doubleValue);

            // 其他类型默认转换
            return Convert.ChangeType(dbValue, targetType);
        }

        /// <summary>
        /// 定义连接类型枚举
        /// </summary>
        public enum JoinType
        {
            Inner,   // 内连接
            Left,    // 左连接
            Right    // 右连接
        }
        /// <summary>
        /// 类型映射
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetSqliteType(Type type, bool isNullable)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;


            if (isNullable == true)
            {
                if (type == typeof(int) || type == typeof(Int32))
                    return "int";
                if (type == typeof(long) || type == typeof(Int64))
                    return "long";
                if (type == typeof(string))
                    return "string";
                if (type == typeof(DateTime))
                    return "DateTime";
                if (type == typeof(bool))
                    return "bool";
                if (type == typeof(decimal))
                    return "decimal";
                if (type == typeof(double))
                    return "double";
                if (type == typeof(float))
                    return "float";
                if (type == typeof(byte[]))
                    return "byte[]";
                return "object";
            }
            else
            {
                if (type == typeof(int) || type == typeof(long) || type == typeof(bool))
                    return "INTEGER";
                if (type == typeof(double) || type == typeof(float))
                    return "REAL";
                if (type == typeof(DateTime))
                    return "DATETIME";
                return "TEXT";
            }

        }
        // 辅助方法1：获取实体中“显式赋值的字段”（核心逻辑）
        private static List<PropertyInfo> GetExplicitlySetProperties<T>(T entity, PropertyInfo primaryKeyProp)
        {
            Type entityType = typeof(T);
            var allProps = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p != primaryKeyProp && !IsNotMapped(p)) // 排除主键和未映射字段
                .ToList();

            var explicitlySetProps = new List<PropertyInfo>();
            var defaultEntity = Activator.CreateInstance<T>(); // 创建“默认值实体”（用于对比）

            foreach (var prop in allProps)
            {
                object currentValue = prop.GetValue(entity); // 当前实体的值
                object defaultValue = prop.GetValue(defaultEntity); // 字段默认值

                // 判断：当前值 != 默认值 → 视为“显式赋值”
                if (!Equals(currentValue, defaultValue))
                {
                    explicitlySetProps.Add(prop);
                }
            }

            return explicitlySetProps;
        }
        // 辅助方法2：判断字段是否标记“不映射到数据库”（避免更新内存字段）
        private static bool IsNotMapped(PropertyInfo prop)
        {
            return prop.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute>() != null;
        }
        /// <summary>
        /// 解析 SQLite 表创建语句中的字段注释
        /// </summary>
        /// <param name="createSql"></param>
        /// <returns></returns>
        private static Dictionary<string, string> ParseSqliteColumnComments(string createSql)
        {
            var comments = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(createSql)) return comments;

            // 标准化SQL语句，将多个空格替换为单个空格
            string normalizedSql = Regex.Replace(createSql, @"\s+", " ").Trim();
            // 正则表达式匹配字段和注释
            // 匹配格式："字段名" 类型 COMMENT '注释内容'
            string pattern = @"\""(\w+)\""\s+\w+\s+COMMENT\s*['\""](.*?)['\""]";
            MatchCollection matches = Regex.Matches(normalizedSql, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {

                string fieldName = match.Groups[1].Value;
                string comment = match.Groups[2].Value;
                comments[fieldName] = comment;

            }

            return comments;
        }
        #endregion
        #region 事务管理方法
        /// <summary>
        /// 开启事务
        /// </summary>
        public void BeginTransaction()
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                _connection.Open(); // 确保连接处于打开状态
            }

            // 如果已有事务正在进行，先回滚旧事务
            if (_transaction != null)
            {
                _transaction.Rollback();
                _transaction.Dispose();
            }

            // 开启新事务（默认为Serializable隔离级别，可根据需要修改）
            _transaction = _connection.BeginTransaction();
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public void CommitTransaction()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("没有活跃的事务，请先调用BeginTransaction");
            }

            try
            {
                _transaction.Commit(); // 提交事务
                Log.Info("事务已成功提交");
            }
            finally
            {
                _transaction.Dispose(); // 释放事务资源
                _transaction = null;
            }
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        public void RollbackTransaction()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("没有活跃的事务，请先调用BeginTransaction");
            }

            try
            {
                _transaction.Rollback(); // 回滚事务
                Log.Info("事务已回滚");
            }
            finally
            {
                _transaction.Dispose(); // 释放事务资源
                _transaction = null;
            }
        }
        #endregion
 





    }



}







