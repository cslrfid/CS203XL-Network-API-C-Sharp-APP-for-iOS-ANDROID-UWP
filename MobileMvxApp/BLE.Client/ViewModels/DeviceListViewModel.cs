using Acr.UserDialogs;
using BLE.Client.Extensions;
using CSLibrary.NetFinder.CS203XL;
using MvvmCross;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Extensions;
using Plugin.Settings.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static CSLibrary.RFIDDEVICE;

namespace BLE.Client.ViewModels
{
    public class DeviceListViewModel : BaseViewModel
    {
        private readonly IUserDialogs _userDialogs;
        private readonly ISettings _settings;
        private readonly IMvxNavigationService _navigation;

        CSLibrary.NetFinder.CS203XL.NetFinder netFinder = new CSLibrary.NetFinder.CS203XL.NetFinder();

        private Guid _previousGuid;
        private CancellationTokenSource _cancellationTokenSource;

        public IList<IService> Services { get; private set; }
        public IDescriptor Descriptor { get; private set; }

        private string _version;
        public string version { get; set; }

        public MvxCommand RefreshCommand => new MvxCommand(() => TryStartScanning(true));
        public MvxCommand<DeviceListItemViewModel> DisconnectCommand => new MvxCommand<DeviceListItemViewModel>(DisconnectDevice);

        public MvxCommand<DeviceListItemViewModel> ConnectDisposeCommand;

        public ObservableCollection<DeviceListItemViewModel> Devices { get; set; } = new ObservableCollection<DeviceListItemViewModel>();
        public bool IsRefreshing => Adapter.IsScanning;
        public bool IsStateOn => true;
        public string StateText => "";
        public DeviceListItemViewModel SelectedDevice
        {
            get { return null; }
            set
            {
                if (value != null)
                {
                    HandleSelectedDevice(value);
                }

                RaisePropertyChanged();
            }
        }

        public MvxCommand StopScanCommand => new MvxCommand(() =>
        {
            try
            {
                Devices.Clear();

                _cancellationTokenSource.Cancel();
                CleanupCancellationToken();
                RaisePropertyChanged(() => IsRefreshing);
            }
            catch (Exception ex)
            {
                CSLibrary.Debug.WriteLine("can not stop _cancellationTokenSource");
            }
        }, () => _cancellationTokenSource != null);

        public DeviceListViewModel(IBluetoothLE bluetoothLe, IAdapter adapter, IUserDialogs userDialogs, ISettings settings, IMvxNavigationService navigation) : base(adapter)
        {
            _userDialogs = userDialogs;
            _settings = settings;
            _navigation = navigation;

            netFinder.OnSearchCompleted += new EventHandler<DeviceFinderArgs>(netfinder_OnSearchCompleted);
            BleMvxApplication.readerName = "";
            BleMvxApplication._reader.DisconnectAsync();
        }

        private void OnStateChanged(object sender, BluetoothStateChangedArgs e)
        {
            RaisePropertyChanged(nameof(IsStateOn));
            RaisePropertyChanged(nameof(StateText));
        }

        bool _scanAgain = true;

        private void Adapter_ScanTimeoutElapsed(object sender, EventArgs e)
        {
            RaisePropertyChanged(() => IsRefreshing);

            CleanupCancellationToken();

            if (_scanAgain)
                ScanForDevices();
        }

        private void AddOrUpdateDevice(string Name, string IP, MODEL BTServiceType)
        {
            InvokeOnMainThread(() =>
            {
                try
                {
                    var vm = Devices.FirstOrDefault(d => d.IP == IP);

                    if (vm == null)
                        Devices.Add(new DeviceListItemViewModel(Name, IP, BTServiceType));
                }
                catch (Exception ex)
                {
                    CSLibrary.Debug.WriteLine("Can not add device");
                }
            });
        }

