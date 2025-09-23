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
using NPOI.SS.Formula.Functions;
using NPOI.Util;
using S7.Net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TestProject1;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using Serilog;
using ILogger = Serilog.ILogger;
#endregion

public class RuntimeNetLogic1 : BaseNetLogic
{
  private static readonly string  dbPath = @"E:\aa\Test.db";
    public override void Start()
    {
        //showdata();
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }
    [ExportMethod]
    public static void UpdateXmlAndCreateXML() {
        string excelPath = @"E:\generate\新增点位.xlsx";//excel 文件路径
        string xmlPath = @"E:\generate\generatexml\ThingTemplates_TS.Module.LAMINTION.Alarm.ThingTemplates.xml";//要修改的xml文件路径
        ExcelToXmlGenerator.GenerateXmlFromExcel(excelPath, xmlPath);
        ///批量生成thing xml文件和remoteing thing xml文件
        CreateXml.CreateThingXml();
        CreateXml.CreateRemoteThingXml();
    }
    [ExportMethod]
    public static void  Test()
    {

        // 创建连接
        var plc = new Plc(CpuType.S7300, "127.0.0.1", 0, 2); // 根据实际情况配置IP地址、机架号和插槽号
        plc.Open();
        if (plc.IsConnected)
        {
            Console.WriteLine("连接到汇川PLC成功");
            plc.Read("DB1.DBW10");
            plc.Write("DB1.DBW10", new ushort[] { 123, 456 });
            plc.Write("DB1.DBW11",11);
          
        }
     
       

    }

    [ExportMethod]
    public static void Showdata()
    {

        string multiLineText = "报警1：温度过高报警2：压力异常报警3：设备离线";
        Project.Current.GetVariable("Model/TxtVaule").Value= multiLineText;
        var db = new SQLiteHelper(dbPath);
             db.GetAllTableNames();
 

    }
}
