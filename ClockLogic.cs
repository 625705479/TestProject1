#region Using directives
using FTOptix.NetLogic;
using System;
using UAManagedCore;
using FTOptix.AuditSigning;
using FTOptix.Recipe;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.Alarm;
using FTOptix.ODBCStore;
using FTOptix.InfluxDBStoreRemote;
using FTOptix.InfluxDBStore;
using FTOptix.InfluxDBStoreLocal;
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
    //    // 定时任务代码 添加定时更新字段值，每十秒加1
    //    var timer = new System.Timers.Timer(10000);
    //    timer.Elapsed += Timer_Elapsed;  // 这里使用了更规范的命名方式

    //    // 设置为自动重复触发
    //    timer.AutoReset = true;

    //    // 启动定时器
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
