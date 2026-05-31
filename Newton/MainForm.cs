using Microsoft.VisualBasic;
using Microsoft.Win32;
using ScottPlot.WinForms;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Newton
{
    public partial class MainForm : Form
    {
        public class BtComPort
        {
            public string Name { get; set; } = "";
            public string Mac { get; set; } = "";
            public string Port { get; set; } = "";

            public override string ToString()
            {
                return $"{Name} ({Port})";
            }
        }
        public class ForceSample
        {
            public long T_us { get; set; }
            public int Raw { get; set; }
            public double Force_N { get; set; }
        }
        public class ForceRingBuffer
        {
            private readonly ForceSample[] buffer;
            private int index = 0;
            private int count = 0;
            private readonly object lockObj = new object();

            public ForceRingBuffer(int capacity)
            {
                buffer = new ForceSample[capacity];
            }

            public void Add(ForceSample sample)
            {
                lock (lockObj)
                {
                    buffer[index] = sample;
                    index = (index + 1) % buffer.Length;

                    if (count < buffer.Length)
                        count++;
                }
            }

            public ForceSample[] GetSnapshot()
            {
                lock (lockObj)
                {
                    ForceSample[] result = new ForceSample[count];

                    int start = (index - count + buffer.Length) % buffer.Length;

                    for (int i = 0; i < count; i++)
                    {
                        int pos = (start + i) % buffer.Length;
                        result[i] = buffer[pos];
                    }

                    return result;
                }
            }

            public void Clear()
            {
                lock (lockObj)
                {
                    index = 0;
                    count = 0;
                }
            }
        }
        private ForceRingBuffer forceBuffer = new ForceRingBuffer(8000);
        private string settingsFile = Path.Combine(Application.StartupPath, "setting.txt");
        private bool isReading = false;
        private bool isOpenA = false;
        private bool isOpenB = false;
        private ForceRingBuffer forceBufferA = new ForceRingBuffer(8000);
        private ForceRingBuffer forceBufferB = new ForceRingBuffer(8000);
        private double displayWindowSec = 10.0;
        private bool isReadingA = false;
        private bool isReadingB = false;
        private Thread readThreadA;
        private Thread readThreadB;
        private double factorA = 1.0;
        private double factorB = 1.0;
        private FormsPlot formsPlot = new FormsPlot();
        private System.Windows.Forms.Timer plotTimer = new System.Windows.Forms.Timer();
        private void PlotTimer_Tick(object sender, EventArgs e)
        {
            double windowSec = displayWindowSec;
            formsPlot.Plot.Axes.SetLimitsX(0, displayWindowSec);
            formsPlot.Plot.Clear();
            bool hasData = false;
            if (isOpenA)
            {
                ForceSample[] samplesA = forceBufferA.GetSnapshot();

                if (samplesA.Length >= 2)
                {
                    double latestA = samplesA[samplesA.Length - 1].T_us / 1_000_000.0;

                    double[] xsA = samplesA
                        .Select(s =>
                        displayWindowSec + (s.T_us / 1_000_000.0 - latestA))
                        .ToArray();

                    double[] ysA = samplesA
                        .Select(s => s.Force_N * factorA)
                        .ToArray();

                    formsPlot.Plot.Add.Scatter(xsA, ysA);
                    hasData = true;
                }
            }

            if (isOpenB)
            {
                ForceSample[] samplesB = forceBufferB.GetSnapshot();

                if (samplesB.Length >= 2)
                {
                    double latestB = samplesB[samplesB.Length - 1].T_us / 1_000_000.0;

                    double[] xsB = samplesB
                        .Select(s => windowSec + (s.T_us / 1_000_000.0 - latestB))
                        .ToArray();

                    double[] ysB = samplesB
                        .Select(s => -s.Force_N * factorB)   // B csatorna előjelfordítva
                        .ToArray();

                    formsPlot.Plot.Add.Scatter(xsB, ysB);
                    hasData = true;
                }
            }

            if (!hasData)
                return;

            formsPlot.Plot.Axes.SetLimitsX(0, windowSec);
            if (chkPhoneMode.Checked)
            {
                // gyorsulásmérő
                formsPlot.Plot.Axes.SetLimitsY(-15, 15);
            }
            else
            {
                // Newton erőmérő
                formsPlot.Plot.Axes.SetLimitsY(-5, 5);
            }


            formsPlot.Refresh();

            if (autoScrollPlot)
            {
                formsPlot.Plot.Axes.SetLimitsX(0, windowSec);
                if (chkPhoneMode.Checked)
                {
                    // gyorsulásmérő
                    formsPlot.Plot.Axes.SetLimitsY(-15, 15);
                }
                else
                {
                    // Newton erőmérő
                    formsPlot.Plot.Axes.SetLimitsY(-5, 5);
                }


            }

            formsPlot.Refresh();
        }
        private void TareDevice(string slot)
        {
            try
            {
                if (slot == "A")
                {
                    if (serialPortA == null || !serialPortA.IsOpen)
                    {
                        MessageBox.Show("Az A csatorna nincs megnyitva.");
                        return;
                    }

                    serialPortA.Write("Z\n");
                    forceBufferA.Clear();
                    lblStatusA.Text = "A: tárázva";
                }
                else if (slot == "B")
                {
                    if (serialPortB == null || !serialPortB.IsOpen)
                    {
                        MessageBox.Show("A B csatorna nincs megnyitva.");
                        return;
                    }

                    serialPortB.Write("Z\n");
                    forceBufferB.Clear();
                    lblStatusB.Text = "B: tárázva";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Tárázási hiba:\n" + ex.Message);
            }
        }
        private SerialPort serialPortA;
        private SerialPort serialPortB;
        private BtComPort deviceInfoA;
        private BtComPort deviceInfoB;
        private bool autoScrollPlot = true;
        private void ReadLoopA()
        {
            ReadLoop(serialPortA, forceBufferA, "A");
        }
        private void ReadLoopB()
        {
            ReadLoop(serialPortB, forceBufferB, "B");
        }
        private void ReadLoop(SerialPort sp, ForceRingBuffer buffer, string slot)
        {
            while ((slot == "A" && isReadingA) || (slot == "B" && isReadingB))
            {
                try
                {
                    string line = sp.ReadLine().Trim();

                    if (TryParseForceLine(line, out ForceSample sample))
                    {
                        buffer.Add(sample);

                        BeginInvoke(new Action(() =>
                        {
                            if (slot == "A")
                            {
                                double displayForce =
                                    sample.Force_N * factorA;


                                if (chkPhoneMode.Checked)
                                    lblStatusA.Text = $"A: Ay={displayForce:F3} m/s²";
                                else
                                    lblStatusA.Text = $"A: F={displayForce:F3} N";


                            }
                            else
                            {
                                double displayForce =
                                    sample.Force_N * factorB;

                                lblStatusB.Text =
                                    $"B: F={displayForce:F3} N";
                            }
                        }));
                    }
                }
                catch
                {
                }
            }
        }
        private void LoadSettings()
        {
            if (!File.Exists(settingsFile))
                return;

            string[] lines = File.ReadAllLines(settingsFile);

            foreach (string line in lines)
            {
                string[] parts = line.Split('=');

                if (parts.Length != 2)
                    continue;

                string key = parts[0].Trim();
                string value = parts[1].Trim();

                if (key == "TimeBaseIndex")
                {
                    if (int.TryParse(value, out int idx))
                    {
                        if (idx >= 0 && idx < comboTimeWindow.Items.Count)
                            comboTimeWindow.SelectedIndex = idx;
                    }
                }
                else if (key == "FactorA")
                {
                    if (double.TryParse(
                        value.Replace(",", "."),
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out double f))
                    {
                        factorA = f;
                        txtFactorA.Text = f.ToString("F3");
                    }
                }
                else if (key == "FactorB")



                {
                    if (double.TryParse(
                        value.Replace(",", "."),
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out double f))
                    {
                        factorB = f;
                        txtFactorB.Text = f.ToString("F3");
                    }
                }
            }
        }
        private void SaveSettings()
        {

            List<string> lines = new List<string>();

            lines.Add($"TimeBaseIndex={comboTimeWindow.SelectedIndex}");
            lines.Add($"FactorA={factorA.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            lines.Add($"FactorB={factorB.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

            File.WriteAllLines(settingsFile, lines);
        }
        private void SaveBuffersToCsv(string fileName)
        {
            ForceSample[] a = forceBufferA.GetSnapshot();
            ForceSample[] b = forceBufferB.GetSnapshot();

            using (StreamWriter sw = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                sw.WriteLine("channel;t_us;t_s;raw;force_N");

                foreach (ForceSample s in a)
                {
                    double t_s = s.T_us / 1_000_000.0;
                    double f = s.Force_N * factorA;

                    sw.WriteLine(
                        $"A;{s.T_us};{t_s.ToString(CultureInfo.InvariantCulture)};{s.Raw};{f.ToString(CultureInfo.InvariantCulture)}");
                }

                foreach (ForceSample s in b)
                {
                    double t_s = s.T_us / 1_000_000.0;
                    double f = -s.Force_N * factorB;

                    sw.WriteLine(
                        $"B;{s.T_us};{t_s.ToString(CultureInfo.InvariantCulture)};{s.Raw};{f.ToString(CultureInfo.InvariantCulture)}");
                }
            }

            MessageBox.Show("CSV mentve.");
        }


        //**************************************
        public MainForm()
        {

            InitializeComponent();
            groupBoxControls.Dock = DockStyle.Top;
            groupBoxControls.Height = 120;

            panelPlot.Dock = DockStyle.Fill;
            panelPlot.BringToFront();

            formsPlot.Dock = DockStyle.Fill;
            panelPlot.Controls.Add(formsPlot);

            formsPlot.Plot.Title("Erő-idő grafikon");
            formsPlot.Plot.XLabel("t (s)");
            formsPlot.Plot.YLabel("F (N)");
            plotTimer.Interval = 50;
            plotTimer.Tick += PlotTimer_Tick;
            plotTimer.Start();
            this.FormClosing += MainForm_FormClosing;

        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadDevicesToCombo();
            comboTimeWindow.Items.Add("2 s");
            comboTimeWindow.Items.Add("5 s");
            comboTimeWindow.Items.Add("10 s");
            comboTimeWindow.Items.Add("20 s");
            comboTimeWindow.Items.Add("30 s");
            comboTimeWindow.Items.Add("60 s");
            comboTimeWindow.SelectedIndex = 2;//10 sec
            txtFactorA.Text = "1,000";
            txtFactorB.Text = "1,000";
            LoadSettings();

        }
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            DisconnectAllDevices();
            SaveSettings();

           
        }
        private void OpenSelectedDeviceToSlot(string slot)
        {
            SerialPort sp = null;

            try
            {
                BtComPort dev = comboDevices.SelectedItem as BtComPort;

                if (dev == null)
                {
                    MessageBox.Show("Nincs kiválasztott eszköz.");
                    return;
                }

                bool phoneMode = chkPhoneMode.Checked;

                sp = new SerialPort(dev.Port, 115200);
                sp.NewLine = "\n";
                sp.ReadTimeout = 1000;
                sp.WriteTimeout = 1000;

                sp.Open();

                sp.DiscardInBuffer();
                sp.DiscardOutBuffer();

                string response;

                if (phoneMode)
                {
                    response = "Telefonos / passzív mód: STAT parancs kihagyva.";
                }
                else
                {
                    try
                    {
                        sp.Write("STAT\n");
                        response = sp.ReadLine().Trim();
                    }
                    catch (Exception ex)
                    {
                        sp.Close();
                        MessageBox.Show("STAT lekérdezési hiba:\n" + ex.Message);
                        return;
                    }
                }

                if (slot == "A")
                {
                    if (serialPortA != null && serialPortA.IsOpen)
                        serialPortA.Close();

                    serialPortA = sp;
                    deviceInfoA = dev;
                    isOpenA = true;

                    lblStatusA.Text = phoneMode
                        ? $"A: {dev.Name} ({dev.Port}) [telefonos mód]"
                        : $"A: {dev.Name} ({dev.Port})";

                    MessageBox.Show(response, "A csatorna");
                }
                else if (slot == "B")
                {
                    if (serialPortB != null && serialPortB.IsOpen)
                        serialPortB.Close();

                    serialPortB = sp;
                    deviceInfoB = dev;
                    isOpenB = true;

                    lblStatusB.Text = phoneMode
                        ? $"B: {dev.Name} ({dev.Port}) [telefonos mód]"
                        : $"B: {dev.Name} ({dev.Port})";

                    MessageBox.Show(response, "B csatorna");
                }
                else
                {
                    sp.Close();
                    MessageBox.Show("Ismeretlen csatorna: " + slot);
                }
            }
            catch (Exception ex)
            {
                if (sp != null && sp.IsOpen)
                    sp.Close();

                MessageBox.Show("Kapcsolódási hiba:\n" + ex.Message);
            }
        }
        private List<BtComPort> GetBluetoothComPorts()
        {
            var namesByMac = new Dictionary<string, string>();
            var portsByMac = new Dictionary<string, string>();

            using (RegistryKey bthenum =
                Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Enum\BTHENUM"))
            {
                if (bthenum == null)
                    return new List<BtComPort>();

                foreach (string devKeyName in bthenum.GetSubKeyNames())
                {
                    if (!devKeyName.StartsWith("Dev_"))
                        continue;

                    string mac = devKeyName.Substring(4).ToUpper();

                    using (RegistryKey devKey = bthenum.OpenSubKey(devKeyName))
                    {
                        if (devKey == null) continue;

                        foreach (string subName in devKey.GetSubKeyNames())
                        {
                            using (RegistryKey subKey = devKey.OpenSubKey(subName))
                            {
                                string friendly =
                                    subKey?.GetValue("FriendlyName")?.ToString();

                                if (!string.IsNullOrWhiteSpace(friendly))
                                    namesByMac[mac] = friendly;
                            }
                        }
                    }
                }

                foreach (string keyName in bthenum.GetSubKeyNames())
                {
                    using (RegistryKey serviceKey = bthenum.OpenSubKey(keyName))
                    {
                        if (serviceKey == null) continue;

                        foreach (string instanceName in serviceKey.GetSubKeyNames())
                        {
                            Match m = Regex.Match(
                                instanceName,
                                @"&([0-9A-Fa-f]{12})_C");

                            if (!m.Success)
                                continue;

                            string mac = m.Groups[1].Value.ToUpper();

                            using (RegistryKey instKey =
                                serviceKey.OpenSubKey(
                                    instanceName + @"\Device Parameters"))
                            {
                                string port =
                                    instKey?.GetValue("PortName")?.ToString();

                                if (!string.IsNullOrWhiteSpace(port))
                                    portsByMac[mac] = port;
                            }
                        }
                    }
                }
            }

            var result = new List<BtComPort>();

            foreach (var p in portsByMac)
            {
                string mac = p.Key;

                result.Add(new BtComPort
                {
                    Mac = mac,
                    Port = p.Value,
                    Name = namesByMac.ContainsKey(mac)
                        ? namesByMac[mac]
                        : "(ismeretlen Bluetooth eszköz)"
                });
            }

            return result
                .OrderBy(x => x.Name)
                .ToList();
        }//Bt kereső függvény
        private void LoadDevicesToCombo()
        {
            comboDevices.Items.Clear();

            var btDevices = GetBluetoothComPorts();

            // Először mindig a névvel azonosított Bluetooth-eszközök
            foreach (var dev in btDevices)
                comboDevices.Items.Add(dev);

            // Telefonos módban minden további COM-port is megjelenik
            if (chkPhoneMode.Checked)
            {
                foreach (string port in SerialPort.GetPortNames().OrderBy(p => p))
                {
                    bool alreadyListed = btDevices.Any(d => d.Port == port);

                    if (!alreadyListed)
                    {
                        comboDevices.Items.Add(new BtComPort
                        {
                            Name = "Ismeretlen / egyéb COM-port",
                            Port = port,
                            Mac = ""
                        });
                    }
                }
            }

            if (comboDevices.Items.Count > 0)
                comboDevices.SelectedIndex = 0;
        }
        private SerialPort? serialPort;
        private bool TryParseForceLine(string line, out ForceSample sample)
        {
            sample = null;

            string[] p = line.Split(';');

            if (p.Length < 3)
                return false;

            if (!long.TryParse(p[0], out long t_us))
                return false;

            if (!int.TryParse(p[1], out int raw))
                return false;

            if (!double.TryParse(
                    p[2],
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double force))
                return false;

            sample = new ForceSample
            {
                T_us = t_us,
                Raw = raw,
                Force_N = force
            };

            return true;
        }
        private void DisconnectAllDevices()
        {
            autoScrollPlot = false;
            plotTimer.Stop();

            isReadingA = false;
            isReadingB = false;

            bool phoneMode = chkPhoneMode.Checked;

            try
            {
                if (serialPortA != null && serialPortA.IsOpen)
                {
                    if (!phoneMode)
                        serialPortA.Write("X\n");

                    serialPortA.Close();
                }
            }
            catch { }

            try
            {
                if (serialPortB != null && serialPortB.IsOpen)
                {
                    if (!phoneMode)
                        serialPortB.Write("X\n");

                    serialPortB.Close();
                }
            }
            catch { }

            serialPortA = null;
            serialPortB = null;

            isOpenA = false;
            isOpenB = false;

            isReadingA = false;
            isReadingB = false;

            deviceInfoA = null;
            deviceInfoB = null;

            lblStatusA.Text = "A: nincs kapcsolat";
            lblStatusB.Text = "B: nincs kapcsolat";
        }
        private void btnStart_Click(object sender, EventArgs e)
        {
            if (!isOpenA && !isOpenB)
            {
                MessageBox.Show("Nincs megnyitott mérőeszköz.");
                return;
            }

            bool phoneMode = chkPhoneMode.Checked;

            if (isOpenA && serialPortA != null && serialPortA.IsOpen)
            {
                forceBufferA.Clear();
                isReadingA = true;

                serialPortA.DiscardInBuffer();

                if (!phoneMode)
                    serialPortA.Write("S\n");

                readThreadA = new Thread(ReadLoopA);
                readThreadA.IsBackground = true;
                readThreadA.Start();

                lblStatusA.Text = phoneMode
                    ? $"A: {deviceInfoA.Name} - telefonos mérés fut"
                    : $"A: {deviceInfoA.Name} - mérés fut";
            }

            if (isOpenB && serialPortB != null && serialPortB.IsOpen)
            {
                forceBufferB.Clear();
                isReadingB = true;

                serialPortB.DiscardInBuffer();

                if (!phoneMode)
                    serialPortB.Write("S\n");

                readThreadB = new Thread(ReadLoopB);
                readThreadB.IsBackground = true;
                readThreadB.Start();

                lblStatusB.Text = phoneMode
                    ? $"B: {deviceInfoB.Name} - telefonos mérés fut"
                    : $"B: {deviceInfoB.Name} - mérés fut";
            }

            autoScrollPlot = true;
            plotTimer.Start();
        }
        private void btnStop_Click(object sender, EventArgs e)
        {
            autoScrollPlot = false;
            plotTimer.Stop();

            isReadingA = false;
            isReadingB = false;

            bool phoneMode = chkPhoneMode.Checked;

            if (!phoneMode)
            {
                try
                {
                    if (serialPortA != null && serialPortA.IsOpen)
                        serialPortA.Write("X\n");
                }
                catch { }

                try
                {
                    if (serialPortB != null && serialPortB.IsOpen)
                        serialPortB.Write("X\n");
                }
                catch { }
            }

            if (isOpenA && deviceInfoA != null)
            {
                lblStatusA.Text = phoneMode
                    ? $"A: {deviceInfoA.Name} - telefonos mód áll"
                    : $"A: {deviceInfoA.Name} - áll";
            }

            if (isOpenB && deviceInfoB != null)
            {
                lblStatusB.Text = phoneMode
                    ? $"B: {deviceInfoB.Name} - telefonos mód áll"
                    : $"B: {deviceInfoB.Name} - áll";
            }
        }
        private void btnOpenA_Click(object sender, EventArgs e)
        {
            OpenSelectedDeviceToSlot("A");
        }
        private void btnOpenB_Click(object sender, EventArgs e)
        {
            OpenSelectedDeviceToSlot("B");
        }
        private void btnTareA_Click(object sender, EventArgs e)
        {
            TareDevice("A");
        }
        private void btnTareB_Click(object sender, EventArgs e)
        {
            TareDevice("B");
        }
        private void comboTimeWindow_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboTimeWindow.SelectedIndex)
            {
                case 0:
                    displayWindowSec = 2;
                    break;

                case 1:
                    displayWindowSec = 5;
                    break;

                case 2:
                    displayWindowSec = 10;
                    break;

                case 3:
                    displayWindowSec = 20;
                    break;

                case 4:
                    displayWindowSec = 30;
                    break;

                case 5:
                    displayWindowSec = 60;
                    break;
            }
        }
        private void clearGraphToolStripMenuItem_Click(object sender, EventArgs e)
        {
            forceBufferA.Clear();
            forceBufferB.Clear();
            PlotTimer_Tick(null, EventArgs.Empty);
            formsPlot.Refresh();
        }
        private void refresDevicesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadDevicesToCombo();
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
        private void txtFactorB_TextChanged(object sender, EventArgs e)
        {
            if (double.TryParse(txtFactorB.Text.Replace(",", "."),
        System.Globalization.NumberStyles.Float,
        System.Globalization.CultureInfo.InvariantCulture,
        out double f))
            {
                factorB = f;
            }
        }
        private void txtFactorA_TextChanged(object sender, EventArgs e)
        {
            if (double.TryParse(txtFactorA.Text.Replace(",", "."),
        System.Globalization.NumberStyles.Float,
        System.Globalization.CultureInfo.InvariantCulture,
        out double f))
            {
                factorA = f;
            }
        }
        private void saveAsCSVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV fájl (*.csv)|*.csv";
                sfd.FileName = "newton_force_measurement.csv";

                if (sfd.ShowDialog() != DialogResult.OK)
                    return;

                SaveBuffersToCsv(sfd.FileName);
            }
        }
        private void helpToolStripMenuItem_Click(
object sender,
EventArgs e)
        {
            System.Diagnostics.Process.Start("notepad.exe", Path.Combine(Application.StartupPath, "help.txt"));

        }

        private void chkPhoneMode_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPhoneMode.Checked)
            {
                // gyorsulásmérő
                chkPhoneMode.Text = "Force";
                formsPlot.Plot.Title("Gyorsulás-idő grafikon");
                formsPlot.Plot.Axes.SetLimitsY(-15, 15);
                formsPlot.Plot.YLabel("a m/s²");
                formsPlot.Refresh();
                btnTareA.Enabled = false;
                btnTareB.Enabled = false;
            }
            else
            {
                // Newton erőmérő
                chkPhoneMode.Text = "Mobil";
                formsPlot.Plot.Title("Erő-idő grafikon");
                formsPlot.Plot.Axes.SetLimitsY(-5, 5);
                formsPlot.Plot.YLabel("F (N)");
                formsPlot.Refresh();
                btnTareA.Enabled = true;
                btnTareB.Enabled = true;
            }

            LoadDevicesToCombo();
        }

        private void btnDisconnectAll_Click(object sender, EventArgs e)
        {
            DisconnectAllDevices();
            LoadDevicesToCombo();
            MessageBox.Show("Minden kapcsolat bontva.");
        }
    }
}

