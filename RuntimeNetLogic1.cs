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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
  //����һ���ֵ� �����洢���������ƺ�ֵ
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
        string excelPath = @"E:\generate\������λ.xlsx";//excel �ļ�·��
        string xmlPath = @"E:\generate\generatexml\ThingTemplates_TS.Module.LAMINTION.Alarm.ThingTemplates.xml";//Ҫ�޸ĵ�xml�ļ�·��
        ExcelToXmlGenerator.GenerateXmlFromExcel(excelPath, xmlPath);
        ///��������thing xml�ļ���remoteing thing xml�ļ�
        CreateXml.CreateThingXml();
        CreateXml.CreateRemoteThingXml();
    }
    [ExportMethod]
    public static void Test()
    {

        // ��������
        var plc = new Plc(CpuType.S7300, "127.0.0.1", 0, 2); // ����ʵ���������IP��ַ�����ܺźͲ�ۺ�
        plc.Open();
        if (plc.IsConnected)
        {
            Console.WriteLine("���ӵ��㴨PLC�ɹ�");
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


        // ִ�������ѯ
        var result = db.Queryable<GradingTypeCorrespond>()
            // ���� GradingType���������� a.code == b.code
            .LeftJoin<GradingType>((a, b) => a.code == b.code)
           // �ؼ���Select ͶӰ���� a�������� b���������ֶθ�ֵ�� TwoTableClass
           .Select((GradingTypeCorrespond a, GradingType b) => new TwoTableClass { })
            // ��ѡ�����ɸѡ�������������� code ��Ϊ�գ�
            .Where(a => !string.IsNullOrEmpty(a.code))
            // ��ѡ�������� id ����
            .OrderBy("t1.id") // t1 ������Ĭ�ϱ�������Ӧ GradingTypeCorrespond
            .ToList<TwoTableClass>();
        string formattedTime = System.DateTime.Now.ToString("[HH:mm:ss.fff]");
        ReadRedis();
        ////�����resultת��string����
        //string[] resultArray = new string[result.Count];
        //for (int i = 0; i < result.Count; i++)
        //{
        //    resultArray[i] = result[i].id + ", " + result[i].name + " ," + result[i].code+ ", " + result[i].grading_position +","+ result[i].created_time;
        //}

        //RedisExample.ListOperations("message", resultArray);

    }
    public static void addVariables() {

        var Object1 = Project.Current.GetObject("Model/Object1").Children.ToList();
        data.Columns.Add("name");
        data.Columns.Add("code");
        data.Rows.Add("ˮ��", "123");
        data.Rows.Add("�߲�", "456");
        data.Rows.Add("����", "789");
        data.Rows.Add("ˮ��", "10111");
        data.Rows.Add("ũ��Ʒ", "12131");
        SetEnumeration(data);
        Object1[0].FindVariable("ˮ��").Value = "123";
    }

    public static void SetEnumeration(DataTable dt) {
        //��ȡö��
        var  AralmEnumeration = Project.Current.Get("Model/Enumeration1");
        #region ͬ��ö��ֵ
        var enumChildren = AralmEnumeration.Children.ToList();
        var firstChild = enumChildren[0] as UAVariable;
        // ���ڵ��Ƿ���Ч
        if (firstChild == null)
        {
            Logger.Error("�޷���ȡ��Ч��UAVariable�ڵ�");
            return;
        }
        uint statusCode = firstChild.DataValue.StatusCode;
        // ��ȡ�ڵ㵱ǰֵ�����������.Value���������ʹ���
        var nodeValue = firstChild.DataValue.Value.Value;
  
        string formattedTime = System.DateTime.Now.ToString("[HH:mm:ss.fff]");
   
 
        // ��֤�Ƿ�Ϊ�ṹ����������
        if (nodeValue is UAManagedCore.Struct[] structArray)
        {
            int loopCount = Math.Min(data.Rows.Count, structArray.Length);
            for (int i = 0; i < loopCount; i++)
            {
                var currentStruct = structArray[i];
                var valuesProperty = currentStruct.GetType().GetProperty("Values");
                if (valuesProperty == null)
                {
                    Logger.Warn($"��{i}���ṹ��û��Values���ԣ�����");
                    continue;
                }

                var values = valuesProperty.GetValue(currentStruct) as object[];
                if (values == null || values.Length < 2)
                {
                    Logger.Warn($"��{i}���ṹ���Values������Ч������");
                    continue;
                }

                var localizedTextObj = values[1] as UAManagedCore.LocalizedText;
                if (localizedTextObj == null)
                {
                    Logger.Warn($"��{i}��Values[1]����LocalizedText���ͣ�����");
                    continue;
                }

                // ��ֵ����
                var row = data.Rows[i];
                string name = row["name"]?.ToString() ?? "";
                string code = row["code"]?.ToString() ?? "";
                localizedTextObj.Text = $"{code}:{name}";
                localizedTextObj.LocaleId = "zh-CN";
            }
            // д�ؽڵ�
            var uaValue = new UAManagedCore.UAValue(structArray);

            // 2. ʹ����ȷ�Ĺ��캯������DataValue
            var updatedDataValue = new UAManagedCore.DataValue(
                uaValue,  // ��װ���UAValue����
                statusCode: statusCode,  // ״̬�루��ʾֵ��Ч��
                DateTime.UtcNow,  // Դʱ���
                DateTime.UtcNow   // ������ʱ���
            );

            // 3. д�ؽڵ�
            firstChild.DataValue = updatedDataValue;


        }
        else
        {
            Logger.Error("�ڵ�ֵ������Ч��Struct[]��������");
        }
        #endregion
    }


    /// <summary>
    /// ��ȡRedis����
    /// </summary>
    public static void ReadRedis() {
        //��ȡ�ַ���:
        //    var db = RedisExample.GetDatabase();
        //    string username = db.StringGet("username1");
        //    ��ȡ�б�
        //    RedisValue[] messages = db.ListRange("message");
        //    ��ȡ��ϣ��
        //    string name = db.HashGet("hashkey", "name");
        //    HashEntry[] allFields = db.HashGetAll("hashkey");
        //    ��ȡ���ϣ�
        //    RedisValue[] allValues = db.SetMembers("test");
        //    ��ȡ���򼯺ϣ�
        //    RedisValue[] topUsers = db.SortedSetRangeByScore("product");
        //    double? score = db.SortedSetScore("product", "banana");
        //    string cachedJson = db.StringGet("product:1");
        //    var cachedProduct = JsonConvert.DeserializeObject<Product>(cachedJson);
        //    var productname = cachedProduct.name;
        // ��ʼ������
        RedisExample.InitializeRedis();



        // 1. �ַ�������
        RedisExample.Operate(
            RedisExample.RedisType.String,
            "username",
            "����", dbIndex: 1,
            useExpire: true,
            expireMinutes: 15);

        // 2. �б����
        RedisExample.Operate(
            RedisExample.RedisType.List,
            "messages",
            new string[] { "��Ϣ1", "��Ϣ2", "��Ϣ3" }, useExpire: true, dbIndex: 1,
            expireMinutes: 15);

        // 3. ��ϣ�����
        RedisExample.Operate(
           RedisExample.RedisType.Hash,
            "user:1001",
            hashEntries: new HashEntry[] {
                new HashEntry("name", "����"),
                new HashEntry("age", 30)
            }, dbIndex: 1,
            useExpire: true,
            expireMinutes: 15);

        // 4. ���ϲ���
        RedisExample.Operate(
            RedisExample.RedisType.Set,
            "tags",
            "����",
            dbIndex: 1,
            useExpire: true,
            expireMinutes: 15);

        // 5. ���򼯺ϲ���
        RedisExample.Operate(
           RedisExample.RedisType.ZSet,
            "rank:game",
            "user1",
            score: 95,
                        dbIndex: 1,
                    useExpire: true,
            expireMinutes: 15);
        // 6. ����������15���ӹ��ڣ�
        RedisExample.Operate(
            RedisExample.RedisType.Stream,
            "order_events",  // ���ļ���
            streamEntries: new NameValueEntry[] {  // ����Ϣ���ֶΣ���ֵ�ԣ�
                new NameValueEntry("order_id", "ORD-12345"),
                new NameValueEntry("status", "paid"),
                new NameValueEntry("amount", 99.9)
            },
            useExpire: true);  // ����15���ӹ���







    }

    public class Product
    {
        public int id { get; set; }
        public string name { get; set; } // ����JSON����"Name"�ֶ�
        public double price { get; set; }
        public string tags { get; set; }

        // ��������...
    }
}
