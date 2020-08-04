#define debug
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
using System.Diagnostics;

namespace s7dotnet {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow {
        S7Client s7Client = new S7Client();
        private bool idle = false;
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
            using (Process x1 = Process.GetCurrentProcess()) {
                foreach (Process x2 in Process.GetProcesses()) {
                    if (x2.ProcessName == x1.ProcessName) {
                        if (x1.Id != x2.Id) {
                            MessageBox.Show("Appication already running!");
                            Application.Current.Shutdown();
                        }
                    }
                }
            }
            metroWindow.Title = $"Pallet Change Manual Control V1 _build-{Assembly.GetEntryAssembly().GetName().Version}";
            menu_Accent.ItemsSource = ThemeManager.Current.Themes
                                            .GroupBy(x => x.ColorScheme)
                                            .OrderBy(a => a.Key)
                                            .Select(a => new AccentColorMenuData { Name = a.Key, ColorBrush = a.First().ShowcaseBrush });

            menu_theme.ItemsSource = ThemeManager.Current.Themes
                                         .GroupBy(x => x.BaseColorScheme)
                                         .Select(x => x.First())
                                         .Select(a => new AppThemeMenuData() { Name = a.BaseColorScheme, BorderColorBrush = a.Resources["MahApps.Brushes.ThemeForeground"] as Brush, ColorBrush = a.Resources["MahApps.Brushes.ThemeBackground"] as Brush });
            tbl_cpu.Text = "S71200";
            tbl_ip.Text = ConfigurationManager.AppSettings["IP"];
            Thread thread = new Thread(new ThreadStart(() => {
            Connect:
                while (true) {
                    int i = s7Client.Connect();
#if debug
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
                    byte[] buffer_InPut = new byte[4];
                    byte[] buffer_OutPut = new byte[4];
                    byte[] buffer_Memory = new byte[1];
                    while (idle == false) ;
                    idle = false;
                    #region [ReadInPut]
                    int readInput = s7Client.ReadArea(S7Client.S7AreaPE, 0, 0, 4, S7Client.S7WLByte, buffer_InPut);
#if debug
                    Console.WriteLine($"Read InPut:{s7Client.ErrorText(readInput)}");
#endif
                    if (readInput != 0) goto Connect;
                    #endregion
                    #region [ReadOutPut]
                    int readOutPut = s7Client.ReadArea(S7Client.S7AreaPA, 0, 0, 4, S7Client.S7WLByte, buffer_OutPut);
#if debug
                    Console.WriteLine($"Read OutPut:{s7Client.ErrorText(readOutPut)}");
#endif
                    if (readOutPut != 0) goto Connect;
                    #endregion
                    #region [ReadMemory]
                    int readMemory = s7Client.ReadArea(S7Client.S7AreaMK, 0, 200, 1, S7Client.S7WLByte, buffer_Memory);
#if debug
                    Console.WriteLine($"Read Memory:{s7Client.ErrorText(readMemory)}");
#endif
                    if (readMemory != 0) goto Connect;
                    #endregion
                    idle = true;
                    #region [UpdateUI]
                    Dispatcher.Invoke(delegate {
                        var color_Theme = ThemeManager.Current.DetectTheme(Application.Current).ShowcaseBrush;
                        statusBar.Background = ThemeManager.Current.DetectTheme(Application.Current).ShowcaseBrush;
                        btn_xilanh1.IsOn = (S7.GetBitAt(buffer_OutPut, 0, 3) == true) & (S7.GetBitAt(buffer_OutPut, 0, 4) == false);
                        btn_xilanh2.IsOn = (S7.GetBitAt(buffer_OutPut, 0, 5) == true) & (S7.GetBitAt(buffer_OutPut, 0, 6) == false);
                        btn_xilanh3.IsOn = S7.GetBitAt(buffer_OutPut, 0, 7);
                        btn_xilanh41.IsOn = (S7.GetBitAt(buffer_OutPut, 1, 0) == true) & (S7.GetBitAt(buffer_OutPut, 1, 1) == false);
                        btn_xilanh51.IsOn = S7.GetBitAt(buffer_OutPut, 2, 0);
                        btn_xilanh61.IsOn = S7.GetBitAt(buffer_OutPut, 2, 1);
                        btn_xilanh71.IsOn = S7.GetBitAt(buffer_OutPut, 2, 2);
                        btn_xilanh42.IsOn = (S7.GetBitAt(buffer_OutPut, 2, 3) == true) & (S7.GetBitAt(buffer_OutPut, 2, 4) == false);
                        btn_xilanh52.IsOn = S7.GetBitAt(buffer_OutPut, 2, 5);
                        btn_xilanh62.IsOn = S7.GetBitAt(buffer_OutPut, 2, 6);
                        btn_xilanh72.IsOn = S7.GetBitAt(buffer_OutPut, 2, 7);
                        btn_pallet.IsOn = S7.GetBitAt(buffer_OutPut, 3, 0);
                        btn_manual.IsOn = S7.GetBitAt(buffer_Memory, 0, 1);

                        tile_red.Background = S7.GetBitAt(buffer_OutPut, 0, 0) ? Brushes.Red : mColor("#454545");
                        tile_green.Background = S7.GetBitAt(buffer_OutPut, 0, 1) ? Brushes.Green : mColor("#454545");
                        tile_yellow.Background = S7.GetBitAt(buffer_OutPut, 0, 2) ? Brushes.Yellow : mColor("#454545");

                        tile_start.Background = S7.GetBitAt(buffer_InPut, 0, 0) ? color_Theme : mColor("#454545");
                        tile_stop.Background = S7.GetBitAt(buffer_InPut, 0, 1) ? color_Theme : mColor("#454545");
                        tile_ss1a.Background = S7.GetBitAt(buffer_InPut, 0, 2) ? color_Theme : mColor("#454545");
                        tile_ss1b.Background = S7.GetBitAt(buffer_InPut, 0, 3) ? color_Theme : mColor("#454545");
                        tile_ss2a.Background = S7.GetBitAt(buffer_InPut, 0, 4) ? color_Theme : mColor("#454545");
                        tile_ss2b.Background = S7.GetBitAt(buffer_InPut, 0, 5) ? color_Theme : mColor("#454545");
                        tile_ss41a.Background = S7.GetBitAt(buffer_InPut, 0, 6) ? color_Theme : mColor("#454545");
                        tile_ss41b.Background = S7.GetBitAt(buffer_InPut, 0, 7) ? color_Theme : mColor("#454545");
                        tile_ss51a.Background = S7.GetBitAt(buffer_InPut, 1, 0) ? color_Theme : mColor("#454545");
                        tile_ss51b.Background = S7.GetBitAt(buffer_InPut, 1, 1) ? color_Theme : mColor("#454545");
                        tile_ss61a.Background = S7.GetBitAt(buffer_InPut, 1, 2) ? color_Theme : mColor("#454545");
                        tile_ss61b.Background = S7.GetBitAt(buffer_InPut, 1, 3) ? color_Theme : mColor("#454545");
                        tile_ss42a.Background = S7.GetBitAt(buffer_InPut, 1, 4) ? color_Theme : mColor("#454545");
                        tile_ss42b.Background = S7.GetBitAt(buffer_InPut, 1, 5) ? color_Theme : mColor("#454545");
                        tile_ss52a.Background = S7.GetBitAt(buffer_InPut, 2, 0) ? color_Theme : mColor("#454545");
                        tile_ss52b.Background = S7.GetBitAt(buffer_InPut, 2, 1) ? color_Theme : mColor("#454545");
                        tile_ss62a.Background = S7.GetBitAt(buffer_InPut, 2, 2) ? color_Theme : mColor("#454545");
                        tile_ss62b.Background = S7.GetBitAt(buffer_InPut, 2, 3) ? color_Theme : mColor("#454545");
                        tile_jig1.Background = S7.GetBitAt(buffer_InPut, 2, 4) ? color_Theme : mColor("#454545");
                        tile_jig2.Background = S7.GetBitAt(buffer_InPut, 2, 5) ? color_Theme : mColor("#454545");
                        tile_open.Background = S7.GetBitAt(buffer_InPut, 2, 6) ? color_Theme : mColor("#454545");
                        tile_close.Background = S7.GetBitAt(buffer_InPut, 2, 7) ? color_Theme : mColor("#454545");
                        groupBox_InsideMC.IsEnabled = groupBox_Jig1.IsEnabled = groupBox_Jig2.IsEnabled = btn_manual.IsOn;
                    });
                    #endregion
                    Thread.Sleep(500);
                }
            }));
            thread.IsBackground = true;
            thread.Start();
        }
        private void btn_Toggled(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            switch ((sender as ToggleSwitch).Name) {
                case "btn_xilanh1":
                    if (btn_xilanh1.IsOn) {
                        //S7.SetBitAt(ref wOutPut, 0, 3, true);
                        //S7.SetBitAt(ref wOutPut, 0, 4, false);
                        while (idle == false) ;
                        idle = false;
                        #region [WriteQ03]
                        int writeQ03 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 0 * 8 + 3, 1, S7Client.S7WLBit, new byte[] { 0 });
#if debug
                        Console.WriteLine($"Write Q03:{s7Client.ErrorText(writeQ03)}");
#endif
                        if (writeQ03 != 0) { idle = true; return; }
                        #endregion
                        #region [WriteQ04]
                        int writeQ04 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 0 * 8 + 4, 1, S7Client.S7WLBit, new byte[] { 1 });
#if debug
                        Console.WriteLine($"Write Q04:{s7Client.ErrorText(writeQ04)}");
#endif
                        if (writeQ04 != 0) { idle = true; return; }
                        #endregion
                        idle = true;

                    }
                    else {
                        //S7.SetBitAt(ref wOutPut, 0, 3, false);
                        //S7.SetBitAt(ref wOutPut, 0, 4, true);
                        while (idle == false) ;
                        idle = false;
                        #region [WriteQ03]
                        int writeQ03 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 0 * 8 + 3, 1, S7Client.S7WLBit, new byte[] { 1 });
#if debug
                        Console.WriteLine($"Write Q03:{s7Client.ErrorText(writeQ03)}");
#endif
                        if (writeQ03 != 0) { idle = true; return; }
                        #endregion
                        #region [WriteQ04]
                        int writeQ04 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 0 * 8 + 4, 1, S7Client.S7WLBit, new byte[] { 0 });
