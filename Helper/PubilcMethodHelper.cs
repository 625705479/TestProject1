using FTOptix.HMIProject;
using FTOptix.Store;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UAManagedCore;

namespace TestProject1.Helper
{
    public static class PubilcMethodHelper
    {
        /// <summary>
        /// 通用方法插入DataTable数据到Store
        /// </summary>
        /// <param name="tablename">表名</param>
        /// <param name="data">数据源</param>
        /// <param name="columnName">列名</param>
        /// <param name="fieldConverters">需要格式化的列 三元表达式</param>
        /// <returns></returns>
        public static bool InsertStore(string tablename, DataTable data, string[] columnName, Dictionary<string, Func<object, object>> fieldConverters = null)
        {
            Func<object, object> handleDBNull = value =>
                  value == DBNull.Value ? null : value;
            try
            {
                var store = Project.Current.Get<Store>("DataStores/EmbeddedDatabase1");
                string deleteSql = $"DELETE FROM {tablename}";
                string[] columnNames;
                object[,] results;
                store.Query(deleteSql, out columnNames, out results);
                for (int i = 0; i < data.Rows.Count; i++)
                {
                    var row = data.Rows[i];


                    object[,] values = new object[1, columnName.Length];
                    // 4. 循环赋值（基于字段列表索引，消除硬编码0-19）
                    for (int j = 0; j < columnName.Length; j++)
                    {
                        string fieldName = columnName[j];
                        // 对特殊字段使用转换后的值，其他字段直接取原始值
                        object value = row[fieldName];
                        if (fieldConverters != null && fieldConverters.ContainsKey(fieldName))
                        {
                            value = fieldConverters[fieldName](value);
                        }
                        else
                        {
                            value = handleDBNull(value);
                        }
                        // 统一处理DBNull
                        values[0, j] = handleDBNull(value);
                    }

                    // 执行插入（字段与值数组长度严格一致）
                    store.Insert(tablename, columnName, values);
                }
                   Logger.Info($"插入表 {tablename} 数据成功，共 {data.Rows.Count} 条记录");
                return true;
            }
            catch (Exception)
            {

                return false;
            }

        }


    }
}
