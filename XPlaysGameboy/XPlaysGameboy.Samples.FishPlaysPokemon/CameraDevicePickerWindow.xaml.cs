using System.Collections.Generic;
using System.Windows;
using AForge.Video.DirectShow;
using System.Linq;

namespace XPlaysGameboy.Samples.FishPlaysPokemon
{
    /// <summary>
    /// Interaction logic for CameraDevicePickerWindow.xaml
    /// </summary>
    public partial class CameraDevicePickerWindow : Window
    {
        public FilterInfo ChosenCamera { get; set; }

        public CameraDevicePickerWindow()
        {
            InitializeComponent();

            var videoInformation = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            var informationList = new List<FilterInfo>();

            foreach (FilterInfo information in videoInformation)
            {
                informationList.Add(information);
            }

            ChosenCamera = informationList.FirstOrDefault();
            Filters.ItemsSource = informationList;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