        bool firstView = true;
        public override async void ViewAppearing()
        {
            base.ViewAppearing();

            if (!firstView)
                return;

            firstView = false;
            try
            {
                TryStartScanning();
            }
            catch (Exception ex)
            {
                CSLibrary.Debug.WriteLine("Device Resume Error");
            }
        }

        public List<DeviceListItemViewModel> SystemDevices { get; private set; }

        public override void ViewDisappearing()
        {
            try
            {
                base.ViewDisappearing();

                netFinder.Stop();
                netFinder.OnSearchCompleted -= new EventHandler<DeviceFinderArgs>(netfinder_OnSearchCompleted);

                RaisePropertyChanged(() => IsRefreshing);
            }
            catch (Exception ex)
            {
                CSLibrary.Debug.WriteLine("Device Suspend error");
            }
        }

        private async void TryStartScanning(bool refresh = false)
        {
            if (IsStateOn && (refresh || !Devices.Any()) && !IsRefreshing)
            {
                Devices.Clear();

                ScanForDevices();
            }
        }

        private async void ScanForDevices()
        {
            try
            {
                RaisePropertyChanged(() => StopScanCommand);
                RaisePropertyChanged(() => IsRefreshing);

                netFinder.SearchDevice();

                var devices = netFinder.Radios;
            }
            catch (Exception ex)
            {
                CSLibrary.Debug.WriteLine("Can not Scan devices");
            }
        }

        void netfinder_OnSearchCompleted(object sender, DeviceFinderArgs e)
        {
            //UpdateInfo(Info_Select);
            //UpdateUI(e.Found);

            var DeviceName = e.Found.DeviceName;
            var IP = e.Found.IPAddress;

            AddOrUpdateDevice(DeviceName, IP, MODEL.CS203XL);
        }

        private void CleanupCancellationToken()
        {
            try
            {
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
                RaisePropertyChanged(() => StopScanCommand);

                if (_scanAgain)
                    ScanForDevices();
            }
            catch (Exception ex)
            {
                CSLibrary.Debug.WriteLine("Can not stop _cancellationTokenSource");
            }
        }

        private async void DisconnectDevice(DeviceListItemViewModel device)
        {
            if (BleMvxApplication._reader.Status != CSLibrary.HighLevelInterface.READERSTATE.DISCONNECT)
                BleMvxApplication._reader.DisconnectAsync();

            try
            {
                if (!device.IsConnected)
                    return;

                _userDialogs.ShowLoading($"Disconnecting {device.Name}...");

                //await Adapter.DisconnectDeviceAsync(device.Device);
            }
            catch (Exception ex)
            {
                _userDialogs.Alert(ex.Message, "Disconnect error");
            }
            finally
            {
                device.Update();
                _userDialogs.HideLoading();
            }
        }

        private async void HandleSelectedDevice(DeviceListItemViewModel devices)
        {
            try
            {
                Connect(devices, devices.BTServiceType);

                _ = _navigation.Close(this);
            }
            catch (Exception ex)
            {
                _userDialogs.Alert(ex.Message, "Disconnect error");
            }

        }

        private async void Connect(DeviceListItemViewModel _device, MODEL deviceType)
        {
            //Trace.Message("device name :" + _device.Name);

            BleMvxApplication._deviceinfo = null;
            BleMvxApplication.readerName = _device.Name;

            //await BleMvxApplication._reader.ConnectAsync("192.168.25.233", 1525);
            await BleMvxApplication._reader.ConnectAsync(_device.IP, 1525);
            BleMvxApplication.connectionName = _device.IP;

            Trace.Message("load config");

            //bool LoadSuccess = await BleMvxApplication.LoadConfig(_device.Name);
            //BleMvxApplication._config.readerID = _device.Name;
            //bool LoadSuccess = await BleMvxApplication.LoadConfig(_device.Id.ToString(), BleMvxApplication._reader.rfid.GetAntennaPort());
            bool LoadSuccess = await BleMvxApplication.LoadConfig(_device.IP, deviceType);
            BleMvxApplication._config.readerID = _device.IP;
        }
    }
}
