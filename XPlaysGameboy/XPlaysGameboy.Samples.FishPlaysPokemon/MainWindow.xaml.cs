using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using AForge.Video.DirectShow;
using AForge.Vision.Motion;
using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using XPlaysGameboy.Icons;
using FileResources = XPlaysGameboy.Samples.FishPlaysPokemon.Properties.Resources;
using Point = System.Windows.Point;

namespace XPlaysGameboy.Samples.FishPlaysPokemon
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly GameboyEngine _engine;
        private readonly MotionDetector _detector;
        private readonly BlobCountingObjectsProcessing _processor;

        private readonly ButtonBase[,] _fields;

        private int currentColumn;
        private int currentRow;

        //moving object size in pixels.
        private const int MinimumMovingObjectSize = 10;

        public MainWindow()
        {
            InitializeComponent();

            _engine = GameboyEngine.Instance;

            _processor = new BlobCountingObjectsProcessing(MinimumMovingObjectSize, MinimumMovingObjectSize, Color.Black);
            _detector = new MotionDetector(new TwoFramesDifferenceDetector(), _processor);

            _fields = new ButtonBase[MotionGrid.ColumnDefinitions.Count, MotionGrid.RowDefinitions.Count];

            RandomizeFields();

            Loaded += MainWindow_Loaded;
        }

        private async void StartRandomizeLoop()
        {
            var offset = 0;
            while (true)
            {
                await Task.Delay(100);

                offset ++;
                if (offset > 10)
                {
                    offset = 0;
                    _fields[currentColumn, currentRow].Push();
                }

                //now fill up the progress bar a little bit.
                var delta = Math.Min(1, ProgressBar.Maximum - ProgressBar.Value);
                ProgressBar.Value += delta;
                if (ProgressBar.Value >= ProgressBar.Maximum)
                {
                    ProgressBar.Value = 0;
                    RandomizeFields();
                }
            }
        }

        void RandomizeFields()
        {
            for (var x = 0; x < _fields.GetLength(0); x++)
            {
                for (var y = 0; y < _fields.GetLength(1); y++)
                {
                    var field = _fields[x, y];
                    if (field != null)
                    {
                        _fields[x, y] = null;
                        MotionGrid.Children.Remove(field);
                    }
                }
            }

            var buttonTypes = new[]
            {
                typeof (ButtonA), typeof (ButtonB), typeof (ButtonSelect), typeof (ButtonDown), typeof (ButtonUp),
                typeof (ButtonRight), typeof (ButtonLeft), typeof (ButtonStart)
            };
            var buttonTypeCounts = new int[buttonTypes.Length];

            var random = new Random();
            for (var x = 0; x < _fields.GetLength(0); x++)
            {
                for (var y = 0; y < _fields.GetLength(1); y++)
                {
                    var lowestAmountIndexes = buttonTypeCounts.Select((c, i) => Array.IndexOf(buttonTypeCounts, buttonTypeCounts.Min(), i)).Where(c => c != -1).ToArray();
                    var offsetToGenerate = random.Next(0, lowestAmountIndexes.Length - 1);

                    var index = lowestAmountIndexes[offsetToGenerate];
                    _fields[x, y] = (ButtonBase)Activator.CreateInstance(buttonTypes[index]);

                    _fields[x, y].Opacity = 0.5;

                    buttonTypeCounts[index]++;

                    Grid.SetColumn(_fields[x, y], x);
                    Grid.SetRow(_fields[x, y], y);

                    MotionGrid.Children.Add(_fields[x, y]);
                }
            }
        }

        async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

            //launch camera stuff.
            var cameraPicker = new CameraDevicePickerWindow();
            cameraPicker.ShowDialog();

            var camera = cameraPicker.ChosenCamera;
            if (camera == null)
            {
                throw new InvalidOperationException("Can't run this sample application without a camera attached to your computer. Restart the program and try again.");
            }

            var captureDevice = new VideoCaptureDevice(camera.MonikerString);

            captureDevice.NewFrame += captureDevice_NewFrame;
            captureDevice.Start();

            //write the pokemon red ROM to the disk.
            var romPath = Path.Combine(Environment.CurrentDirectory, "PokemonRed.gb");
            File.WriteAllBytes(romPath, FileResources.PokemonRed);

            await _engine.Start(romPath, GameboyArea);

            //start randomize loop.
            StartRandomizeLoop();
        }

        void captureDevice_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {

            lock (this)
            {

                Dispatcher.Invoke(delegate()
                {

                    var image = eventArgs.Frame;
                    _detector.ProcessFrame(image);

                    if (_processor.ObjectsCount > 0)
                    {

                        var largestMovementArea = int.MinValue;
                        var largestMovementCenter = default(Point);

                        foreach (var zone in _processor.ObjectRectangles)
                        {
                            var movementArea = zone.Width * zone.Height;
                            if (movementArea > largestMovementArea)
                            {
                                largestMovementArea = movementArea;
                                largestMovementCenter = new Point(zone.X + zone.Width / 2, zone.Y + zone.Height / 2);
                            }
                        }

                        //now determine where the center movement is, and in which grid.
                        var imageWidth = (double)image.Width;
                        var imageHeight = (double)image.Height;

                        var gridRowCount = MotionGrid.RowDefinitions.Count;
                        var gridColumnCount = MotionGrid.ColumnDefinitions.Count;

                        var cellWidth = imageWidth / gridColumnCount;
                        var cellHeight = imageHeight / gridRowCount;

                        var targetColumn = (int)Math.Min(Math.Max((largestMovementCenter.X) / cellWidth, 0), gridColumnCount - 1);
                        var targetRow = (int)Math.Min(Math.Max((largestMovementCenter.Y) / cellHeight, 0), gridRowCount - 1);

                        var currentProgressBarRow = Grid.GetRow(ProgressBar);
                        var currentProgressBarColumn = Grid.GetColumn(ProgressBar);

                        //reposition the progress bar if the cell with activity in it has changed.
                        if (currentProgressBarColumn != targetColumn || currentProgressBarRow != targetRow)
                        {
                            Grid.SetColumn(ProgressBar, targetColumn);
                            Grid.SetRow(ProgressBar, targetRow);

                            this.currentColumn = targetColumn;
                            this.currentRow = targetRow;

                            ProgressBar.Value = 0;
                        }

                    }

                    var source = ConvertBitmap(image);
                    CameraFeed.Source = source;

                    image.Dispose();

                });

            }
        }

        private static BitmapSource ConvertBitmap(Bitmap source)
        {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                          source.GetHbitmap(),
                          IntPtr.Zero,
                          Int32Rect.Empty,
                          BitmapSizeOptions.FromEmptyOptions());
        }
    }
}
