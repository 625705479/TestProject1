#region Using directives
using FTOptix.Core;
using FTOptix.CoreBase;
using FTOptix.HMIProject;
using FTOptix.NativeUI;
using FTOptix.NetLogic;
using FTOptix.Retentivity;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.UI;
using Newtonsoft.Json;
using NPOI.POIFS.Crypt;
using NPOI.SS.Formula.Functions;
using NPOI.Util;
using NPOI.XSSF.Streaming.Values;
using S7.Net;
using S7.Net.Types;
using Serilog;
using SixLabors.Fonts;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using TestProject1;
using TestProject1.Model;
using UAManagedCore;
using static NPOI.HSSF.Util.HSSFColor;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;
using static TestProject1.SQLiteHelper;
using DateTime = System.DateTime;
using ILogger = Serilog.ILogger;
using Match = System.Text.RegularExpressions.Match;
using OpcUa = UAManagedCore.OpcUa;
#endregion

public class RuntimeNetLogic1 : BaseNetLogic
{
    private static readonly string dbPath = @"E:\aa\Test.db";
    private static readonly DataTable data = new DataTable();
  //定义一个字典 用来存储变量的名称和值
    private static Dictionary<string, object> variables = new Dictionary<string, object>();

    public override void Start()
    {
        addVariables();

        //showdata();
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }
    [ExportMethod]
    public static void UpdateXmlAndCreateXML()
    {
        string excelPath = @"E:\generate\新增点位.xlsx";//excel 文件路径
        string xmlPath = @"E:\generate\generatexml\ThingTemplates_TS.Module.LAMINTION.Alarm.ThingTemplates.xml";//要修改的xml文件路径
        ExcelToXmlGenerator.GenerateXmlFromExcel(excelPath, xmlPath);
        ///批量生成thing xml文件和remoteing thing xml文件
        CreateXml.CreateThingXml();
        CreateXml.CreateRemoteThingXml();
    }
    [ExportMethod]
    public static void Test()
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

    [ExportMethod]
    public static void Showdata()
    {
        var db = new SQLiteHelper(dbPath);
        //var x = db.Query("datachange_log").Where(" lot_no = @p2","10111").ToList();
        var multiLineText = db.Query("datachange_log").Where("Id = @po AND lot_no = @po2", 482, "10111").GetSingleObject("position_name");
        Project.Current.GetVariable("Model/TxtVaule").Value = multiLineText.ToString();


        // 执行连表查询
        var result = db.Queryable<GradingTypeCorrespond>()
            // 左连 GradingType：连接条件 a.code == b.code
            .LeftJoin<GradingType>((a, b) => a.code == b.code)
           // 关键：Select 投影，将 a（主表）和 b（连表）的字段赋值给 TwoTableClass
           .Select((GradingTypeCorrespond a, GradingType b) => new TwoTableClass { })
            // 可选：添加筛选条件（比如主表 code 不为空）
            .Where(a => !string.IsNullOrEmpty(a.code))
            // 可选：按主表 id 排序
            .OrderBy("t1.id") // t1 是主表默认别名，对应 GradingTypeCorrespond
            .ToList<TwoTableClass>();
        string formattedTime = System.DateTime.Now.ToString("[HH:mm:ss.fff]");
        _ = ReadRedis();
    ExcelHelper.FileDownExcel();


        ////将这个result转成string数组
        //string[] resultArray = new string[result.Count];
        //for (int i = 0; i < result.Count; i++)
        //{
        //    resultArray[i] = result[i].id + ", " + result[i].name + " ," + result[i].code+ ", " + result[i].grading_position +","+ result[i].created_time;
        //}

        //RedisExample.ListOperations("message", resultArray);

    }
    [ExportMethod]
    public static void SaveExcelFile()
    {
        var result=ExcelHelper.FileDownExcel();
        string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string saveFilePath = userProfilePath + "\\Downloads\\" + result.FileDownloadName;
        //通过io文件流保存到本地
        using (var fileStream = new FileStream(
          path: saveFilePath,
          mode: FileMode.Create, // 不存在则创建，存在则覆盖
          access: FileAccess.Write,
          share: FileShare.None)) // 写入时禁止其他程序占用
        {
            // 将FileContent中的字节流写入本地文件
            fileStream.Write(result.fileContents, 0, result.fileContents.Length);
            // 强制刷新缓冲区，确保数据完全写入（大文件建议加）
            fileStream.Flush();
        }


    }

    public static void addVariables() {

        var Object1 = Project.Current.GetObject("Model/Object1").Children.ToList();
        data.Columns.Add("name");
        data.Columns.Add("code");
        data.Rows.Add("水果", "123");
        data.Rows.Add("蔬菜", "456");
        data.Rows.Add("肉类", "789");
        data.Rows.Add("水产", "10111");
        data.Rows.Add("农产品", "12131");
        SetEnumeration(data);
        Object1[0].FindVariable("水果").Value = "123";
    }

    public static void SetEnumeration(DataTable dt) {
        //获取枚举
        var  AralmEnumeration = Project.Current.Get("Model/Enumeration1");
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
    public static async Task ReadRedis() {
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
        int dbIndex = 1;
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






}

    public class Product
    {
        public int id { get; set; }
        public string name { get; set; } // 假设JSON中有"Name"字段
        public double price { get; set; }
        public string tags { get; set; }

        // 其他属性...
    }