#if debug
                        Console.WriteLine($"Write Q04:{s7Client.ErrorText(writeQ04)}");
#endif
                        if (writeQ04 != 0) { idle = true; return; }
                        #endregion
                        idle = true;
                    }
                    break;

                case "btn_xilanh2":
                    if (btn_xilanh2.IsOn) {
                        //S7.SetBitAt(ref wOutPut, 0, 5, true);
                        //S7.SetBitAt(ref wOutPut, 0, 6, false);
                        while (idle == false) ;
                        idle = false;
                        int writeQ05 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 0 * 8 + 5, 1, S7Client.S7WLBit, new byte[] { 0 });
#if debug
                        Console.WriteLine($"Write Q05:{s7Client.ErrorText(writeQ05)}");
#endif
                        if (writeQ05 != 0) { idle = true; return; }
                        int writeQ06 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 0 * 8 + 6, 1, S7Client.S7WLBit, new byte[] { 1 });
#if debug
                        Console.WriteLine($"Write Q06:{s7Client.ErrorText(writeQ06)}");
#endif
                        if (writeQ06 != 0) { idle = true; return; }
                        idle = true;
                    }
                    else {
                        //S7.SetBitAt(ref wOutPut, 0, 5, false);
                        //S7.SetBitAt(ref wOutPut, 0, 6, true);
                        while (idle == false) ;
                        idle = false;
                        int writeQ05 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 0 * 8 + 5, 1, S7Client.S7WLBit, new byte[] { 1 });
