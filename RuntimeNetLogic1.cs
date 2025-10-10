#region Using directives
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using TestProject1;
using TestProject1.Helper;
using TestProject1.Model;
using UAManagedCore;
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
        string xmlPath = @"E:\generate\generatexml\ThingTemplates_TS.Module.LAMINATEDREFLUXLINEM.Alarm.ThingTemplates.xml";//要修改的xml文件路径
        ExcelToXmlGenerator.GenerateXmlFromExcel(excelPath, xmlPath);
        CreateXml.PropertyBindServices();
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
        bool isSuccess = ExcelHelper.FileDownExcel();

    }







}


