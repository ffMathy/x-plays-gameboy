using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using PInvoke;
using XPlaysGameboy.Properties;

using DrawingPoint = System.Drawing.Point;

namespace XPlaysGameboy
{
    public class GameboyEngine
    {
        private static GameboyEngine _engine;

        public static GameboyEngine Instance
        {
            get { return _engine ?? (_engine = new GameboyEngine()); }
        }

        private IntPtr _gameboyWindowHandle;

        private GameboyEngine()
        {

        }

        public async Task Start(string romLocation, FrameworkElement projectTo, double emulationSpeed = 1.0)
        {
            Window projectWindow = null;

            var parent = projectTo;
            while (parent.Parent != null)
            {
                parent = (FrameworkElement)parent.Parent;
                if (parent is Window)
                {
                    projectWindow = (Window)parent;
                }
            }

            if (projectWindow == null)
            {
                throw new ArgumentException("The projection element must be a child of a window.", "projectTo");
            }

            foreach (var existingProcess in Process.GetProcessesByName("bgb"))
            {
                using (existingProcess)
                {
                    existingProcess.Kill();
                    await Task.Delay(1000);
                }
            }

            await Task.Delay(1000);

            var emulatorRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "XPlaysGameboy", "Emulator");
            if (!Directory.Exists(emulatorRoot))
            {
                Directory.CreateDirectory(emulatorRoot);
            }

            var emulatorFilePath = Path.Combine(emulatorRoot, "bgb.exe");
            if (!File.Exists(emulatorFilePath))
            {
                File.WriteAllBytes(emulatorFilePath, Resources.bgbexe);
                File.WriteAllText(Path.Combine(emulatorRoot, "bgb.ini"), Resources.bgbini);
            }

            var information = new ProcessStartInfo(emulatorFilePath);
            information.Arguments = "\"" + romLocation + "\" " +
                                    "-setting Speed=" + emulationSpeed.ToString(new CultureInfo("en-US")) + " ";

            var process = Process.Start(information);

            Debug.Assert(process != null, "process != null");
            while (process.MainWindowHandle == IntPtr.Zero)
            {
                await Task.Delay(1000);
            }

            await Task.Delay(1000);

            var gameboyWindowHandle = NativeMethods.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Tfgb", null);
            _gameboyWindowHandle = gameboyWindowHandle;

            NativeMethods.ShowWindow(gameboyWindowHandle, NativeMethods.WindowShowStyle.Hide);

            var style = (long)NativeMethods.GetWindowLong(process.MainWindowHandle, NativeMethods.WindowLongIndexFlags.GWL_STYLE);
            style &= ~((uint)NativeMethods.SetWindowLongFlags.WS_CAPTION | (uint)NativeMethods.SetWindowLongFlags.WS_THICKFRAME | (uint)NativeMethods.SetWindowLongFlags.WS_MINIMIZE | (uint)NativeMethods.SetWindowLongFlags.WS_MAXIMIZE | (uint)NativeMethods.SetWindowLongFlags.WS_SYSMENU | (uint)NativeMethods.SetWindowLongFlags.WS_EX_APPWINDOW | (uint)NativeMethods.SetWindowLongFlags.WS_EX_OVERLAPPEDWINDOW | (uint)NativeMethods.SetWindowLongFlags.WS_OVERLAPPED | (uint)NativeMethods.SetWindowLongFlags.WS_ICONIC | (uint)NativeMethods.SetWindowLongFlags.WS_BORDER | (uint)NativeMethods.SetWindowLongFlags.WS_DLGFRAME | (uint)NativeMethods.SetWindowLongFlags.WS_EX_CLIENTEDGE | (uint)NativeMethods.SetWindowLongFlags.WS_EX_COMPOSITED | (uint)NativeMethods.SetWindowLongFlags.WS_EX_DLGMODALFRAME | (uint)NativeMethods.SetWindowLongFlags.WS_MAXIMIZE | (uint)NativeMethods.SetWindowLongFlags.WS_MAXIMIZEBOX | (uint)NativeMethods.SetWindowLongFlags.WS_MINIMIZE | (uint)NativeMethods.SetWindowLongFlags.WS_MINIMIZEBOX | (uint)NativeMethods.SetWindowLongFlags.WS_POPUP | (uint)NativeMethods.SetWindowLongFlags.WS_SIZEBOX | (uint)NativeMethods.SetWindowLongFlags.WS_TILED);
            style |= (uint)NativeMethods.SetWindowLongFlags.WS_EX_TOPMOST;
            style |= (uint)NativeMethods.SetWindowLongFlags.WS_EX_TOOLWINDOW;
            NativeMethods.SetWindowLong(gameboyWindowHandle, NativeMethods.WindowLongIndexFlags.GWL_STYLE, (NativeMethods.SetWindowLongFlags)style);

            NativeMethods.ShowWindow(gameboyWindowHandle, NativeMethods.WindowShowStyle.Show);

            var resizeProjection = (Action)delegate()
            {
                var projectionLocation = projectTo.PointToScreen(new Point(0, 0));
                var projectionSize = new Size(projectTo.ActualWidth, projectTo.ActualHeight);

                NativeMethods.SetWindowPos(gameboyWindowHandle, new IntPtr(-1), (int)projectionLocation.X, (int)projectionLocation.Y,
                    (int)projectionSize.Width, (int)projectionSize.Height, NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE);
            };

            projectTo.SizeChanged += delegate
            {
                resizeProjection();
            };
            projectWindow.LocationChanged += delegate
            {
                resizeProjection();
            };
            projectWindow.SizeChanged += delegate
            {
                resizeProjection();
            };
            projectWindow.Closed += delegate
            {
                process.Kill();
            };
            projectWindow.StateChanged += delegate
            {
                if (projectWindow.WindowState == WindowState.Minimized)
                {
                    NativeMethods.ShowWindow(_gameboyWindowHandle, NativeMethods.WindowShowStyle.Hide);
                }
                else
                {
                    NativeMethods.ShowWindow(_gameboyWindowHandle, NativeMethods.WindowShowStyle.ShowNoActivate);
                }
            };
            projectWindow.Activated += delegate
            {
                NativeMethods.ShowWindow(_gameboyWindowHandle, NativeMethods.WindowShowStyle.ShowNoActivate);
            };
            projectWindow.Deactivated += delegate
            {
                NativeMethods.ShowWindow(_gameboyWindowHandle, NativeMethods.WindowShowStyle.Hide);
            };

            resizeProjection();

        }

        private void SendKey(int keyCode, int delayMultiplier = 1)
        {
            NativeMethods.SendMessage(_gameboyWindowHandle, 0x100, new IntPtr(keyCode), IntPtr.Zero);
            Thread.Sleep(1 * delayMultiplier);
            NativeMethods.SendMessage(_gameboyWindowHandle, 0x101, new IntPtr(keyCode), IntPtr.Zero);

        }

        public void TapRight()
        {
            SendKey(0x27);
        }

        public void TapLeft()
        {
            SendKey(0x25);
        }

        public void TapUp()
        {
            SendKey(0x26);
        }

        public void TapDown()
        {
            SendKey(0x28);
        }

        public void TapA()
        {
            SendKey(0x53);
        }

        public void TapB()
        {
            SendKey(0x41);
        }

        public void TapSelect()
        {
            SendKey(0x10);
        }

        public void TapStart()
        {
            SendKey(0xD);
        }

        public void SaveState()
        {
            SendKey(0x71, 10);
        }

        public void LoadState()
        {
            SendKey(0x73, 10);
        }

        public void ToggleSpeedMode()
        {
            SendKey(0x6B, 10);
        }

    }
}