#if debug
                        Console.WriteLine($"Write Q05:{s7Client.ErrorText(writeQ05)}");
#endif
                        if (writeQ05 != 0) { idle = true; return; }
                        int writeQ06 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 0 * 8 + 6, 1, S7Client.S7WLBit, new byte[] { 0 });
#if debug
                        Console.WriteLine($"Write Q06:{s7Client.ErrorText(writeQ06)}");
#endif
                        if (writeQ06 != 0) { idle = true; return; }
                        idle = true;
                    }
                    break;
                case "btn_xilanh3":
                    if (btn_xilanh3.IsOn) {
                        //S7.SetBitAt(ref wOutPut, 0, 7, true);
                        while (idle == false) ;
                        idle = false;
                        int writeQ07 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 0 * 8 + 7, 1, S7Client.S7WLBit, new byte[] { 0 });
#if debug
                        Console.WriteLine($"Write Q07:{s7Client.ErrorText(writeQ07)}");
#endif
                        if (writeQ07 != 0) { idle = true; return; }
                        idle = true;
                    }
                    else {
                        //S7.SetBitAt(ref wOutPut, 0, 7, false);
                        while (idle == false) ;
                        idle = false;
                        int writeQ07 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 0 * 8 + 7, 1, S7Client.S7WLBit, new byte[] { 1 });
