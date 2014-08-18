using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;
using FormsApplication = System.Windows.Forms.Application;
using FileResources = XPlaysGameboy.Samples.SimulatorPlaysPokemon.Properties.Resources;

namespace XPlaysGameboy.Samples.SimulatorPlaysPokemon
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly GameboyEngine _engine;

        public MainWindow()
        {
            InitializeComponent();

            _engine = GameboyEngine.Instance;

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //write the pokemon red ROM to the disk.
            var romPath = Path.Combine(Environment.CurrentDirectory, "PokemonRed.gb");
            File.WriteAllBytes(romPath, FileResources.PokemonRed);

            //start the emulator with 5X normal speed.
            await _engine.Start(romPath, GameboyArea, 10);

            //now load the game.
            _engine.TapStart();
            _engine.LoadState();

            //now that the game is running, start simulating random keypresses.
            StartKeyPressLoop();
        }

        private async void StartKeyPressLoop()
        {
            var random = new Random((int)DateTime.UtcNow.Ticks);
            var frame = 0;

            var ticks = DateTime.UtcNow.Ticks;

            var commandList = new LinkedList<string>();
            while (true)
            {
                FormsApplication.DoEvents();
                Thread.Sleep(Math.Max(100 - (int)(DateTime.UtcNow.Ticks - ticks), 1));

                ticks = DateTime.UtcNow.Ticks;

                frame++;

                //every 100th frame (10 seconds), save progress.
                if (frame == 1000)
                {
                    _engine.SaveState();
                    frame = 0;
                }

                string commandName;
                switch (Math.Min(random.Next(0, 6), 5))
                {
                    case 0:
                        commandName = "Right";
                        _engine.TapRight();
                        break;
                            
                    case 1:
                        commandName = "Left";
                        _engine.TapLeft();
                        break;

                    case 2:
                        commandName = "Down";
                        _engine.TapDown();
                        break;

                    case 3:
                        commandName = "Up";
                        _engine.TapUp();
                        break;

                    case 4:
                        commandName = "A";
                        _engine.TapA();
                        break;

                    case 5:
                        commandName = "B";
                        _engine.TapB();
                        break;

                    default:
                        throw new InvalidOperationException();
                }

                commandList.AddFirst(commandName);
                if (commandList.Count > 100)
                {
                    commandList.RemoveLast();
                }

                Log.ItemsSource = null;
                Log.ItemsSource = commandList;
            }
        }
    }
}
