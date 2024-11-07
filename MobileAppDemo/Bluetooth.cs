using System;
using System.Collections.Generic;
using InTheHand.Net;
using InTheHand.Net.Sockets;
using InTheHand.Net.Bluetooth;
using System.Threading;

namespace MobileAppDemo
{
    public static class Bluetooth
    {
        public static BluetoothDeviceInfo[] ScanDevices()
        {
            // Verifica quali dispositivi sono raggiungibili
            return new BluetoothClient().DiscoverDevicesInRange();
        }

        public static bool Pair(BluetoothDeviceInfo deviceToPair, string pin)
        {
            if (deviceToPair.Authenticated)
                return true;

            if (BluetoothSecurity.PairRequest(deviceToPair.DeviceAddress, pin))
                return true;

            return false;
        }

        public static bool Pair(BluetoothDeviceInfo deviceToPair)
        {
            return Pair(deviceToPair, "");
        }

        public static bool Connect(BluetoothDeviceInfo deviceToConnect, Action ListenerAction)
        {
            // Se il pairing va a buon fine
            if (Pair(deviceToConnect))
            {
                // Lancio il thread che gestisce la connessione
                Thread t = new Thread(() => ListenerAction());
                t.IsBackground = true;
                t.Start();

                return true;
            }

            return false;
        }

        public static void Disconnect(BluetoothClient client)
        {
            if (client != null)
            {
                client.Client.Disconnect(true);
                client.Close();
                client.Dispose();
            }
        }
    }
}
