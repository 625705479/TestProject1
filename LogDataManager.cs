using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FTOptix.Recipe;
using FTOptix.OmronFins;
using FTOptix.CommunicationDriver;
using FTOptix.Modbus;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.System;
using FTOptix.SerialPort;
using FTOptix.UI;

namespace TestProject1
{
    internal class LogDataManager : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
