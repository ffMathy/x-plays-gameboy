using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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

        public async Task Start(string romLocation, FrameworkElement projectTo)
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
            Directory.CreateDirectory(emulatorRoot);

            var emulatorFilePath = Path.Combine(emulatorRoot, "bgb.exe");
            File.WriteAllBytes(emulatorFilePath, Resources.bgbexe);
            File.WriteAllText(Path.Combine(emulatorRoot, "bgb.ini"), Resources.bgbini);

            var information = new ProcessStartInfo(emulatorFilePath);
            information.Arguments = "\"" + romLocation + "\"";

            var process = Process.Start(information);

            Debug.Assert(process != null, "process != null");
            while (process.MainWindowHandle == IntPtr.Zero)
            {
                await Task.Delay(1000);
            }

            await Task.Delay(1000);

            var gameboyWindowHandle = NativeMethods.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Tfgb", null);
            _gameboyWindowHandle = gameboyWindowHandle;

            var style = (long)NativeMethods.GetWindowLong(process.MainWindowHandle, NativeMethods.WindowLongIndexFlags.GWL_STYLE);
            style &= ~((uint)NativeMethods.SetWindowLongFlags.WS_CAPTION | (uint)NativeMethods.SetWindowLongFlags.WS_THICKFRAME | (uint)NativeMethods.SetWindowLongFlags.WS_MINIMIZE | (uint)NativeMethods.SetWindowLongFlags.WS_MAXIMIZE | (uint)NativeMethods.SetWindowLongFlags.WS_SYSMENU);
            style |= (uint)NativeMethods.SetWindowLongFlags.WS_EX_TOPMOST;
            NativeMethods.SetWindowLong(gameboyWindowHandle, NativeMethods.WindowLongIndexFlags.GWL_STYLE, (NativeMethods.SetWindowLongFlags)style);

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

            resizeProjection();

        }

        private async Task SendKey(int keyCode)
        {
            NativeMethods.SendMessage(_gameboyWindowHandle, 0x100, new IntPtr(keyCode), IntPtr.Zero);
            await Task.Delay(50);
            NativeMethods.SendMessage(_gameboyWindowHandle, 0x101, new IntPtr(keyCode), IntPtr.Zero);

            await Task.Delay(100);
        }

        public async void TapA()
        {
            await SendKey(0x53);
        }

        public async void TapB()
        {
            await SendKey(0x41);
        }

        public async void TapSelect()
        {
            await SendKey(0x10);
        }

        public async void TapStart()
        {
            await SendKey(0xD);
        }

        public async void SaveState()
        {
            await SendKey(0x71);
        }

        public async void LoadState()
        {
            await SendKey(0x72);
        }

    }
}
