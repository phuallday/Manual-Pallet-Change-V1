using MahApps.Metro.Controls;
using System;
using System.Windows;
using System.Windows.Media;
using System.Threading;
using System.Windows.Threading;
using Snap7;
using System.Configuration;
using ControlzEx.Theming;
using System.Linq;
using System.Windows.Input;
using System.Reflection;
using System.Data.OleDb;
using System.IO;

namespace s7dotnet {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow {
        S7Client s7Client = new S7Client();
        private bool idle = false;

        byte[] buffer_InPut;
        byte[] buffer_OutPut;
        byte[] buffer_Memory;

        public MainWindow() {
            InitializeComponent();
            this.DataContext = this;
            s7Client.SetConnectionType(S7Client.CONNTYPE_PG);
            s7Client.ConnectTo(ConfigurationManager.AppSettings["IP"], 0, 1);
        }
        public static Brush mColor(string code) {
            var converter = new System.Windows.Media.BrushConverter();
            var brush = (Brush)converter.ConvertFromString(code);
            return brush;
        }
        private void metroWindow_Loaded(object sender, RoutedEventArgs e) {
            #region[theme]
            metroWindow.Title = $"Pallet Change Manual Control V1 _build-{Assembly.GetEntryAssembly().GetName().Version}";
            menu_Accent.ItemsSource = ThemeManager.Current.Themes
                                            .GroupBy(x => x.ColorScheme)
                                            .OrderBy(a => a.Key)
                                            .Select(a => new AccentColorMenuData { Name = a.Key, ColorBrush = a.First().ShowcaseBrush });

            menu_theme.ItemsSource = ThemeManager.Current.Themes
                                         .GroupBy(x => x.BaseColorScheme)
                                         .Select(x => x.First())
                                         .Select(a => new AppThemeMenuData() { Name = a.BaseColorScheme, BorderColorBrush = a.Resources["MahApps.Brushes.ThemeForeground"] as Brush, ColorBrush = a.Resources["MahApps.Brushes.ThemeBackground"] as Brush });
            #endregion
            tbl_cpu.Text = $"S71200 - {ConfigurationManager.AppSettings["Machine"]}";
            tbl_ip.Text = ConfigurationManager.AppSettings["IP"];
            Thread thread = new Thread(new ThreadStart(() => {
            Connect:
                while (true) {
                    int i = s7Client.Connect();
#if DEBUG
                    Console.WriteLine($"Connected:{s7Client.ErrorText(i)}");
#endif
                    if (i == 0) {
                        Dispatcher.Invoke(delegate {
                            tbl_connect.Text = "Connect: True";
                            statusBar.Background = ThemeManager.Current.DetectTheme(Application.Current).ShowcaseBrush;
                            grid_Connecting.Visibility = Visibility.Hidden;
                            gird_Control.IsEnabled = true;
                            idle = true;
                        });
                        goto ReadData;
                    }
                    else { //update ui
                        Dispatcher.Invoke(delegate {
                            tbl_connect.Text = "Connect: False";
                            statusBar.Background = mColor("#454545");
                            //statusBar.IsEnabled = false;

                            tbl_ConnectPLC.Text = $"Đang kết nối đến PLC {ConfigurationManager.AppSettings["IP"]}";
                            grid_Connecting.Visibility = Visibility.Visible;
                            gird_Control.IsEnabled = false;
                        });
                    }
                    Thread.Sleep(500);
                }
            ReadData:
                while (true) {
                    buffer_InPut = new byte[3];
                    buffer_OutPut = new byte[3];
                    buffer_Memory = new byte[1];
                    while (idle == false) ;
                    idle = false;
                    #region [ReadInPut]
                    int readInput = s7Client.ReadArea(S7Client.S7AreaPE, 0, 0, 3, S7Client.S7WLByte, buffer_InPut);
#if DEBUG
                    Console.WriteLine($"Read InPut:{s7Client.ErrorText(readInput)}");
#endif
                    if (readInput != 0) goto Connect;
                    #endregion
                    #region [ReadOutPut]
                    int readOutPut = s7Client.ReadArea(S7Client.S7AreaPA, 0, 0, 3, S7Client.S7WLByte, buffer_OutPut);
#if DEBUG
                    Console.WriteLine($"Read OutPut:{s7Client.ErrorText(readOutPut)}");
#endif
                    if (readOutPut != 0) goto Connect;
                    #endregion
                    #region [ReadMemory]
                    int readMemory = s7Client.ReadArea(S7Client.S7AreaMK, 0, 200, 1, S7Client.S7WLByte, buffer_Memory);
#if DEBUG
                    Console.WriteLine($"Read Memory:{s7Client.ErrorText(readMemory)}");
#endif
                    if (readMemory != 0) goto Connect;
                    #endregion
                    #region [ReadSQL]
                    #endregion
                    idle = true;
                    #region [UpdateUI]
                    Dispatcher.Invoke(delegate {
                        var color_Theme = ThemeManager.Current.DetectTheme(Application.Current).ShowcaseBrush;
                        statusBar.Background = ThemeManager.Current.DetectTheme(Application.Current).ShowcaseBrush;
                        btn_xl1ON.IsEnabled = !S7.GetBitAt(buffer_OutPut, 0, 0);
                        btn_xl1OFF.IsEnabled = S7.GetBitAt(buffer_OutPut, 0, 0);
                        btn_xl2ON.IsEnabled = !S7.GetBitAt(buffer_OutPut, 0, 1);
                        btn_xl2OFF.IsEnabled = S7.GetBitAt(buffer_OutPut, 0, 1);
                        btn_xl3ON.IsEnabled = !S7.GetBitAt(buffer_OutPut, 0, 2);
                        btn_xl3OFF.IsEnabled = S7.GetBitAt(buffer_OutPut, 0, 2);
                        btn_xl4ON.IsEnabled = !S7.GetBitAt(buffer_OutPut, 0, 3);
                        btn_xl4OFF.IsEnabled = S7.GetBitAt(buffer_OutPut, 0, 3);
                        btn_xl5ON.IsEnabled = !S7.GetBitAt(buffer_OutPut, 0, 4);
                        btn_xl5OFF.IsEnabled = S7.GetBitAt(buffer_OutPut, 0, 4);
                        btn_xl6ON.IsEnabled = !S7.GetBitAt(buffer_OutPut, 2, 0);
                        btn_xl6OFF.IsEnabled = S7.GetBitAt(buffer_OutPut, 2, 0);
                        btn_xl7ON.IsEnabled = !S7.GetBitAt(buffer_OutPut, 2, 1);
                        btn_xl7OFF.IsEnabled = S7.GetBitAt(buffer_OutPut, 2, 1);
                        btn_plON.IsEnabled = !S7.GetBitAt(buffer_OutPut, 0, 5);
                        btn_plOFF.IsEnabled = S7.GetBitAt(buffer_OutPut, 0, 5);
                        btn_manualMode.IsEnabled = !S7.GetBitAt(buffer_Memory, 0, 1);
                        btn_autoMode.IsEnabled = S7.GetBitAt(buffer_Memory, 0, 1);

                        lb_xl1Status.Background = S7.GetBitAt(buffer_OutPut, 0, 0)? Brushes.Green : Brushes.Red;
                        lb_xl2Status.Background = S7.GetBitAt(buffer_OutPut, 0, 1)? Brushes.Green : Brushes.Red;
                        lb_xl3Status.Background = S7.GetBitAt(buffer_OutPut, 0, 2)? Brushes.Green : Brushes.Red;
                        lb_xl4Status.Background = S7.GetBitAt(buffer_OutPut, 0, 3)? Brushes.Green : Brushes.Red;
                        lb_xl5Status.Background = S7.GetBitAt(buffer_OutPut, 0, 4)? Brushes.Green : Brushes.Red;
                        lb_xl6Status.Background = S7.GetBitAt(buffer_OutPut, 2, 0)? Brushes.Green : Brushes.Red;
                        lb_xl7Status.Background = S7.GetBitAt(buffer_OutPut, 2, 1) ? Brushes.Green : Brushes.Red;
                        lb_plStatus.Background = S7.GetBitAt(buffer_OutPut, 0, 5) ? Brushes.Green : Brushes.Red;



                        tile_red.Background = S7.GetBitAt(buffer_OutPut, 0, 7) ? Brushes.Red : mColor("#454545");
                        tile_green.Background = S7.GetBitAt(buffer_OutPut, 1, 0) ? Brushes.Green : mColor("#454545");
                        tile_yellow.Background = S7.GetBitAt(buffer_OutPut, 1, 1) ? Brushes.Yellow : mColor("#454545");

                        tile_ss1a.Background = S7.GetBitAt(buffer_InPut, 0, 3) ? color_Theme : mColor("#454545");
                        tile_ss1b.Background = S7.GetBitAt(buffer_InPut, 0, 4) ? color_Theme : mColor("#454545");
                        tile_ss2b.Background = S7.GetBitAt(buffer_InPut, 0, 5) ? color_Theme : mColor("#454545");
                        tile_ss3a.Background = S7.GetBitAt(buffer_InPut, 0, 6) ? color_Theme : mColor("#454545");
                        tile_ss3b.Background = S7.GetBitAt(buffer_InPut, 0, 7) ? color_Theme : mColor("#454545");
                        tile_ss4a.Background = S7.GetBitAt(buffer_InPut, 1, 0) ? color_Theme : mColor("#454545");
                        tile_ss4b.Background = S7.GetBitAt(buffer_InPut, 1, 1) ? color_Theme : mColor("#454545");
                        tile_ss5b.Background = S7.GetBitAt(buffer_InPut, 1, 2) ? color_Theme : mColor("#454545");
                        tile_jig1.Background = S7.GetBitAt(buffer_InPut, 1, 3) ? color_Theme : mColor("#454545");
                        tile_jig2.Background = S7.GetBitAt(buffer_InPut, 1, 4) ? color_Theme : mColor("#454545");
                        tile_open.Background = S7.GetBitAt(buffer_InPut, 2, 2) ? color_Theme : mColor("#454545");
                        tile_close.Background = S7.GetBitAt(buffer_InPut, 1, 5) ? color_Theme : mColor("#454545");
                        groupBox_InsideMC.IsEnabled = groupBox_Jig1.IsEnabled = groupBox_Jig2.IsEnabled = !btn_manualMode.IsEnabled;
                    });
                    #endregion
                    Thread.Sleep(500);
                }
            }));
            thread.IsBackground = true;
            thread.Start();

            Thread thread1 = new Thread(new ThreadStart(() => {
                while (true) {
                    try {
                        using (OleDbConnection connection = new OleDbConnection(ConfigurationManager.ConnectionStrings["192.168.100.100"].ConnectionString)) {
                            OleDbCommand oleDbCommand = new OleDbCommand($"SELECT [ID] FROM[OST].[dbo].[Assy_Stage] WHERE[Machine] = '{ConfigurationManager.AppSettings["Machine"]}'", connection);
                            connection.Open();
                            OleDbDataReader reader = oleDbCommand.ExecuteReader();
                            while (reader.Read()) {
                                if (reader[0].ToString() == "0") {
                                    while (idle == false) ;
                                    idle = false;
                                    int writeQ22 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 2 * 8 + 2, 1, S7Client.S7WLBit, new byte[] { 0 });
#if DEBUG
                                    Console.WriteLine($"Write Q22:{s7Client.ErrorText(writeQ22)}");
#endif
                                    if (writeQ22 != 0) {
                                        reader.Close();
                                        idle = true;
                                        //goto Connect;
                                    }

                                    int writeQ23 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 2 * 8 + 3, 1, S7Client.S7WLBit, new byte[] { 0 });
#if DEBUG
                                    Console.WriteLine($"Write Q23:{s7Client.ErrorText(writeQ23)}");
#endif
                                    if (writeQ23 != 0) {
                                        reader.Close();
                                        idle = true;
                                        //goto Connect;
                                    }
                                    idle = true;
                                }

                                else if (reader[0].ToString() == "1") {
                                    while (idle == false) ;
                                    idle = false;
                                    int writeQ22 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 2 * 8 + 2, 1, S7Client.S7WLBit, new byte[] { 1 });
#if DEBUG
                                    Console.WriteLine($"Write Q22:{s7Client.ErrorText(writeQ22)}");
#endif
                                    if (writeQ22 != 0) {
                                        reader.Close(); idle = true;
                                        //goto Connect; 
                                    }
                                    int writeQ23 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 2 * 8 + 3, 1, S7Client.S7WLBit, new byte[] { 0 });
#if DEBUG
                                    Console.WriteLine($"Write Q23:{s7Client.ErrorText(writeQ23)}");
#endif
                                    if (writeQ23 != 0) {
                                        reader.Close();
                                        idle = true;
                                        //goto Connect;
                                    }
                                }

                                else if (reader[0].ToString() == "2") {
                                    while (idle == false) ;
                                    idle = false;
                                    int writeQ22 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 2 * 8 + 2, 1, S7Client.S7WLBit, new byte[] { 0 });
#if DEBUG
                                    Console.WriteLine($"Write Q22:{s7Client.ErrorText(writeQ22)}");
#endif
                                    if (writeQ22 != 0) {
                                        reader.Close();
                                        idle = true;
                                        //goto Connect;
                                    }
                                    int writeQ23 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 2 * 8 + 3, 1, S7Client.S7WLBit, new byte[] { 1 });
#if DEBUG
                                    Console.WriteLine($"Write Q23:{s7Client.ErrorText(writeQ23)}");
#endif
                                    if (writeQ23 != 0) {
                                        reader.Close();
                                        idle = true;
                                        //goto Connect;
                                    }
                                    idle = true;
                                }
                            }
                            reader.Close();
                        }
                    }
                    catch (Exception ex) {
                        File.AppendAllText("Log.txt", $"{DateTime.Now}:{ex.Message}\r\n");
                    }
                    if (S7.GetBitAt(buffer_Memory, 0, 3)) { //m200.3
                        try {
                            using (OleDbConnection connection = new OleDbConnection(ConfigurationManager.ConnectionStrings["192.168.100.100"].ConnectionString)) {
                                using (OleDbDataAdapter dataAdapter = new OleDbDataAdapter()) {
                                    OleDbCommand command = new OleDbCommand($"UPDATE [OST].[dbo].[Assy_Stage] SET [PROCESS] = '1' WHERE [Machine] = '{ConfigurationManager.AppSettings["Machine"]}'", connection);
                                    connection.Open();
                                    dataAdapter.UpdateCommand = command;
                                    dataAdapter.UpdateCommand.ExecuteNonQuery();
                                    connection.Close();
                                }
                            }
                            while (idle == false) ;
                            idle = false;
                            int writeM2003 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 200 * 8 + 3, 1, S7Client.S7WLBit, new byte[] { 0 });
#if DEBUG
                            Console.WriteLine($"Write M2003:{s7Client.ErrorText(writeM2003)}");
#endif
                            if (writeM2003 != 0) {
                                idle = true;
                            }
                        }
                        catch (Exception ex) {
                            File.AppendAllText("Log.txt", $"{DateTime.Now}:{ex.Message}\r\n");
                        }
                    }
                    Thread.Sleep(1000);
                }
            }));
            thread1.IsBackground = true;
            thread1.Start();
        }

        private void btn_Setting_Click(object sender, RoutedEventArgs e) {
            firstFlyout.IsOpen = true;
        }

        private void btn_plON_Click(object sender, RoutedEventArgs e) {
            //S7.SetBitAt(ref wOutPut, 3, 0, false);
            while (idle == false) ;
            idle = false;
            int writeQ30 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 0 * 8 + 5, 1, S7Client.S7WLBit, new byte[] { 1 });
