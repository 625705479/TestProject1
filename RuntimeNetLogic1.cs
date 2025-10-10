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
        string excelPath = @"E:\generate\������λ.xlsx";//excel �ļ�·��
        string xmlPath = @"E:\generate\generatexml\ThingTemplates_TS.Module.LAMINATEDREFLUXLINEM.Alarm.ThingTemplates.xml";//Ҫ�޸ĵ�xml�ļ�·��
        ExcelToXmlGenerator.GenerateXmlFromExcel(excelPath, xmlPath);
        CreateXml.PropertyBindServices();
        ///��������thing xml�ļ���remoteing thing xml�ļ�
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
        _ = PublicMdethod.ReadRedis();


        ////�����resultת��string����
        //string[] resultArray = new string[result.Count];
        //for (int i = 0; i < result.Count; i++)
        //{
        //    resultArray[i] = result[i].id + ", " + result[i].name + " ," + result[i].code+ ", " + result[i].grading_position +","+ result[i].created_time;
        //}

        //RedisExample.ListOperations("message", resultArray);



    }
    /// <summary>
    /// �����ļ�����
    /// </summary>
    [ExportMethod]
    public static void SaveFile()
    {
        bool isSuccess = ExcelHelper.FileDownExcel();

    }







}


