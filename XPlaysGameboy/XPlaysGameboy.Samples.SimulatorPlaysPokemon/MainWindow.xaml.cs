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
        private readonly GameboyEngine _gameboy;
        private readonly TwitchChatEngine _twitchChatEngine;

        private int slowmotionCountdown;

        public MainWindow()
        {
            InitializeComponent();

            slowmotionCountdown = 300;

            _gameboy = GameboyEngine.Instance;
            _twitchChatEngine = TwitchChatEngine.Instance;

            var dataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "XPlaysPokemon");
            var twitchApiKeyPath = Path.Combine(dataRoot, "Twitch.oauth");
            var twitchUsernamePath = Path.Combine(dataRoot, "Twitch.username");
            if (File.Exists(twitchApiKeyPath) && File.Exists(twitchUsernamePath))
            {
                var twitchApiKey = File.ReadAllText(twitchApiKeyPath);
                var twitchUsername = File.ReadAllText(twitchUsernamePath);
                _twitchChatEngine.Start(twitchUsername, twitchApiKey);
            }

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //write the pokemon red ROM to the disk.
            var romPath = Path.Combine(Environment.CurrentDirectory, "PokemonRed.gb");
            if (!File.Exists(romPath))
            {
                File.WriteAllBytes(romPath, FileResources.PokemonRed);
            }

            //start the emulator with 5X normal speed.
            await _gameboy.Start(romPath, GameboyArea, 10);

            await Task.Delay(5000);

            //now load the game.
            _gameboy.TapStart();

            await Task.Delay(5000);

            _gameboy.LoadState();

            await Task.Delay(5000);

            //now that the game is running, start simulating random keypresses.
            StartKeyPressLoop();
            StartSlowMotionLoop();
        }

        private async void StartSlowMotionLoop()
        {
            while (true)
            {
                await Task.Delay(1000);

                slowmotionCountdown -= 1;

                if (slowmotionCountdown < -30)
                {
                    slowmotionCountdown = 300;
                }
            }
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

                var delay = 100;
                if (slowmotionCountdown < 0)
                {
                    //TODO: slowmotion.
                }

                Thread.Sleep(Math.Max(delay - (int)(DateTime.UtcNow.Ticks - ticks), 1));

                ticks = DateTime.UtcNow.Ticks;

                frame++;

                //every 100th frame (10 seconds), save progress.
                if (frame == 1000)
                {
                    _gameboy.SaveState();
                    frame = 0;
                }

                string commandName;
                switch (Math.Min(random.Next(0, 6), 5))
                {
                    case 0:
                        commandName = "Right";
                        _gameboy.TapRight();
                        break;
                            
                    case 1:
                        commandName = "Left";
                        _gameboy.TapLeft();
                        break;

                    case 2:
                        commandName = "Down";
                        _gameboy.TapDown();
                        break;

                    case 3:
                        commandName = "Up";
                        _gameboy.TapUp();
                        break;

                    case 4:
                        commandName = "A";
                        _gameboy.TapA();
                        break;

                    case 5:
                        commandName = "B";
                        _gameboy.TapB();
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
