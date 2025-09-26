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


    public override void Start()
    {
       PublicMdethod.addVariables();

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
    public static void Showdata()
    {
        var db = new SQLiteHelper(dbPath);
        //var x = db.Query("datachange_log").Where(" lot_no = @p2","10111").ToList();
        var multiLineText = db.Query("datachange_log").Where("Id = @po AND lot_no = @po2", 482, "10111").GetSingleObject("position_name");
        Project.Current.GetVariable("Model/TxtVaule").Value = multiLineText.ToString();
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
        _ = PublicMdethod.ReadRedis();

    
    ////将这个result转成string数组
    //string[] resultArray = new string[result.Count];
    //for (int i = 0; i < result.Count; i++)
    //{
    //    resultArray[i] = result[i].id + ", " + result[i].name + " ," + result[i].code+ ", " + result[i].grading_position +","+ result[i].created_time;
    //}

    //RedisExample.ListOperations("message", resultArray);



}
    /// <summary>
    /// 下载文件功能
    /// </summary>
    [ExportMethod]
    public static void SaveFile()
    {
    bool isSuccess  =ExcelHelper.FileDownExcel();

    }

   





}