#if debug
                        Console.WriteLine($"Write Q07:{s7Client.ErrorText(writeQ07)}");
#endif
                        if (writeQ07 != 0) { idle = true; return; }
                        idle = true;
                    }
                    break;
                case "btn_xilanh41":
                    if (btn_xilanh41.IsOn) {
                        //S7.SetBitAt(ref wOutPut, 1, 0, true);
                        //S7.SetBitAt(ref wOutPut, 1, 1, false);
                        while (idle == false) ;
                        idle = false;
                        int writeQ10 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 1 * 8 + 0, 1, S7Client.S7WLBit, new byte[] { 0 });
#if debug
                        Console.WriteLine($"Write Q10:{s7Client.ErrorText(writeQ10)}");
#endif
                        if (writeQ10 != 0) { idle = true; return; }
                        int writeQ11 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 1 * 8 + 1, 1, S7Client.S7WLBit, new byte[] { 1 });
#if debug
                        Console.WriteLine($"Write Q11:{s7Client.ErrorText(writeQ11)}");
#endif
                        if (writeQ11 != 0) { idle = true; return; }
                        idle = true;
                    }
                    else {
                        //S7.SetBitAt(ref wOutPut, 1, 0, false);
                        //S7.SetBitAt(ref wOutPut, 1, 1, true);
                        while (idle == false) ;
                        idle = false;
                        int writeQ10 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 1 * 8 + 0, 1, S7Client.S7WLBit, new byte[] { 1 });
#if debug
                        Console.WriteLine($"Write Q10:{s7Client.ErrorText(writeQ10)}");
#endif
                        if (writeQ10 != 0) { idle = true; return; }
                        int writeQ11 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 1 * 8 + 1, 1, S7Client.S7WLBit, new byte[] { 0 });
#if debug
                        Console.WriteLine($"Write Q11:{s7Client.ErrorText(writeQ11)}");
#endif
                        if (writeQ11 != 0) { idle = true; return; }
                        idle = true;
                    }
                    break;
                case "btn_xilanh51":
                    if (btn_xilanh51.IsOn) {
                        //S7.SetBitAt(ref wOutPut, 2, 0, true);
                        while (idle == false) ;
                        idle = false;
                        int writeQ20 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 2 * 8 + 0, 1, S7Client.S7WLBit, new byte[] { 0 });
#if debug
                        Console.WriteLine($"Write Q20:{s7Client.ErrorText(writeQ20)}");
#endif
                        if (writeQ20 != 0) { idle = true; return; }
                        idle = true;
                    }
                    else {
                        //S7.SetBitAt(ref wOutPut, 2, 0, false);
                        while (idle == false) ;
                        idle = false;
                        int writeQ20 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 2 * 8 + 0, 1, S7Client.S7WLBit, new byte[] { 1 });
#if debug
                        Console.WriteLine($"Write Q20:{s7Client.ErrorText(writeQ20)}");
#endif
                        if (writeQ20 != 0) { idle = true; return; }
                        idle = true;
                    }
                    break;
                case "btn_xilanh61":
                    if (btn_xilanh61.IsOn) {
                        //S7.SetBitAt(ref wOutPut, 2, 1, true);
                        while (idle == false) ;
                        idle = false;
                        int writeQ21 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 2 * 8 + 1, 1, S7Client.S7WLBit, new byte[] { 0 });
#if debug
                        Console.WriteLine($"Write Q21:{s7Client.ErrorText(writeQ21)}");
#endif
                        if (writeQ21 != 0) { idle = true; return; }
                        idle = true;
                    }
                    else {
                        //S7.SetBitAt(ref wOutPut, 2, 1, false);
                        while (idle == false) ;
                        idle = false;
                        int writeQ21 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 2 * 8 + 1, 1, S7Client.S7WLBit, new byte[] { 1 });
#if debug
                        Console.WriteLine($"Write Q21:{s7Client.ErrorText(writeQ21)}");
#endif
                        if (writeQ21 != 0) { idle = true; return; }
                        idle = true;
                    }
                    break;
                case "btn_xilanh71":
                    if (btn_xilanh71.IsOn) {
                        //S7.SetBitAt(ref wOutPut, 2, 2, true);
                        while (idle == false) ;
                        idle = false;
                        int writeQ22 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 2 * 8 + 2, 1, S7Client.S7WLBit, new byte[] { 0 });
