using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OQSDrug
{
    public class ClickThroughToolStrip : ToolStrip
    {
        private const int WM_MOUSEACTIVATE = 0x21;
        private const int MA_ACTIVATE = 1;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEACTIVATE)
            {
                m.Result = (IntPtr)MA_ACTIVATE;
                return;
            }
            base.WndProc(ref m);
        }
    }
}