#if DEBUG
            Console.WriteLine($"Write Q30:{s7Client.ErrorText(writeQ30)}");
#endif
            if (writeQ30 != 0) { idle = true; return; }
            idle = true;
        }

        private void btn_plOFF_Click(object sender, RoutedEventArgs e) {
            //S7.SetBitAt(ref wOutPut, 3, 0, true);
            while (idle == false) ;
            idle = false;
            int writeQ30 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 0 * 8 + 5, 1, S7Client.S7WLBit, new byte[] { 0 });
#if DEBUG
            Console.WriteLine($"Write Q30:{s7Client.ErrorText(writeQ30)}");
#endif
            if (writeQ30 != 0) { idle = true; return; }
            idle = true;
        }

        private void btn_xl1ON_Click(object sender, RoutedEventArgs e) {
            //S7.SetBitAt(ref wOutPut, 0, 3, false);
            //S7.SetBitAt(ref wOutPut, 0, 4, true);
            while (idle == false) ;
            idle = false;
            #region [WriteQ00]
            int writeQ00 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 0 * 8 + 0, 1, S7Client.S7WLBit, new byte[] { 1 });
#if DEBUG
            Console.WriteLine($"Write Q00:{s7Client.ErrorText(writeQ00)}");
#endif
            if (writeQ00 != 0) { idle = true; return; }
            #endregion
            idle = true;
        }

        private void btn_xl1OFF_Click(object sender, RoutedEventArgs e) {
            //S7.SetBitAt(ref wOutPut, 0, 3, true);
            //S7.SetBitAt(ref wOutPut, 0, 4, false);
            while (idle == false) ;
            idle = false;
            #region [WriteQ00]
            int writeQ00 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 0 * 8 + 0, 1, S7Client.S7WLBit, new byte[] { 0 });
