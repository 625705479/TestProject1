#region Using directives
using FTOptix.Alarm;
using FTOptix.AuditSigning;
using FTOptix.CommunicationDriver;
using FTOptix.HMIProject;
using FTOptix.InfluxDBStore;
using FTOptix.InfluxDBStoreLocal;
using FTOptix.InfluxDBStoreRemote;
using FTOptix.Modbus;
using FTOptix.MQTTClient;
using FTOptix.NetLogic;
using FTOptix.ODBCStore;
using FTOptix.OmronFins;
using FTOptix.Recipe;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.UI;
using MQTTnet;
using NPOI.HPSF;
using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Tsp;
using S7.Net;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using TestProject1;
using TestProject1.Helper;
using TestProject1.Model;
using UAManagedCore;
using static SQLite.SQLite3;
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
        string excelPath = @"E:\generate新增点位.xlsx";//excel �ļ�·��
        string xmlPath = @"E:\generate\generatexml\ThingTemplates_TS.Module.LAMINATEDREFLUXLINEM.Alarm.ThingTemplates.xml";//Ҫ�޸ĵ�xml�ļ�·��
        ExcelToXmlGenerator.GenerateXmlFromExcel(excelPath, xmlPath);
        CreateXml.PropertyBindServices();
        ///��������thing xml�ļ���remoteing thing xml�ļ�
        CreateXml.CreateThingXml();
        CreateXml.CreateRemoteThingXml();
    }


    [ExportMethod]
    public static void Showdata(string res)
    {
        LoadVM();
        //测试SQLite
        //var db = new SQLiteHelper(dbPath);
        ////var x = db.Query("datachange_log").Where(" lot_no = @p2","10111").ToList();
        //var multiLineText = db.Query("datachange_log").Where("Id = @po AND lot_no = @po2", 482, "10111").GetSingleObject("position_name");
        //Project.Current.GetVariable("Model/TxtVaule").Value = multiLineText.ToString();
        //var result = db.Queryable<GradingTypeCorrespond>()
        //    // ���� GradingType���������� a.code == b.code
        //    .LeftJoin<GradingType>((a, b) => a.code == b.code)
        //   // �ؼ���Select ͶӰ���� a��������� b����������ֶθ�ֵ�� TwoTableClass
        //   .Select((GradingTypeCorrespond a, GradingType b) => new TwoTableClass { })
        //    // ��ѡ�����ɸѡ�������������� code ��Ϊ�գ�
        //    .Where(a => !string.IsNullOrEmpty(a.code))
        //    // ��ѡ�������� id ����
        //    .OrderBy("t1.id") // t1 ������Ĭ�ϱ�������Ӧ GradingTypeCorrespond
        //    .ToList<TwoTableClass>();
        //string formattedTime = System.DateTime.Now.ToString("[HH:mm:ss.fff]");
        //_ = PublicMdethod.ReadRedis();


        ////�����resultת��string����
        //string[] resultArray = new string[result.Count];
        //for (int i = 0; i < result.Count; i++)
        //{
        //    resultArray[i] = result[i].id + ", " + result[i].name + " ," + result[i].code+ ", " + result[i].grading_position +","+ result[i].created_time;
        //}
        var DB =SQLiteHelper.Instance;
       var stu= DB.ExecuteQuery("select * from datachange_log");
        //RedisExample.ListOperations("message", resultArray);
        //CheckboxTest(res);
        OptionButtonTest();
        var result = Project.Current.GetVariable("Model/checked").Value.Value;

    }
    /// <summary>
    /// �����ļ�����
    /// </summary>
    [ExportMethod]
    public static void SaveFile()
    {
   ExcelHelper.FileDownExcel();

    }

    /// <summary>
    /// 复选按钮测试
    /// </summary>
    /// <param name = "res" ></ param >
    public static void CheckboxTest(string res)
    {
        if (res == "1")
        {
            Logger.Info("按钮被选中");

        }
        else
        {
            Logger.Info("按钮未选中");
        }
    }
    /// <summary>
    ///单选按钮测试
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
  
    public static void LoadVM()
    {
        #region 插入user_role数据到Store
        var (sql, path) = ("SELECT * from user_role", Project.Current.GetVariable("Model/SqlliteDataPath")?.Value?.Value?.ToString() ?? throw new InvalidOperationException("SqlliteDataPath不存在"));
        var dt = new SQLiteHelper(path).ExecuteQuery(sql);
        var converters = new Dictionary<string, Func<object, object>>
        {
            ["role"] = value =>
            {
                var strValue = (value == DBNull.Value ? null : value)?.ToString() ?? string.Empty;
                return strValue == "1" ? "普通用户" : "管理员";
            }
        };
        PubilcMethodHelper.InsertStore("user_role", dt, new string[] { "user_account", "password", "role" }, converters);
        #endregion



    }


}


