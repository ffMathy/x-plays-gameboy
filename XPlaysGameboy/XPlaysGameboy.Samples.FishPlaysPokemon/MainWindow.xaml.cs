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
        public MainWindow()
        {
            InitializeComponent();

            //write the pokemon red ROM to the disk.
            var romPath = Path.Combine(Environment.CurrentDirectory, "PokemonRed.gb");
            File.WriteAllBytes(romPath, FileResources.PokemonRed);

            var engine = new GameboyEngine();
            engine.Start(romPath, RenderGrid);
        }
    }
}