#if DEBUG
            Console.WriteLine($"Write Q00:{s7Client.ErrorText(writeQ00)}");
#endif
            if (writeQ00 != 0) { idle = true; return; }
            #endregion
            idle = true;
        }

        private void btn_xl3ON_Click(object sender, RoutedEventArgs e) {
            //S7.SetBitAt(ref wOutPut, 0, 7, false);
            while (idle == false) ;
            idle = false;
            int writeQ07 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 0 * 8 + 2, 1, S7Client.S7WLBit, new byte[] { 1 });
#if DEBUG
            Console.WriteLine($"Write Q07:{s7Client.ErrorText(writeQ07)}");
#endif
            if (writeQ07 != 0) { idle = true; return; }
            idle = true;
        }

        private void btn_xl3OFF_Click(object sender, RoutedEventArgs e) {
            //S7.SetBitAt(ref wOutPut, 0, 7, true);
            while (idle == false) ;
            idle = false;
            int writeQ07 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 0 * 8 + 2, 1, S7Client.S7WLBit, new byte[] { 0 });
#if DEBUG
            Console.WriteLine($"Write Q07:{s7Client.ErrorText(writeQ07)}");
#endif
            if (writeQ07 != 0) { idle = true; return; }
            idle = true;
        }

        private void btn_xl4ON_Click(object sender, RoutedEventArgs e) {
            //S7.SetBitAt(ref wOutPut, 1, 0, false);
            //S7.SetBitAt(ref wOutPut, 1, 1, true);
            while (idle == false) ;
            idle = false;
            int writeQ10 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 0 * 8 + 3, 1, S7Client.S7WLBit, new byte[] { 1 });
