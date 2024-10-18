using System;
using System.Collections.Generic;
using System.Drawing;
using InTheHand.Net;
using InTheHand.Net.Sockets;
using InTheHand.Net.Bluetooth;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Text;

namespace MobileAppDemo
{
    public partial class MainGui : Form
    {
        BluetoothDeviceInfo[] btDevicesInRange;
        BluetoothDeviceInfo btConnectedDevice;

        BluetoothClient btClient;

        Thread btThreadListener = null;

        public MainGui()
        {
            InitializeComponent();

            DataParser.Instance.SetCommand(new List<BtCommand>
            {
                new BtCommand() { Text = "ping",        Callback = CallbackPing },
                new BtCommand() { Text = "START FILE",  Callback = null },
                new BtCommand() { Text = "END OF FILE", Callback = CallbackEndOfFile }
            });
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void SetStatus(string status)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(SetStatus), new object[] { status });
                return;
            }

            lblStatus.Text = status;
        }

        private void UpdateRxData(List<ListViewItem> items)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<List<ListViewItem>>(UpdateRxData), new object[] { items });
                return;
            }

            listViewRxDataViewer.Items.Clear();
            listViewRxDataViewer.Items.AddRange(items.ToArray());
        }

        #region BLUETOOTH
        private void ScanDevices()
        {
            try
            {
                // Verifico quali dispositivi sono raggiungibili
                btDevicesInRange = Bluetooth.ScanDevices();
                SetStatus($"{btDevicesInRange.Length} devices in range");

                ToolStripAddDevices(btDevicesInRange);
            }
            catch (PlatformNotSupportedException)
            {
                // Il bluetooth del PC è disattivato/non funzionante/non presente 
                MessageBox.Show("Bluetooth is turned off");
                SetStatus($"Not connected");
            }
        }

        private void ManageConnectClick(object sender, EventArgs e)
        {
            ToolStripMenuItem obj = (ToolStripMenuItem)sender;
            for (int i = 0; i < connectToolStripMenuItem.DropDownItems.Count; i++)
            {
                // Determino qual'è l'indice dell'elemento che ha scatenato l'evento
                if (obj == connectToolStripMenuItem.DropDownItems[i])
                {
                    SetStatus($"Connecting");

                    // Se eventualmente sono già connesso allora mi disconnetto
                    Manage_bt_disconnect();

                    // Determino il dispoitivo al quale connettermi
                    btConnectedDevice = btDevicesInRange[i];
                    Bluetooth.Connect(btConnectedDevice, Client_connect_thread);

                    SetStatus($"Connected");

                    break;
                }
            }
        }

        public void Client_connect_thread()
        {
            BluetoothEndPoint bt_end_point = new BluetoothEndPoint(btConnectedDevice.DeviceAddress, BluetoothService.SerialPort);
            BluetoothClient client = new BluetoothClient();

            try
            {
                client.Connect(bt_end_point);

                Stream str = client.GetStream();
                str.ReadTimeout = 200;
                str.WriteTimeout = 200;

                // Se la connessione va a buon fine lancio il thread listener
                Start_bt_listener(str);

                btClient = client;
            }
            catch (Exception)                      // Se succede qualcosa di imprevisto
            {
                btClient = null;       // Cancello il client
            }
        }

        public void Start_bt_listener(Stream str)
        {
            if (btThreadListener == null)
            {
                btThreadListener = new Thread(() => Bt_communication_handler(str));
                btThreadListener.IsBackground = true;
                btThreadListener.Start();
            }
        }

        private void Manage_bt_disconnect()
        {
            // Se il thread di comunicazione è definito
            if (btThreadListener != null)
            {
                Utils.CloseThread(btThreadListener);

                // Se il client connessto è definito
                if (btClient != null)
                {
                    btClient.Close();
                    btClient.Dispose();
                }

                btThreadListener = null;
            }

            SetStatus($"Not connected");
        }
        #endregion

        #region COMUNICAZIONE BLUETOOTH
        private void Bt_communication_handler(Stream str)
        {
            while (true)
            {
                try
                {
                    int data = str.ReadByte();
                    if (data != -1)
                    {
                        DataParser.Instance.PushCharacter((char)data);
                        //UpdateRxData((char)data);
                    }
                }
                catch (IOException) { }
            }
        }
        #endregion

        #region CALLBACKS
        private void CallbackPing()
        {
            Stream str = btClient.GetStream();
            byte[] bResponse = Encoding.ASCII.GetBytes("pong");
            str.Write(bResponse, 0, bResponse.Length);
        }

        private void CallbackEndOfFile()
        {
            List<string> lines = DataParser.Instance.GetPayload();
            List<ListViewItem> items = new List<ListViewItem>();

            const char CSV_SEPARATOR = ';';
            listViewRxDataViewer.Items.Clear();

            foreach (string line in lines)
            {
                var fields = line.Trim('\n').Trim('\r').Split(CSV_SEPARATOR).ToList();

                while (fields.Count < 3)
                    fields.Add("");

                ListViewItem item = new ListViewItem(fields[0]);
                item.SubItems.Add(fields[1]);
                item.SubItems.Add(fields[2]);

                // Add the item to the ListView
                items.Add(item);
            }

            UpdateRxData(items);
            btClient.GetStream().Flush();
            DataParser.Instance.ClrPayload();
        }
        #endregion

        private void ToolStripAddDevices(BluetoothDeviceInfo[] devices)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<BluetoothDeviceInfo[]>(ToolStripAddDevices), new object[] { devices });
                return;
            }

            List<ToolStripMenuItem> devicesList = new List<ToolStripMenuItem>();

            // Per ogni dispositivo trovato
            foreach (BluetoothDeviceInfo d in devices)
            {
                // Lo aggiungo al menù a tendina della connessione
                ToolStripMenuItem i = new ToolStripMenuItem
                {
                    Name = d.DeviceName,
                    Size = new Size(180, 30),
                    Text = d.DeviceName
                };

                // Associo ad ognuno l'evento di connessione
                i.Click += new EventHandler(this.ManageConnectClick);

                devicesList.Add(i);
            }

            connectToolStripMenuItem.DropDownItems.Clear();
            connectToolStripMenuItem.DropDownItems.AddRange(devicesList.ToArray());
        }

        private void searchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetStatus("Scanning...");
            Thread t = new Thread(() => ScanDevices());
            t.IsBackground = true;
            t.Start();
        }

        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Manage_bt_disconnect();
        }

        private void listViewRxDataViewer_DoubleClick(object sender, EventArgs e)
        {
            listViewRxDataViewer.Items.Clear();
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string fileName = "output.pdf";

            PdfUtils.ExportListView(listViewRxDataViewer, fileName);
            Process.Start(fileName);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox aboutBox = new AboutBox();
            aboutBox.ShowDialog();
        }
    }
}
