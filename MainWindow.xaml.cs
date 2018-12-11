using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.ServiceProcess;
using System.ComponentModel;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows.Threading;

namespace Toggle_SQL_Services
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ServiceController sc1 = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == "OracleServiceXE"); //XboxNetApiSvc
        private ServiceController sc2 = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == "OracleXETNSListener"); //WwanSvc

        private RegistryKey registryKey1 = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\services\OracleServiceXE");
        private RegistryKey registryKey2 = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\services\OracleXETNSListener");

        private DispatcherTimer timer = new DispatcherTimer();
        private int timerCounter = 0;

        private BackgroundWorker worker = new BackgroundWorker();

        private bool sc1Exists = false;
        private bool sc2Exists = false;
        private bool sc1StartupIsManual = false;
        private bool sc2StartupIsManual = false;

        public MainWindow()
        {
            InitializeComponent();

            worker.DoWork               += Worker_DoWork;
            worker.RunWorkerCompleted   += Worker_RunWorkerCompleted;
            worker.ProgressChanged      += Worker_ProgressChanged;

            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;

            timer.Tick += Timer_Tick;
            timer.Interval = TimeSpan.FromSeconds(3);

            pbStatus.Visibility = Visibility.Hidden;

            if ((sc1 != null) && (sc2 != null))
            {
                sc1Exists = true;
                sc2Exists = true;
            }

            //Both exist
            if (sc1Exists && sc2Exists)
            {
                if (GetStartupType(registryKey1) == "MANUAL")
                {
                    sc1StartupIsManual = true;
                }
                if (GetStartupType(registryKey2) == "MANUAL")
                {
                    sc2StartupIsManual = true;
                }

                lblOracleServiceXE.Content = "OracleServiceXE: " + sc1.Status;
                lblOracleXETNSListener.Content = "OracleXETNSListener: " + sc2.Status;

                if (sc1.Status == ServiceControllerStatus.Running)
                {
                    lblOracleServiceXE.Background = new SolidColorBrush(Color.FromRgb(168, 222, 0));
                }
                else
                {
                    lblOracleServiceXE.Background = new SolidColorBrush(Color.FromRgb(222, 0, 0));
                }

                if (sc2.Status == ServiceControllerStatus.Running)
                {
                    lblOracleXETNSListener.Background = new SolidColorBrush(Color.FromRgb(168, 222, 0));
                }
                else
                {
                    lblOracleXETNSListener.Background = new SolidColorBrush(Color.FromRgb(222, 0, 0));
                }
            }
            else {
                lblOracleServiceXE.Content = "OracleServiceXE: " + "Not Installed";
                lblOracleServiceXE.Background = new SolidColorBrush(Color.FromRgb(222, 0, 0));

                lblOracleXETNSListener.Content = "OracleXETNSListener: " + "Not Installed";
                lblOracleXETNSListener.Background = new SolidColorBrush(Color.FromRgb(222, 0, 0));
            }

            
            if (!sc1StartupIsManual)
            {
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    Arguments = "/C sc config \"OracleServiceXE\" start= demand" // /C important
                };
                process.StartInfo = startInfo;
                process.Start();
            }
            if (!sc2StartupIsManual)
            {
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    Arguments = "/C sc config \"OracleXETNSListener\" start= demand" // /C important
                };
                process.StartInfo = startInfo;
                process.Start();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            switch (timerCounter)
            {
                case 0:
                    btnToggleServices.Content = "STILL LOADING...";
                    timerCounter++;
                    break;
                case 1:
                    btnToggleServices.Content = "YUP, STILL LOADING...";
                    timerCounter++;
                    break;
                case 2:
                    btnToggleServices.Content = "IT'S LOADING!";
                    timerCounter++;
                    break;
                default:
                    btnToggleServices.Content = "ლ(ಠ益ಠ)ლ";
                    timerCounter++;
                    break;
            }
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            pbStatus.Visibility = Visibility.Hidden;
            btnToggleServices.IsEnabled = true;
            btnToggleServices.Content = "TOGGLE SERVICES";
            timer.Stop();
            timerCounter = 0;
            btnToggleServices.Opacity = 1;
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            switch (e.ProgressPercentage)
            {
                case 1:
                    lblOracleServiceXE.Background = new SolidColorBrush(Color.FromRgb(222, 0, 0));
                    lblOracleServiceXE.Content = "OracleServiceXE: " + sc1.Status;
                    break;
                case 2:
                    lblOracleServiceXE.Background = new SolidColorBrush(Color.FromRgb(168, 222, 0));
                    lblOracleServiceXE.Content = "OracleServiceXE: " + sc1.Status;
                    break;
                case 3:
                    lblOracleXETNSListener.Background = new SolidColorBrush(Color.FromRgb(222, 0, 0));
                    lblOracleXETNSListener.Content = "OracleXETNSListener: " + sc2.Status;
                    break;
                case 4:
                    lblOracleXETNSListener.Background = new SolidColorBrush(Color.FromRgb(168, 222, 0));
                    lblOracleXETNSListener.Content = "OracleXETNSListener: " + sc2.Status;
                    break;
            }
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            switch (sc1.Status)
            {
                case ServiceControllerStatus.Running:
                    try
                    {
                        sc1.Stop();
                        sc1.WaitForStatus(ServiceControllerStatus.Stopped);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                    worker.ReportProgress(1);

                    break;
                case ServiceControllerStatus.Stopped:
                    try
                    {
                        sc1.Start();
                        sc1.WaitForStatus(ServiceControllerStatus.Running);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }

                    worker.ReportProgress(2);
                    break;
            }

            switch (sc2.Status)
            {
                case ServiceControllerStatus.Running:
                    try
                    {
                        sc2.Stop();
                        sc2.WaitForStatus(ServiceControllerStatus.Stopped);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                    worker.ReportProgress(3);

                    break;
                case ServiceControllerStatus.Stopped:
                    try
                    {
                        sc2.Start();
                        sc2.WaitForStatus(ServiceControllerStatus.Running);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                    worker.ReportProgress(4);

                    break;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void BtnToggleServices_Click(object sender, RoutedEventArgs e)
        {
            if (!sc1Exists && !sc2Exists)
            {
                MessageBox.Show("Services Not Installed", "Errorrrrrr :/");
            }
            else
            {
                btnToggleServices.IsEnabled = false;
                btnToggleServices.Content = "LOADING...";
                btnToggleServices.Opacity = 0.4;
                pbStatus.Visibility = Visibility.Visible;
                pbStatus.IsIndeterminate = true;

                timer.Start();

                worker.RunWorkerAsync();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            if (worker.IsBusy)
            {
                MessageBox.Show("Program running...", "ლ(ಠ益ಠ)ლ");
            }
            else
            {
                Close();
            }
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Toggle SQL Services\n\nGemaakt voor Data\nDev: Symon Blazejczak", "About");
        }

        private string GetStartupType(RegistryKey registryKey)
        {
            //getting startup type
            int startupTypeValue = (int)registryKey.GetValue("Start");
            string startupType = string.Empty;

            switch (startupTypeValue)
            {
                case 0:
                    startupType = "BOOT";
                    break;

                case 1:
                    startupType = "SYSTEM";
                    break;

                case 2:
                    startupType = "AUTOMATIC";
                    break;

                case 3:
                    startupType = "MANUAL";
                    break;

                case 4:
                    startupType = "DISABLED";
                    break;

                default:
                    startupType = "UNKNOWN";
                    break;
            }

            return startupType;

        }

        private void Window_Activated(object sender, EventArgs e)
        {
            WindowDeactivatedMask.Visibility = Visibility.Collapsed;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            WindowDeactivatedMask.Visibility = Visibility.Visible;
        }
    }
}