#if DEBUG
            Console.WriteLine($"Write Q10:{s7Client.ErrorText(writeQ10)}");
#endif
            if (writeQ10 != 0) { idle = true; return; }
            idle = true;
        }

        private void btn_xl4OFF_Click(object sender, RoutedEventArgs e) {
            //S7.SetBitAt(ref wOutPut, 1, 0, true);
            //S7.SetBitAt(ref wOutPut, 1, 1, false);
            while (idle == false) ;
            idle = false;
            int writeQ10 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 0 * 8 + 3, 1, S7Client.S7WLBit, new byte[] { 0 });
#if DEBUG
            Console.WriteLine($"Write Q10:{s7Client.ErrorText(writeQ10)}");
#endif
            if (writeQ10 != 0) { idle = true; return; }
            idle = true;
        }

        private void btn_xl2ON_Click(object sender, RoutedEventArgs e) {
            //S7.SetBitAt(ref wOutPut, 0, 5, false);
            //S7.SetBitAt(ref wOutPut, 0, 6, true);
            while (idle == false) ;
            idle = false;
            int writeQ05 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 0 * 8 + 1, 1, S7Client.S7WLBit, new byte[] { 1 });
#if DEBUG
            Console.WriteLine($"Write Q05:{s7Client.ErrorText(writeQ05)}");
