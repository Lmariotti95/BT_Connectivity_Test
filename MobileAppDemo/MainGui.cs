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
using iTextSharp.text.xml.xmp;

namespace MobileAppDemo
{
    public partial class MainGui : Form
    {
        BluetoothDeviceInfo[] btDevicesInRange;
        BluetoothDeviceInfo btConnectedDevice;

        BluetoothClient btClient;

        Thread btThreadListener = null;
        FileSystemWatcher fileSystemWatcher = null;

        public MainGui()
        {
            InitializeComponent();

            DataParser.Instance.SetCommand(new List<BtCommand>
            {
                new BtCommand() { Text = "ping",        Callback = CallbackPing },
                new BtCommand() { Text = "START FILE",  Callback = CallbackStartOfFile },
                new BtCommand() { Text = "END OF FILE", Callback = CallbackEndOfFile }
            });

            ImageList imageList = new ImageList();
            imageList.Images.Add("Folder", Properties.Resources.folder_ico_48_48); // Add folder icon
            imageList.Images.Add("File", Properties.Resources.file_ico_48_48);     // Add file icon
            imageList.ImageSize = new Size(32, 32);
            treeViewTickets.ImageList = imageList;

            PopulateTreeView(CommonPaths.ticketFolder);
            treeViewTickets.NodeMouseDoubleClick += TreeViewTickets_NodeMouseDoubleClick;

            FileSystemWatcher fileSystemWatcher = new FileSystemWatcher(CommonPaths.ticketFolder);
            fileSystemWatcher.IncludeSubdirectories = true;
            fileSystemWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite;

            fileSystemWatcher.Created += OnChanged;
            fileSystemWatcher.Deleted += OnChanged;
            fileSystemWatcher.Renamed += OnChanged;

            // Start monitoring
            fileSystemWatcher.EnableRaisingEvents = true;
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            // Reload the folder structure on any change event
            this.BeginInvoke((Action)(() => PopulateTreeView(CommonPaths.ticketFolder)));
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

        private void ResetTimer(System.Windows.Forms.Timer timer)
        {
            timer.Stop();
            timer.Start();
        }

        public void PopulateTreeView(string rootFolderPath)
        {
            // Clear existing nodes
            treeViewTickets.Nodes.Clear();

            // Create the root node based on the folder
            DirectoryInfo rootDir = new DirectoryInfo(rootFolderPath);
            TreeNode rootNode = new TreeNode(rootDir.Name) 
            { 
                Tag = rootDir, 
                ImageKey = "Folder", 
                SelectedImageKey = "Folder" 
            };

            treeViewTickets.Nodes.Add(rootNode);

            // Populate the tree with files and folders
            PopulateTreeNode(rootNode);
            treeViewTickets.ExpandAll();
        }

        private void PopulateTreeNode(TreeNode node)
        {
            DirectoryInfo directory = (DirectoryInfo)node.Tag;

            try
            {
                // Add folders
                foreach (var dir in directory.GetDirectories())
                {
                    TreeNode dirNode = new TreeNode(dir.Name)
                    {
                        Tag = dir,
                        ImageKey = "Folder",
                        SelectedImageKey = "Folder"
                    };

                    node.Nodes.Add(dirNode);
                    PopulateTreeNode(dirNode); // Recursive call for subdirectories
                }

                // Add files
                foreach (var file in directory.GetFiles())
                {
                    TreeNode fileNode = new TreeNode(file.Name)
                    {
                        Tag = file,
                        ImageKey = "File",
                        SelectedImageKey = "File"
                    };

                    node.Nodes.Add(fileNode);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Handle access-denied errors if needed
            }
        }

        private void TreeViewTickets_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            // Check if the clicked node is a file (not a directory)
            if (e.Node.Tag is FileInfo fileInfo)
            {
                try
                {
                    // Open the file with the default associated application
                    Process.Start(fileInfo.FullName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error opening file: " + ex.Message);
                }
            }
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


        public void UpdatePictureBox(PictureBox pBox, Image img)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<PictureBox, Image>(UpdatePictureBox), new object[] { pBox, img });
                return;
            }

            pBox.Image = img;
        }

        private void Manage_bt_disconnect()
        {
            // Se il thread di comunicazione è definito
            if (btThreadListener != null)
            {
                Utils.CloseThread(btThreadListener);

                // Se il client connessto è definito
                Bluetooth.Disconnect(btClient);
                btThreadListener = null;
            }

            commTimeoutTimer.Stop();
            pictureBoxStatus.Image = Properties.Resources.RadioIconRed;
            SetStatus($"Not connected");
        }
        #endregion

        #region COMUNICAZIONE BLUETOOTH
        bool recordingFile = false;
        string rxPayload = "";
        List<byte> rxDataPayload = new List<byte>();

