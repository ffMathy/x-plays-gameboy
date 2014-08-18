using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace XPlaysGameboy.Icons
{
    /// <summary>
    /// Interaction logic for ButtonStart.xaml
    /// </summary>
    public partial class ButtonStart : ButtonBase
    {
        public ButtonStart()
        {
            InitializeComponent();
        }

        public override async Task Push()
        {
            GameboyEngine.Instance.TapStart();
            await base.Push();
        }
    }
}
