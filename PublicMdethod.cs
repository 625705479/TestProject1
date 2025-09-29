using FTOptix.HMIProject;
using S7.Net;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestProject1.Helper;
using UAManagedCore;

namespace TestProject1
{
    /// <summary>
    /// This is a public class
    /// </summary>
    /// 
    public class PublicMdethod
    {
        //定义一个通用的data
        private static readonly DataTable data = new DataTable();
        //定义一个字典 用来存储变量的名称和值
        private static Dictionary<string, object> variables = new Dictionary<string, object>();
        /// <summary>
        /// 添加报错变量
        /// </summary>
        public static void addVariables()
        {
            string formattedTime = System.DateTime.Now.ToString("[HH:mm:ss.fff]");

            var Object1 = Project.Current.GetObject("Model/Object1").Children.ToList();
            data.Columns.Add("name");
            data.Columns.Add("code");
            data.Rows.Add(formattedTime+":水果", "123");
            data.Rows.Add(formattedTime + ":蔬菜", "456");
            data.Rows.Add(formattedTime + ":肉类", "789");
            data.Rows.Add(formattedTime+":水产", "10111");
            data.Rows.Add(formattedTime+":农产品", "12131");
            SetEnumeration(data);
            Object1[0].FindVariable("水果").Value = "123";
        }
        /// <summary>
        /// 给枚举赋值
        /// </summary>
        /// <param name="dt"></param>

        public static void SetEnumeration(DataTable dt)
        {
            //获取枚举
            var AralmEnumeration = Project.Current.Get("Model/Enumeration1");
            #region 同步枚举值
            var enumChildren = AralmEnumeration.Children.ToList();
            var firstChild = enumChildren[0] as UAVariable;
            // 检查节点是否有效
            if (firstChild == null)
            {
                Logger.Error("无法获取有效的UAVariable节点");
                return;
            }
            uint statusCode = firstChild.DataValue.StatusCode;
            // 获取节点当前值（修正多余的.Value，避免类型错误）
            var nodeValue = firstChild.DataValue.Value.Value;

            string formattedTime = System.DateTime.Now.ToString("[HH:mm:ss.fff]");


            // 验证是否为结构体数组类型
            if (nodeValue is UAManagedCore.Struct[] structArray)
            {
                int loopCount = Math.Min(data.Rows.Count, structArray.Length);
                for (int i = 0; i < loopCount; i++)
                {
                    var currentStruct = structArray[i];
                    var valuesProperty = currentStruct.GetType().GetProperty("Values");
                    if (valuesProperty == null)
                    {
                        Logger.Warn($"第{i}个结构体没有Values属性，跳过");
                        continue;
                    }

                    var values = valuesProperty.GetValue(currentStruct) as object[];
                    if (values == null || values.Length < 2)
                    {
                        Logger.Warn($"第{i}个结构体的Values数组无效，跳过");
                        continue;
                    }

                    var localizedTextObj = values[1] as UAManagedCore.LocalizedText;
                    if (localizedTextObj == null)
                    {
                        Logger.Warn($"第{i}个Values[1]不是LocalizedText类型，跳过");
                        continue;
                    }

                    // 赋值操作
                    var row = data.Rows[i];
                    string name = row["name"]?.ToString() ?? "";
                    string code = row["code"]?.ToString() ?? "";
                    localizedTextObj.Text = $"{code}:{name}";
                    localizedTextObj.LocaleId = "zh-CN";
                }
                // 写回节点
                var uaValue = new UAManagedCore.UAValue(structArray);

                // 2. 使用正确的构造函数创建DataValue
                var updatedDataValue = new UAManagedCore.DataValue(
                    uaValue,  // 包装后的UAValue对象
                    statusCode: statusCode,  // 状态码（表示值有效）
                    DateTime.UtcNow,  // 源时间戳
                    DateTime.UtcNow   // 服务器时间戳
                );

                // 3. 写回节点
                firstChild.DataValue = updatedDataValue;


            }
            else
            {
                Logger.Error("节点值不是有效的Struct[]数组类型");
            }
            #endregion
        }


        /// <summary>
        /// 读取Redis数据
        /// </summary>
        public static async Task ReadRedis()
        {
            //获取字符串:
            //    var db = RedisExample.GetDatabase();
            //    string username = db.StringGet("username1");
            //    获取列表：
            //    RedisValue[] messages = db.ListRange("message");
            //    获取哈希表：
            //    string name = db.HashGet("hashkey", "name");
            //    HashEntry[] allFields = db.HashGetAll("hashkey");
            //    获取集合：
            //    RedisValue[] allValues = db.SetMembers("test");
            //    获取有序集合：
            //    RedisValue[] topUsers = db.SortedSetRangeByScore("product");
            //    double? score = db.SortedSetScore("product", "banana");
            //    string cachedJson = db.StringGet("product:1");
            //    var cachedProduct = JsonConvert.DeserializeObject<Product>(cachedJson);
            //    var productname = cachedProduct.name;
     

            // 1. 初始化连接（确保只初始化一次）
            RedisExample.GetConnection();

            // 2. 统一配置
            int dbIndex = 0;
            TimeSpan expiry = TimeSpan.FromMinutes(15);
            int maxParallelism = 4; // 控制并行操作数量

            // 3. 获取数据库连接（复用连接）
            var db = RedisExample.GetDatabase(dbIndex);

            // 4. 定义所有异步操作
            var operations = new List<Task>
    {
        // 字符串操作 - 使用原生异步方法
        db.StringSetAsync("username", "张三", expiry),
        
        // 列表操作
        Task.Run(async () =>
        {
            foreach (var msg in new[] { "消息1", "消息2", "消息3" })
            {
                await db.ListRightPushAsync("messages", msg);
            }
            await db.KeyExpireAsync("messages", expiry);
        }),
        
        // 哈希表操作
        db.HashSetAsync("user:1001", new HashEntry[] {
            new HashEntry("name", "李四"),
            new HashEntry("age", 30)
        })
        .ContinueWith(_ => db.KeyExpireAsync("user:1001", expiry)),
        
        // 集合操作
        db.SetAddAsync("tags", "热门")
        .ContinueWith(_ => db.KeyExpireAsync("tags", expiry)),
        
        // 有序集合操作
        db.SortedSetAddAsync("rank:game", "user1", 95)
        .ContinueWith(_ => db.KeyExpireAsync("rank:game", expiry)),
        
        // 流操作
        db.StreamAddAsync("order_events", new NameValueEntry[] {
            new NameValueEntry("order_id", "ORD-12345"),
            new NameValueEntry("status", "paid"),
            new NameValueEntry("amount", 99.9)
        })
        .ContinueWith(_ => db.KeyExpireAsync("order_events", expiry))
    };

            // 5. 控制并行度执行
            for (int i = 0; i < operations.Count; i += maxParallelism)
            {
                var batch = operations.Skip(i).Take(maxParallelism);
                await Task.WhenAll(batch);
            }
        }
        /// <summary>
        /// 连接PLC 
        /// </summary>
        public static void ConnectToPlc()
        {

            // 创建连接
            var plc = new Plc(CpuType.S7300, "127.0.0.1", 0, 2); // 根据实际情况配置IP地址、机架号和插槽号
            plc.Open();
            if (plc.IsConnected)
            {
                Console.WriteLine("连接到汇川PLC成功");
                plc.Read("DB1.DBW10");
                plc.Write("DB1.DBW10", new ushort[] { 123, 456 });
                plc.Write("DB1.DBW11", 11);

            }



        }
    }
}