        private void Bt_communication_handler(Stream str)
        {
            // Inizializzo la stringa di ricezione
            string rxMsg = "";
            List<byte> rxData = new List<byte>();

            // Inizializzo i comandi gestiti dal bluetooth
            List <BtCommand> commands = new List<BtCommand>
            {
                new BtCommand() { Text = "ping\r\n",          Callback = CallbackPing },
                new BtCommand() { Text = "START OF FILE\r\n", Callback = CallbackStartOfFile },
                new BtCommand() { Text = "END OF FILE\r\n",   Callback = CallbackEndOfFile }
            };

            while (true)
            {
                try
                {
                    // Leggo byte a byte
                    int data = str.ReadByte();
                    if (data != -1)
                    {
                        //rxMsg += (char)data;
                        rxData.Add((byte)data);

                        // Se è abilitata la registrazione del corpo del messaggio
                        if (recordingFile)
                            rxDataPayload.Add((byte)data);
                        //rxPayload += (char)data;

                        rxMsg = Encoding.Unicode.GetString(rxData.ToArray());

                        // Scorro i comandi
                        foreach (BtCommand cmd in commands)
                        {
                            int x = rxMsg.IndexOf(cmd.Text);

                            // Se la stringa contiene un messaggio valido
                            if (x != -1)
                            {
                                rxMsg = "";// rxMsg.Remove(x, cmd.Text.Length);
                                rxData.Clear();

                                if (recordingFile)
                                {
                                    rxPayload = Encoding.Unicode.GetString(rxDataPayload.ToArray());
                                    int k = rxPayload.IndexOf(cmd.Text);
                                    if (k != -1)
                                        rxPayload = rxPayload.Remove(k, cmd.Text.Length);
                                }
                                
                                if(cmd.Callback != null)
                                {
                                    Invoke((Action)(() => 
                                    {
                                        cmd.Callback(); 
                                    }));
                                }
                            }
                        }
                    }
                }
                catch (IOException) { }
            }
        }
        #endregion

        #region CALLBACKS
        private void Bluetooth_send_response(BluetoothClient client, string response)
        {
            Stream str = client.GetStream();
            byte[] bResponse = Encoding.ASCII.GetBytes(response);
            str.Write(bResponse, 0, bResponse.Length);
        }

        private void CallbackPing()
        {
            ResetTimer(commTimeoutTimer);
            UpdatePictureBox(pictureBoxStatus, Properties.Resources.RadioIconGreen);
            Bluetooth_send_response(btClient, "pong\r\n");
        }
        private void CallbackStartOfFile()
        {
            ResetTimer(commTimeoutTimer);
            UpdatePictureBox(pictureBoxStatus, Properties.Resources.RadioIconGreen);
            recordingFile = true;
        }

        private UInt32 GetRxCrc32(string msg)
        {
            try
            {
                string hexStr = msg.Substring(msg.Length - 10, 8);
                return UInt32.Parse(hexStr, System.Globalization.NumberStyles.HexNumber);
            }
            catch(Exception)
            {
                return Crc32.DEFAULT_CRC32;
            }
        }

        private void CallbackEndOfFile()
        {
            ResetTimer(commTimeoutTimer);
            UpdatePictureBox(pictureBoxStatus, Properties.Resources.RadioIconGreen);
            recordingFile = false;

            // 8 caratteri di CRC32 e 2 caratteri di CR LF 
            string rxPayloadNoCrc32 = rxPayload.Substring(0, rxPayload.Length - 10);

            UInt32 calcCrc32 = Crc32.Append(rxPayloadNoCrc32, Crc32.DEFAULT_CRC32, Crc32.DEFAULT_CRC32_XOR);
            UInt32 rxCrc32 = GetRxCrc32(rxPayload);

            rxPayload = "";
            rxDataPayload.Clear();

            if (calcCrc32 != rxCrc32)
            {
                MessageBox.Show("Unvalid message received", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            List<string> lines = rxPayloadNoCrc32.Split('\n').ToList();

            string[] lblTicketId = new string[]
            {
                "RECEIPT ID",          // en
                "ID SCONTRINO",        // it
                "ID DE RECU ",         // fr
                "ID DE RECIBO",        // es
                "ID DO RECEBIMENTO",   // pt
                "RECEIPT-ID",          // de
                "ID ODBIORU",          // pl
                "FIS KIMLIGI",         // tr
                "ИДЕНТИФИКАТОР ЧЕКА",  // ru
                "回单号"                // zh-CN
            };

            foreach (string line in lines)
            {
                if(lblTicketId.Any(substring => line.Contains(substring)))
                {
                    var fields = line.Trim('\n').Trim('\r').Split(';').ToList();
                    if (fields.Count > 1)
                    {
                        string fileName = $"{fields[1]}.pdf";
                        PdfUtils.ExportRawLines(Path.Combine(CommonPaths.ticketFolder, fileName), lines);
                        break;
                    }
                }
            }

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

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox aboutBox = new AboutBox();
            aboutBox.ShowDialog();
        }

        private void commTimeoutTimer_Tick(object sender, EventArgs e)
        {
            UpdatePictureBox(pictureBoxStatus, Properties.Resources.RadioIconRed);
        }

        private void openFolderInFileExplorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(CommonPaths.ticketFolder);
        }

        private void MainGui_FormClosing(object sender, FormClosingEventArgs e)
        {
            Bluetooth.Disconnect(btClient);
        }
    }
}
