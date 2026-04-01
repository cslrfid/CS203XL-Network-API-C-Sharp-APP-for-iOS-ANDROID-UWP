using MvvmCross.ViewModels;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static CSLibrary.RFIDDEVICE;

namespace BLE.Client.ViewModels
{
    public class DeviceListItemViewModel : MvxNotifyPropertyChanged
    {
        //public IDevice Device { get; private set; }
        public string IP;
        public MODEL BTServiceType { get; private set; }
        
        public string IdString {
            get {
                return IP;

/*
                string idString = Id.ToString().ToUpper();
                string macString = idString.Substring(idString.Length - 12, 2) + ":";
                macString += idString.Substring(idString.Length - 10, 2) + ":";
                macString += idString.Substring(idString.Length - 8, 2) + ":";
                macString += idString.Substring(idString.Length - 6, 2) + ":";
                macString += idString.Substring(idString.Length - 4, 2) + ":";
                macString += idString.Substring(idString.Length - 2, 2);
                return macString;
*/
            } 
        }
        public string Model => BTServiceType.ToString();
        public bool IsConnected = false;
        public int Rssi = 0;
        public string Name { get; set; }

        public DeviceListItemViewModel(string Name, string IP, MODEL BTServiceType)
        {
            this.Name = Name;
            this.IP = IP;
            this.BTServiceType = BTServiceType;
        }

        public void Update(IDevice newDevice = null)
        {
            /*
            if (newDevice != null)
            {
                Device = newDevice;
            }*/
            RaisePropertyChanged(nameof(IsConnected));
            RaisePropertyChanged(nameof(Rssi));
        }
    }
}