#if debug
                        Console.WriteLine($"Write Q22:{s7Client.ErrorText(writeQ22)}");
#endif
                        if (writeQ22 != 0) { idle = true; return; }
                        idle = true;
                    }
                    else {
                        //S7.SetBitAt(ref wOutPut, 2, 2, false);
                        while (idle == false) ;
                        idle = false;
                        int writeQ22 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 2 * 8 + 2, 1, S7Client.S7WLBit, new byte[] { 1 });
#if debug
                        Console.WriteLine($"Write Q22:{s7Client.ErrorText(writeQ22)}");
#endif
                        if (writeQ22 != 0) { idle = true; return; }
                        idle = true;
                    }
                    break;

                case "btn_xilanh42":
                    if (btn_xilanh42.IsOn) {
                        //S7.SetBitAt(ref wOutPut, 2, 3, true);
                        //S7.SetBitAt(ref wOutPut, 2, 4, false);
                        while (idle == false) ;
                        idle = false;
                        int writeQ23 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 2 * 8 + 3, 1, S7Client.S7WLBit, new byte[] { 0 });
#if debug
                        Console.WriteLine($"Write Q23:{s7Client.ErrorText(writeQ23)}");
#endif
                        if (writeQ23 != 0) { idle = true; return; }
                        int writeQ24 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 2 * 8 + 4, 1, S7Client.S7WLBit, new byte[] { 1 });
#if debug
                        Console.WriteLine($"Write Q24:{s7Client.ErrorText(writeQ24)}");
#endif
                        if (writeQ24 != 0) { idle = true; return; }
                        idle = true;
                    }
                    else {
                        //S7.SetBitAt(ref wOutPut, 2, 3, false);
                        //S7.SetBitAt(ref wOutPut, 2, 4, true);
                        while (idle == false) ;
                        idle = false;
                        int writeQ23 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 2 * 8 + 3, 1, S7Client.S7WLBit, new byte[] { 1 });
#if debug
                        Console.WriteLine($"Write Q25:{s7Client.ErrorText(writeQ23)}");
#endif
                        if (writeQ23 != 0) { idle = true; return; }
                        int writeQ24 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 2 * 8 + 4, 1, S7Client.S7WLBit, new byte[] { 0 });
#if debug
                        Console.WriteLine($"Write Q26:{s7Client.ErrorText(writeQ24)}");
#endif
                        if (writeQ24 != 0) { idle = true; return; }
                        idle = true;
                    }
                    break;
                case "btn_xilanh52":
                    if (btn_xilanh52.IsOn) {
                        //S7.SetBitAt(ref wOutPut, 2, 5, true);
                        while (idle == false) ;
                        idle = false;
                        int writeQ25 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 2 * 8 + 5, 1, S7Client.S7WLBit, new byte[] { 0 });
#if debug
                        Console.WriteLine($"Write Q25:{s7Client.ErrorText(writeQ25)}");
#endif
                        if (writeQ25 != 0) { idle = true; return; }
                        idle = true;
                    }
                    else {
                        //S7.SetBitAt(ref wOutPut, 2, 5, false);
                        while (idle == false) ;
                        idle = false;
                        int writeQ25 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 2 * 8 + 5, 1, S7Client.S7WLBit, new byte[] { 1 });
#if debug
                        Console.WriteLine($"Write Q25:{s7Client.ErrorText(writeQ25)}");
#endif
                        if (writeQ25 != 0) { idle = true; return; }
                        idle = true;
                    }
                    break;
                case "btn_xilanh62":
                    if (btn_xilanh62.IsOn) {
                        //S7.SetBitAt(ref wOutPut, 2, 6, true);
                        while (idle == false) ;
                        idle = false;
                        int writeQ26 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 2 * 8 + 6, 1, S7Client.S7WLBit, new byte[] { 0 });
#if debug
                        Console.WriteLine($"Write Q26:{s7Client.ErrorText(writeQ26)}");
#endif
                        if (writeQ26 != 0) { idle = true; return; }
                        idle = true;
                    }
                    else {
                        //S7.SetBitAt(ref wOutPut, 2, 6, false);
                        while (idle == false) ;
                        idle = false;
                        int writeQ26 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 2 * 8 + 6, 1, S7Client.S7WLBit, new byte[] { 1 });
