#region Using directives
using FTOptix.NetLogic;
using System;
using UAManagedCore;
#endregion

public class ClockLogic : BaseNetLogic
{
    public override void Start()
    {
        periodicTask = new PeriodicTask(UpdateTime, 1000, LogicObject);
        //periodicTask = new PeriodicTask(PeriodicTask, 10000, LogicObject);

        periodicTask.Start();
    }

    public override void Stop()
    {
        periodicTask.Dispose();
        periodicTask = null;
    }
    //private static  int number ;
    private void UpdateTime()
    {
        LogicObject.GetVariable("Time").Value = DateTime.Now;
        LogicObject.GetVariable("UTCTime").Value = DateTime.UtcNow;
    }

    private PeriodicTask periodicTask;
    //private void PeriodicTask()
    //{
    //    // ��ʱ������� ��Ӷ�ʱ�����ֶ�ֵ��ÿʮ���1
    //    var timer = new System.Timers.Timer(10000);
    //    timer.Elapsed += Timer_Elapsed;  // ����ʹ���˸��淶��������ʽ

    //    // ����Ϊ�Զ��ظ�����
    //    timer.AutoReset = true;

    //    // ������ʱ��
    //    timer.Enabled = true;




    //}

    //private void Timer_Elapsed(object sender, ElapsedEventArgs e)
    //{
    //    int value = 0;
    //    for (int i = 0; i < 100000000; i++) {
    //        value=i;
    //        Project.Current.GetVariable("Model/Variable1").Value = value.ToString();
    //    }

    //}
}
