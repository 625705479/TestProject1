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
using NPOI.Util;
using S7.Net;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TestProject1;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
#endregion

public class RuntimeNetLogic1 : BaseNetLogic
{
  
    public override void Start()
    {
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
    public void test()
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

}
