using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

namespace Minecraft_Auto_Click
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private double attackSpeed = 5;
        private bool isStart = false;
        private readonly Timer detectMinecraftPId, autoClick;
        private IntPtr minecraftIntPtr = IntPtr.Zero;

        #region InteropServices
        #region Click
        // https://stackoverflow.com/a/34739416
        private const int WM_LBUTTONDOWN = 0x201;
        private const int WM_LBUTTONUP = 0x202;
        private const int MK_LBUTTON = 0x0001;
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private static IntPtr CreateLParam(int LoWord, int HiWord)
        {
            return (IntPtr)((HiWord << 16) | (LoWord & 0xffff));
        }
        #endregion

        #region HotKey
        // https://blog.magnusmontin.net/2015/03/31/implementing-global-hot-keys-in-wpf/

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;

        // F8 https://learn.microsoft.com/zh-tw/windows/win32/inputdev/virtual-key-codes?redirectedfrom=MSDN
        private const uint VK_F6 = 0x75;

        private HwndSource? source;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            IntPtr handle = new WindowInteropHelper(this).Handle;
            source = HwndSource.FromHwnd(handle);
            source.AddHook(HwndHook);

            RegisterHotKey(handle, HOTKEY_ID, 0, VK_F6); //F6
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_ID:
                            int vkey = (((int)lParam >> 16) & 0xFFFF);
                            if (vkey == VK_F6)
                            {
                                ToggleAutoClick();
                            }
                            handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }
        #endregion
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            
            attackSpeed = Properties.Settings.Default.ClickSpeed;
            sliderAttackSpeed.Dispatcher.Invoke(() => sliderAttackSpeed.Value = attackSpeed);

            detectMinecraftPId = new Timer((obj) => 
            {
                var processes = Process.GetProcessesByName("javaw");
                if (!processes.Any())
                    minecraftIntPtr = IntPtr.Zero;
                else
                    minecraftIntPtr = processes.First().MainWindowHandle;

                labMCHMID.Dispatcher.Invoke(() => labMCHMID.Content = minecraftIntPtr.ToString());
            }, null, 1000, 30000);

            autoClick = new Timer((obj) =>
            {
                if (minecraftIntPtr == IntPtr.Zero)
                    return;

                IntPtr lParam = (IntPtr)((1 << 16) | 1);
                PostMessage(minecraftIntPtr, WM_LBUTTONDOWN, IntPtr.Zero, lParam);
                PostMessage(minecraftIntPtr, WM_LBUTTONUP, IntPtr.Zero, lParam);
            });
        }

        private void btnToggle_Click(object sender, RoutedEventArgs e)
        {
            ToggleAutoClick();
        }

        private void ToggleAutoClick()
        {
            if (!isStart)
            {
                isStart = true;
                btnToggle.Dispatcher.Invoke(() => btnToggle.Content = "停止");
                autoClick.Change(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1000 / attackSpeed));
            }
            else
            {
                isStart = false;
                btnToggle.Dispatcher.Invoke(() => btnToggle.Content = "開始");
                autoClick.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        private void sliderAttackSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {

                if (labAttackSpeed == null)
                    return;

                attackSpeed = Math.Max(Math.Round(e.NewValue, 2), 0.01);

                labAttackSpeed.Dispatcher.Invoke(() => labAttackSpeed.Content = attackSpeed);
                Properties.Settings.Default.ClickSpeed = attackSpeed;
                Properties.Settings.Default.Save();

                if (isStart)
                    autoClick.Change(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1000 / attackSpeed));
            }
            catch (Exception ex)
            {
                Debugger.Log(3, "ValueChange", ex.ToString());
            }
        }
    }
}
