using System;
using System.Diagnostics;
using System.IO;
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

        public GameboyEngine()
        {

        }

        public async void Start(string romLocation, FrameworkElement projectTo)
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

            var style = (long)NativeMethods.GetWindowLong(process.MainWindowHandle, NativeMethods.WindowLongFlags.GWL_STYLE);
            style &= ~((uint)NativeMethods.SetWindowLongFlags.WS_CAPTION | (uint)NativeMethods.SetWindowLongFlags.WS_THICKFRAME | (uint)NativeMethods.SetWindowLongFlags.WS_MINIMIZE | (uint)NativeMethods.SetWindowLongFlags.WS_MAXIMIZE | (uint)NativeMethods.SetWindowLongFlags.WS_SYSMENU);
            style |= (uint)NativeMethods.SetWindowLongFlags.WS_EX_TOPMOST;
            NativeMethods.SetWindowLong(gameboyWindowHandle, NativeMethods.WindowLongFlags.GWL_STYLE, (NativeMethods.SetWindowLongFlags)style);

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

        public async void TapA()
        {
            await Task.Run(delegate()
            {
                SendKeys.SendWait("B");
            });
        }

        public async void TapB()
        {
            await Task.Run(delegate()
            {
                SendKeys.SendWait("A");
            });
        }

        public async void TapSelect()
        {
            await Task.Run(delegate()
            {
                SendKeys.SendWait("+");
            });
        }

        public async void TapStart()
        {
            await Task.Run(delegate()
            {
                SendKeys.SendWait("{ENTER}");
            });
        }

        public async void SaveState()
        {
            await Task.Run(delegate()
            {
                SendKeys.SendWait("{F2}");
            });
        }

        public async void LoadState()
        {
            await Task.Run(delegate()
            {
                SendKeys.SendWait("{F4}");
            });
        }

    }
}
