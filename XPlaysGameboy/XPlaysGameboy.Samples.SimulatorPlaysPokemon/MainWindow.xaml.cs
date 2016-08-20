using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
using XPlaysGameboy.Samples.SimulatorPlaysPokemon.Models;
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
        private readonly string[] _channelOwners;

        private readonly GameboyEngine _gameboy;
        private readonly TwitchChatEngine _twitchChatEngine;

        private const int SpeedyTime = 600;
        private const int SlowTime = 60;
        private const int SpeedMultiplier = 50;

        private int _slowmotionCountdown;
        private int _lastCommandIndex;

        private DateTime _startTime;

        private readonly DateTime _launchTime;

        private RepeatRequest _repeatRequest;

        public MainWindow()
        {
            InitializeComponent();

            SizeToContent = SizeToContent.WidthAndHeight;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.None;

            Left = 0;
            Top = 0;

            _launchTime = DateTime.UtcNow;

            //assign two power users who can use special chat commands.
            _channelOwners = new[] { "ffMathy", "RandomnessPlaysPokemon" };

            _slowmotionCountdown = SpeedyTime;

            _gameboy = GameboyEngine.Instance;
            _twitchChatEngine = TwitchChatEngine.Instance;

            //initialize chat, but only if an API key and username is available. you'll need to generate these yourselves.
            var dataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "XPlaysGameboy");
            var twitchOAuthTokenPath = Path.Combine(dataRoot, "Twitch.oauth");
            var twitchUsernamePath = Path.Combine(dataRoot, "Twitch.username");
            if (File.Exists(twitchOAuthTokenPath) && File.Exists(twitchUsernamePath))
            {
                var twitchOAuthToken = File.ReadAllText(twitchOAuthTokenPath);
                var twitchUsername = File.ReadAllText(twitchUsernamePath);
                _twitchChatEngine.Start(twitchUsername, twitchOAuthToken);
            }

            _twitchChatEngine.MessageReceived += _twitchChatEngine_MessageReceived;
            _twitchChatEngine.UserJoined += _twitchChatEngine_UserJoined;

            Loaded += MainWindow_Loaded;
        }

        void _twitchChatEngine_UserJoined(string username)
        {
            if ((DateTime.UtcNow - _launchTime).TotalMinutes > 15)
            {
                _twitchChatEngine.SendMessage("Welcome to the stream, @" +
                                      username +
                                      "! Be sure to read the description for a full list of commands and more details.");
            }
        }

        void _twitchChatEngine_MessageReceived(string username, string message)
        {
            Dispatcher.Invoke(delegate()
            {
                var item = new ChatLogItem()
                {
                    Message = message,
                    Username = username
                };

                ChatLog.Items.Insert(0, item);
                if (ChatLog.Items.Count > 25)
                {
                    ChatLog.Items.RemoveAt(ChatLog.Items.Count - 1);
                }
            });

            var messageUpper = message.ToUpper();

            var isChannelOwner = _channelOwners.Any(p => string.Equals(p.Trim(), username.Trim(), StringComparison.OrdinalIgnoreCase));
            var split = messageUpper.Split(' ');
            switch (split[0])
            {
                case "REPEAT":
                    if (_repeatRequest == null && split.Length > 1)
                    {
                        int amount;
                        if (int.TryParse(split[1], out amount))
                        {
                            //amount can be between 2 and 15.
                            amount = Math.Min(Math.Max(amount, 2), 25);

                            _repeatRequest = new RepeatRequest()
                            {
                                Amount = amount,
                                RequestAuthor = username,
                                CommandIndex = _lastCommandIndex
                            };
                        }
                    }
                    break;

                //allows power-users to reposition the window.
                case "REPOSITION":
                    if (isChannelOwner && split.Length > 2)
                    {
                        int x;
                        int y;
                        if (int.TryParse(split[1], out x) && int.TryParse(split[2], out y))
                        {
                            Dispatcher.Invoke(delegate()
                            {
                                Left = x;
                                Top = y;
                            });
                        }
                    }
                    break;

                case "MODESWITCH":
                    if (_twitchChatEngine.IsOperator(username) && split.Length > 1)
                    {
                        var mode = split[1].ToUpper();
                        if (mode == "SLOW")
                        {
                            _gameboy.StopSpeedMode();
                            _slowmotionCountdown = 0;
                        }
                        else if (mode == "SPEED")
                        {
                            _gameboy.StartSpeedMode();
                            _slowmotionCountdown = SpeedyTime;
                        }
                    }
                    break;

                case "RESTART":
                    if (isChannelOwner)
                    {
                        using (var myProcess = Process.GetCurrentProcess())
                        {
                            Process.Start(myProcess.ProcessName + ".exe");
                        }
                    }
                    break;
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //write the pokemon red ROM to the disk.
            var romPath = Path.Combine(Environment.CurrentDirectory, "PokemonCrystal.gba");
            if (!File.Exists(romPath))
            {
                File.WriteAllBytes(romPath, FileResources.PokemonCrystal);
            }

            //start the emulator with 20X normal speed.
            await _gameboy.Start(romPath, GameboyArea, SpeedMultiplier);

            _startTime = File.GetLastWriteTime(Path.Combine(_gameboy.EmulatorDirectory, "bgb.exe"));

            await Task.Delay(5000);

            //enable turbo.
            _gameboy.StartSpeedMode();

            await Task.Delay(1000);

            //now load the game.
            _gameboy.TapStart();

            await Task.Delay(1000);

            _gameboy.LoadState();

            await Task.Delay(1000);

            //now that the game is running, start simulating random keypresses.
            StartKeyPressLoop();
            StartSlowMotionLoop();
            StartBackupLoop();
        }

        private async void StartBackupLoop()
        {
            var offset = 0L;
            while (true)
            {
                await Task.Delay(30000);
                _gameboy.SaveState();
                await Task.Delay(30000);

                offset++;
                if (offset % 60 == 0)
                {

                    var backupPath = Path.Combine(_gameboy.DataDirectory, "Backup " + DateTime.UtcNow.Ticks);
                    Directory.CreateDirectory(backupPath);

                    _gameboy.PerformBackup(backupPath);

                }
            }
        }

        private async void StartSlowMotionLoop()
        {
            while (true)
            {
                await Task.Delay(1000);

                _slowmotionCountdown -= 1;

                if (_slowmotionCountdown < -SlowTime)
                {
                    _slowmotionCountdown = SpeedyTime;
                }
            }
        }

        private async void StartKeyPressLoop()
        {

            var random = new Random((int)DateTime.UtcNow.Ticks);

            var lastTick = DateTime.UtcNow;
            var lastStart = DateTime.UtcNow;

            const int SmallDelay = 2;

            var commandList = new LinkedList<string>();
            while (true)
            {

                var delay = SmallDelay;
                if (_slowmotionCountdown < 0)
                {

                    //disable turbo.
                    _gameboy.StopSpeedMode();

                    delay = 1500;

                    SlowMotionCountdown.Text = "Mode: Slowdown (Speed in: " + (SlowTime + _slowmotionCountdown) + " seconds)";
                }
                else
                {

                    //enable turbo.
                    _gameboy.StartSpeedMode();

                    SlowMotionCountdown.Text = "Mode: Speed (Slowdown in: " + _slowmotionCountdown + " seconds)";
                }

                Action command;

                string commandName;
                _lastCommandIndex = _repeatRequest == null ? Math.Min(random.Next(0, 22), 21) : _repeatRequest.CommandIndex;
                switch (_lastCommandIndex)
                {
                    case 0:
                    case 7:
                    case 13:
                        commandName = "→";
                        command = _gameboy.TapRight;
                        break;

                    case 1:
                    case 8:
                    case 14:
                        commandName = "←";
                        command = _gameboy.TapLeft;
                        break;

                    case 2:
                    case 9:
                    case 15:
                        commandName = "↓";
                        command = _gameboy.TapDown;
                        break;

                    case 3:
                    case 10:
                    case 16:
                        commandName = "↑";
                        command = _gameboy.TapUp;
                        break;

                    case 4:
                    case 11:
                    case 17:
                        commandName = "A";
                        command = _gameboy.TapA;
                        break;

                    case 5:
                    case 12:
                    case 18:
                        commandName = "B";
                        command = _gameboy.TapB;
                        break;

                    case 6:
                        if ((DateTime.UtcNow - lastStart).TotalSeconds > 10)
                        {
                            lastStart = DateTime.UtcNow;

                            commandName = "ST";
                            command = delegate()
                            {
                                Thread.Sleep(10);
                                _gameboy.TapStart();
                                Thread.Sleep(100);
                                if (!_gameboy.IsInSpeedMode)
                                {
                                    Thread.Sleep(1000);
                                }
                                _gameboy.TapStart();
                                Thread.Sleep(10);
                            };
                        }
                        else
                        {
                            continue;
                        }
                        break;

                    case 19:
                    case 20:
                    case 21:
                        commandName = "SE";
                        command = _gameboy.TapSelect;
                        break;

                    default:
                        throw new InvalidOperationException();
                }

                var difference = (int)(DateTime.UtcNow - lastTick).TotalMilliseconds;
                await Task.Delay(Math.Max(delay - difference, 1));

                lastTick = DateTime.UtcNow;

                if (_repeatRequest != null)
                {
                    LastRepeat.Text = _repeatRequest.Amount + "X [" + commandName + "] by " + _repeatRequest.RequestAuthor;
                }

                commandList.AddFirst(commandName);
                if (commandList.Count > 50)
                {
                    commandList.RemoveLast();
                }

                Log.ItemsSource = null;
                Log.ItemsSource = commandList;

                var realTimeSpent = DateTime.Now.Subtract(_startTime);
                var pokemonTimeSpent = new TimeSpan(realTimeSpent.Ticks * SpeedMultiplier);

                RealTimeSpent.Text = realTimeSpent.Days + "d " + realTimeSpent.Hours.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + "h " + realTimeSpent.Minutes.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + "m " + realTimeSpent.Seconds.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + "s";
                PokemonTimeSpent.Text = pokemonTimeSpent.Days + "d " + pokemonTimeSpent.Hours.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + "h " + pokemonTimeSpent.Minutes.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + "m " + pokemonTimeSpent.Seconds.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + "s";

                var repeat = _repeatRequest == null ? 1 : _repeatRequest.Amount;
                for (var i = 0; i < repeat; i++)
                {
                    command();
                    if (i != 0)
                    {
                        await Task.Delay(10);
                        if (!_gameboy.IsInSpeedMode)
                        {
                            await Task.Delay(500);
                        }
                    }
                }

                _repeatRequest = null;

                FormsApplication.DoEvents();

            }
        }
    }
}