#endif
            if (writeQ05 != 0) { idle = true; return; }
            idle = true;
        }

        private void btn_xl2OFF_Click(object sender, RoutedEventArgs e) {
            //S7.SetBitAt(ref wOutPut, 0, 5, true);
            //S7.SetBitAt(ref wOutPut, 0, 6, false);
            while (idle == false) ;
            idle = false;
            int writeQ05 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 0 * 8 + 1, 1, S7Client.S7WLBit, new byte[] { 0 });
#if DEBUG
            Console.WriteLine($"Write Q05:{s7Client.ErrorText(writeQ05)}");
#endif
            if (writeQ05 != 0) { idle = true; return; }
            idle = true;
        }

        private void btn_xl6ON_Click(object sender, RoutedEventArgs e) {
            //S7.SetBitAt(ref wOutPut, 2, 1, false);
            while (idle == false) ;
            idle = false;
            int writeQ21 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 2 * 8 + 0, 1, S7Client.S7WLBit, new byte[] { 1 });
#if DEBUG
            Console.WriteLine($"Write Q21:{s7Client.ErrorText(writeQ21)}");
#endif
            if (writeQ21 != 0) { idle = true; return; }
            idle = true;
        }

        private void btn_xl6OFF_Click(object sender, RoutedEventArgs e) {
            //S7.SetBitAt(ref wOutPut, 2, 1, true);
            while (idle == false) ;
            idle = false;
            int writeQ21 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 2 * 8 + 0, 1, S7Client.S7WLBit, new byte[] { 0 });
#if DEBUG
            Console.WriteLine($"Write Q21:{s7Client.ErrorText(writeQ21)}");
