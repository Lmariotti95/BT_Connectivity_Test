# Esempio di connessione bluetooth utilizzando la libreria Plugin.BLE
- Verifica i dispositivi raggiungibili
- Si connette ad un dispositivo
- Accede al servizio SPS
- Legge i dati ricevuti da SPS
- Test eseguiti su:
  - [X] Windows
  - [ ] Android
  - [ ] iOS

```C#
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.Exceptions;

using System.Diagnostics;
using System.Text;

namespace MyNamespace
{
    public class MyBleClass
    {
        private readonly IAdapter _adapter;
        private readonly IBluetoothLE _bluetooth;

        private List<IDevice> devicesInRange = new List<IDevice>();

        // Dati grezzi ricevuti
        private List<byte> rxData = new List<byte>();

        private readonly Guid SpsServiceUuid = Guid.Parse("2456E1B9-26E2-8F83-E744-F34F01E9D701");
        private readonly Guid SpsCharacteristicUuid = Guid.Parse("2456E1B9-26E2-8F83-E744-F34F01E9D703");

        public MyBleClass()
        {
            new Permissions.Bluetooth().RequestAsync();

            _bluetooth = CrossBluetoothLE.Current;
            _adapter = CrossBluetoothLE.Current.Adapter;
        }

        public List<IDevice> GetDevicesInRange() { return this.devicesInRange; }

        #region BLUETOOTH

        public void ScanForDeviceInRange()
        {
            devicesInRange.Clear();
            ScanForBluetoothDevices();
        }

        private async void ScanForBluetoothDevices()
        {
            // Se il bluetooth è abilitato
            if (_bluetooth.State == BluetoothState.On)
            {
                if (!_adapter.IsScanning)
                {
                    _adapter.DeviceDiscovered += (s, a) => deviceList.Add(a.Device);
                    _adapter.ScanMode = ScanMode.LowLatency;

                    await _adapter.StartScanningForDevicesAsync();
                }
            }
            else
            {
                Console.WriteLine("Please turn on Bluetooth");
            }
        }

        private async void Connect(IDevice device)
        {
            try
            {
                await _adapter.ConnectToDeviceAsync(device);
                GetSpsCharacteristicAsync(device);
            }
            catch (DeviceConnectionException e)
            {
                // ... could not connect to device
            }
        }

        private async void GetSpsCharacteristicAsync(IDevice device)
        {
            // Get sul service SPS
            var service = await device.GetServiceAsync(SpsServiceUuid);
            if (service == null)
            {
                Console.WriteLine("SPS service not found.");
                return;
            }

            // Get sulla caratteristica SPS
            var characteristic = await service.GetCharacteristicAsync(SpsCharacteristicUuid);
            if (characteristic == null)
            {
                Console.WriteLine("SPS characteristic not found.");
                return;
            }

            // Abilita l'evento sulla ricezione SPS
            characteristic.ValueUpdated += (s, e) =>
            {
                rxData.AddRange(e.Characteristic.Value);
            };

            // Avvia il listener
            await characteristic.StartUpdatesAsync();
        }
        #endregion
    }
}
```