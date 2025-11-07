#region Using directives
using FTOptix.Alarm;
using FTOptix.AuditSigning;
using FTOptix.HMIProject;
using FTOptix.InfluxDBStore;
using FTOptix.InfluxDBStoreRemote;
using FTOptix.MQTTClient;
using FTOptix.NetLogic;
using FTOptix.ODBCStore;
using FTOptix.Recipe;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.UI;
using MQTTnet;
using NPOI.SS.Formula.Functions;
using StackExchange.Redis;
using System;
using System.Linq;
using TestProject1;
using TestProject1.Helper;
using TestProject1.Model;
using UAManagedCore;
using FTOptix.InfluxDBStoreLocal;
using static SQLite.SQLite3;
using System.ComponentModel.DataAnnotations;
using S7.Net;
using NPOI.HPSF;
using System.Security.Cryptography;
#endregion

public class RuntimeNetLogic1 : BaseNetLogic
{


  
    public override void Start()
    {
        PublicMethod.AddVariables();
        PublicMethod.SetVariable();
        PublicMethod.DB();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }
    [ExportMethod]
    public static void UpdateXmlAndCreateXML()
    {
        string excelPath = @"E:\generate\新增点位.xlsx";//excel 文件路径
        string xmlPath = @"E:\generate\generatexml\ThingTemplates_TS.Module.LAMINATEDREFLUXLINEM.Alarm.ThingTemplates.xml";//要修改的xml文件路径
        ExcelToXmlGenerator.GenerateXmlFromExcel(excelPath, xmlPath);
        CreateXml.PropertyBindServices();
        ///批量生成thing xml文件和remoteing thing xml文件
        CreateXml.CreateThingXml();
        CreateXml.CreateRemoteThingXml();
    }


    [ExportMethod]
    public static void Showdata(string res)
    {
        //var db = new SQLiteHelper(dbPath);
        ////var x = db.Query("datachange_log").Where(" lot_no = @p2","10111").ToList();
        //var multiLineText = db.Query("datachange_log").Where("Id = @po AND lot_no = @po2", 482, "10111").GetSingleObject("position_name");
        //Project.Current.GetVariable("Model/TxtVaule").Value = multiLineText.ToString();
        //var result = db.Queryable<GradingTypeCorrespond>()
        //    // 左连 GradingType：连接条件 a.code == b.code
        //    .LeftJoin<GradingType>((a, b) => a.code == b.code)
        //   // 关键：Select 投影，将 a（主表）和 b（连表）的字段赋值给 TwoTableClass
        //   .Select((GradingTypeCorrespond a, GradingType b) => new TwoTableClass { })
        //    // 可选：添加筛选条件（比如主表 code 不为空）
        //    .Where(a => !string.IsNullOrEmpty(a.code))
        //    // 可选：按主表 id 排序
        //    .OrderBy("t1.id") // t1 是主表默认别名，对应 GradingTypeCorrespond
        //    .ToList<TwoTableClass>();
        //string formattedTime = System.DateTime.Now.ToString("[HH:mm:ss.fff]");
        //_ = PublicMdethod.ReadRedis();


        ////将这个result转成string数组
        //string[] resultArray = new string[result.Count];
        //for (int i = 0; i < result.Count; i++)
        //{
        //    resultArray[i] = result[i].id + ", " + result[i].name + " ," + result[i].code+ ", " + result[i].grading_position +","+ result[i].created_time;
        //}
        var DB=SQLiteHelper.Instance;
       var stu= DB.ExecuteQuery("select * from datachange_log");
        //RedisExample.ListOperations("message", resultArray);
        CheckboxTest(res);
        OptionButtonTest();
        var db = Project.Current.Get<InfluxDBStoreLocal>("DataStores/LocalInfluxDBDatabase");
        string[] columnNames = new string[]
        {
    "id",
    "lot_no",
    "create_time",
    "region",
    "line_no",
    "grading_position",
    "if_ng",
    "ng_reason",
    "box_no",
    "if_corner",
    "lot_number",
    "electric",
    "color",
    "grade",
    "item",
    "order",
    "power",
    "ff",
    "product_family",
    "nameplate_qrInfo",
    "double_nameplate_qrInfo",
    "side_module_no",
    "max_num",
    "rule_type",
    "status",
    "if_force_pack"
        };
        // 提取值到二维数组（这里是1行26列的结构，对应一条数据）
        object[,] results = new object[1, 26];
        results[0, 0] = 89;  // id
        results[0, 1] = "R012505260004";  // lot_no
        results[0, 2] = "2025-05-26 14:23:20.837046";  // create_time
        results[0, 3] = "A";  // region
        results[0, 4] = "1";  // line_no
        results[0, 5] = "1档";  // grading_position
        results[0, 6] = 1;  // if_ng
        results[0, 7] = "";  // ng_reason
        results[0, 8] = "a001";  // box_no
        results[0, 9] = 1;  // if_corner
        results[0, 10] = 14;  // lot_number
        results[0, 11] = "I3";  // electric
        results[0, 12] = "Z";  // color
        results[0, 13] = "490";  // grade
        results[0, 14] = "aa";  // item
        results[0, 15] = "DJ254M1008";  // order
        results[0, 16] = "1.996A";  // power
        results[0, 17] = "0.99";  // ff
        results[0, 18] = "TSM-490DE18M(II)";  // product_family
        results[0, 19] = " nameplateQRInfo";  // nameplate_qrInfo
        results[0, 20] = "double";  // double_nameplate_qrInfo
        results[0, 21] = "sideModuleNo";  // side_module_no
        results[0, 22] = 14;  // max_num
        results[0, 23] = "0000";  // rule_type
        results[0, 24] = "正常";  // status
        results[0, 25] = 0;  // if_force_pack
        
        db.Insert("grading_record_log", columnNames, results);
        string sql = "SELECT* FROM grading_record_log ";
        string[] columnName;
        object[,] resul;
        db.Insert("grading_record_log",columnNames,results);
        var resultdata = db.Tables["grading_record_log"].Columns.AsQueryable(); // 按表名获取

    }
    /// <summary>
    /// 下载文件功能
    /// </summary>
    [ExportMethod]
    public static void SaveFile()
    {
   ExcelHelper.FileDownExcel();

    }
 
    /// <summary>
    /// 复选框选中事件
    /// </summary>
    /// <param name="res"></param>
    public static void CheckboxTest(string res)
    {
        if (res == "1")
        {
            Logger.Info("复选按钮选中状态");
        }
        else
        {
            Logger.Info("复选按钮未选中状态");
        }
    }
    /// <summary>
    /// 单选按钮取值
    /// </summary>
    public static void OptionButtonTest()
    {
        var result = Project.Current.GetVariable("Model/checked").Value.Value;
        var result1 = Project.Current.GetVariable("Model/checked1").Value.Value;
        if ((bool)result)
        {
            var genderText = (Project.Current.GetObject("UI/MainWindow/OptionButton1") as dynamic)
                ?.Children[1]?.DataValue?.Value?.Value?.Text as string;
        }
        if((bool)result1)
        {
            var genderText = (Project.Current.GetObject("UI/MainWindow/OptionButton2") as dynamic)
                ?.Children[1]?.DataValue?.Value?.Value?.Text as string;
        }


    }
    




}


