using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace XPlaysGameboy.Icons
{
    public abstract class ButtonBase : UserControl
    {
        public event Action ButtonPushed;

        public virtual async Task Push()
        {
            if (ButtonPushed != null)
            {
                ButtonPushed();
            }
        }
    }
}
