using System;
using System.IO;
using System.Windows;
using FileResources = XPlaysGameboy.Samples.FishPlaysPokemon.Properties.Resources;

namespace XPlaysGameboy.Samples.FishPlaysPokemon
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

            _engine = new GameboyEngine();

            Loaded += MainWindow_Loaded;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //write the pokemon red ROM to the disk.
            var romPath = Path.Combine(Environment.CurrentDirectory, "PokemonRed.gb");
            File.WriteAllBytes(romPath, FileResources.PokemonRed);

            _engine.Start(romPath, RenderGrid);
        }
    }
}