#endif
            if (writeQ21 != 0) { idle = true; return; }
            idle = true;
        }

        private void btn_xl5ON_Click(object sender, RoutedEventArgs e) {
            //S7.SetBitAt(ref wOutPut, 2, 0, false);
            while (idle == false) ;
            idle = false;
            int writeQ20 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 0 * 8 + 4, 1, S7Client.S7WLBit, new byte[] { 1 });
#if DEBUG
            Console.WriteLine($"Write Q20:{s7Client.ErrorText(writeQ20)}");
#endif
            if (writeQ20 != 0) { idle = true; return; }
            idle = true;
        }

        private void btn_xl5OFF_Click(object sender, RoutedEventArgs e) {
            //S7.SetBitAt(ref wOutPut, 2, 0, true);
            while (idle == false) ;
            idle = false;
            int writeQ20 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 0 * 8 + 4, 1, S7Client.S7WLBit, new byte[] { 0 });
#if DEBUG
            Console.WriteLine($"Write Q20:{s7Client.ErrorText(writeQ20)}");
#endif
            if (writeQ20 != 0) { idle = true; return; }
            idle = true;
        }

        private void btn_xl7ON_Click(object sender, RoutedEventArgs e) {
            //S7.SetBitAt(ref wOutPut, 2, 2, false);
            while (idle == false) ;
            idle = false;
            int writeQ22 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 2 * 8 + 1, 1, S7Client.S7WLBit, new byte[] { 1 });
#if DEBUG
            Console.WriteLine($"Write Q22:{s7Client.ErrorText(writeQ22)}");
#endif
            if (writeQ22 != 0) { idle = true; return; }
            idle = true;
        }

        private void btn_xl7OFF_Click(object sender, RoutedEventArgs e) {
            //S7.SetBitAt(ref wOutPut, 2, 2, true);
            while (idle == false) ;
            idle = false;
            int writeQ22 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 2 * 8 + 1, 1, S7Client.S7WLBit, new byte[] { 0 });
#if DEBUG
            Console.WriteLine($"Write Q22:{s7Client.ErrorText(writeQ22)}");
#endif
            if (writeQ22 != 0) { idle = true; return; }
            idle = true;
        }

        private void btn_autoMode_Click(object sender, RoutedEventArgs e) {
            while (idle == false) ;
            idle = false;
            int writeManual = s7Client.WriteArea(S7Client.S7AreaMK, 0, 200 * 8 + 1, 1, S7Client.S7WLBit, new byte[] { 0 });
#if DEBUG
            Console.WriteLine($"Write M200.1:{s7Client.ErrorText(writeManual)}");
#endif
            if (writeManual != 0) { idle = true; return; }
            idle = true;
        }

        private void btn_manualMode_Click(object sender, RoutedEventArgs e) {
            while (idle == false) ;
            int writeManual = s7Client.WriteArea(S7Client.S7AreaMK, 0, 200 * 8 + 1, 1, S7Client.S7WLBit, new byte[] { 1 });
#if DEBUG
            Console.WriteLine($"Write M200.1:{s7Client.ErrorText(writeManual)}");
#endif
            if (writeManual != 0) { idle = true; return; }
            idle = true;
        }
    }
    public class AppThemeMenuData : AccentColorMenuData {
        protected override void DoChangeTheme(object sender) {
            ThemeManager.Current.ChangeThemeBaseColor(Application.Current, this.Name);
        }
    }
    public class AccentColorMenuData {
        public string Name { get; set; }

        public Brush BorderColorBrush { get; set; }

        public Brush ColorBrush { get; set; }

        public AccentColorMenuData() {
            this.ChangeAccentCommand = new SimpleCommand(o => true, this.DoChangeTheme);
        }

        public ICommand ChangeAccentCommand { get; }

        protected virtual void DoChangeTheme(object sender) {
            ThemeManager.Current.ChangeThemeColorScheme(Application.Current, this.Name);
        }
    }
    public class SimpleCommand : ICommand {
        public SimpleCommand(Func<object, bool> canExecute = null, Action<object> execute = null) {
            this.CanExecuteDelegate = canExecute;
            this.ExecuteDelegate = execute;
        }

        public Func<object, bool> CanExecuteDelegate { get; set; }

        public Action<object> ExecuteDelegate { get; set; }

        public bool CanExecute(object parameter) {
            var canExecute = this.CanExecuteDelegate;
            return canExecute == null || canExecute(parameter);
        }

        public event EventHandler CanExecuteChanged {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public void Execute(object parameter) {
            this.ExecuteDelegate?.Invoke(parameter);
        }
    }
}