#if debug
                        Console.WriteLine($"Write Q26:{s7Client.ErrorText(writeQ26)}");
#endif
                        if (writeQ26 != 0) { idle = true; return; }
                        idle = true;
                    }
                    break;
                case "btn_xilanh72":
                    if (btn_xilanh72.IsOn) {
                        //S7.SetBitAt(ref wOutPut, 2, 7, true);
                        while (idle == false) ;
                        idle = false;
                        int writeQ27 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 2 * 8 + 7, 1, S7Client.S7WLBit, new byte[] { 0 });
#if debug
                        Console.WriteLine($"Write Q27:{s7Client.ErrorText(writeQ27)}");
#endif
                        if (writeQ27 != 0) { idle = true; return; }
                        idle = true;
                    }
                    else {
                        //S7.SetBitAt(ref wOutPut, 2, 7, false);
                        while (idle == false) ;
                        idle = false;
                        int writeQ27 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 2 * 8 + 7, 1, S7Client.S7WLBit, new byte[] { 1 });
#if debug
                        Console.WriteLine($"Write Q27:{s7Client.ErrorText(writeQ27)}");
#endif
                        if (writeQ27 != 0) { idle = true; return; }
                        idle = true;
                    }
                    break;
                case "btn_pallet":
                    if (btn_pallet.IsOn) {
                        //S7.SetBitAt(ref wOutPut, 3, 0, true);
                        while (idle == false) ;
                        idle = false;
                        int writeQ30 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 3 * 8 + 0, 1, S7Client.S7WLBit, new byte[] { 0 });
#if debug
                        Console.WriteLine($"Write Q30:{s7Client.ErrorText(writeQ30)}");
#endif
                        if (writeQ30 != 0) { idle = true; return; }
                        idle = true;
                    }
                    else {
                        //S7.SetBitAt(ref wOutPut, 3, 0, false);
                        while (idle == false) ;
                        idle = false;
                        int writeQ30 = s7Client.WriteArea(S7Client.S7AreaPA, 0, 3 * 8 + 0, 1, S7Client.S7WLBit, new byte[] { 1 });
#if debug
                        Console.WriteLine($"Write Q30:{s7Client.ErrorText(writeQ30)}");
#endif
                        if (writeQ30 != 0) { idle = true; return; }
                        idle = true;
                    }
                    break;
                case "btn_manual":
                    if (btn_manual.IsOn) {
                        //S7.SetBitAt(ref wManual, 0, 0, true);
                        //S7.SetBitAt(ref wStop, 0, 0, true);
                        while (idle == false) ;
                        idle = false;
                        int writeManual = s7Client.WriteArea(S7Client.S7AreaMK, 0, 200 * 8 + 1, 1, S7Client.S7WLBit, new byte[] { 0 });
#if debug
                        Console.WriteLine($"Write M200.1:{s7Client.ErrorText(writeManual)}");
#endif
                        if (writeManual != 0) { idle = true; return; }
                        int writeStop = s7Client.WriteArea(S7Client.S7AreaMK, 0, 200 * 8 + 0, 1, S7Client.S7WLBit, new byte[] { 0 });
#if debug
                        Console.WriteLine($"Write M200.1:{s7Client.ErrorText(writeStop)}");
#endif
                        if (writeStop != 0) { idle = true; return; }
                        idle = true;
                    }
                    else {
                        //S7.SetBitAt(ref wManual, 0, 0, false);
                        //S7.SetBitAt(ref wStop, 0, 0, false);
                        while (idle == false) ;
                        int writeManual = s7Client.WriteArea(S7Client.S7AreaMK, 0, 200 * 8 + 1, 1, S7Client.S7WLBit, new byte[] { 1 });
#if debug
                        Console.WriteLine($"Write M200.1:{s7Client.ErrorText(writeManual)}");
#endif
                        if (writeManual != 0) { idle = true; return; }
                        int writeStop = s7Client.WriteArea(S7Client.S7AreaMK, 0, 200 * 8 + 0, 1, S7Client.S7WLBit, new byte[] { 1 });
#if debug
                        Console.WriteLine($"Write M200.1:{s7Client.ErrorText(writeStop)}");
#endif
                        if (writeStop != 0) { idle = true; return; }
                        idle = true;
                    }
                    break;
            }
        }


        private void btn_Setting_Click(object sender, RoutedEventArgs e) {
            firstFlyout.IsOpen = true;
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
