using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using XPlaysGameboy.Properties;

namespace XPlaysGameboy
{
    class GameboyEngine
    {

        public GameboyEngine()
        {
            
        }

        public async void Start(string romLocation, FrameworkElement projectTo)
        {
            var emulatorRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "XPlaysGameboy", "Emulator");
            var emulatorFilePath = Path.Combine(emulatorRoot, "bgb.exe");
            File.WriteAllBytes(emulatorFilePath, Resources.bgbexe);
            File.WriteAllText(Path.Combine(emulatorRoot, "bgb.ini"), Resources.bgbini);

            var information = new ProcessStartInfo(emulatorFilePath);
            information.Arguments = "\"" + romLocation + "\"";

            using (var process = Process.Start(information))
            {

                Debug.Assert(process != null, "process != null");
                while (process.MainWindowHandle == default(IntPtr))
                {
                    await Task.Delay(100);
                }

                var gameboyHandle = process.MainWindowHandle;

                var projectionLocation = projectTo.PointToScreen(new Point(0, 0));
                var projectionSize = new Size(projectTo.ActualWidth, projectTo.ActualHeight);

                var resizeProjection = (Action) delegate()
                {

                };

                projectTo.SizeChanged += delegate(object sender, SizeChangedEventArgs args)
                {
                    projectionLocation = projectTo.PointToScreen(new Point(0, 0));
                    projectionSize = new Size(projectTo.ActualWidth, projectTo.ActualHeight);
                };

                resizeProjection();

            }
        }

    }
}
