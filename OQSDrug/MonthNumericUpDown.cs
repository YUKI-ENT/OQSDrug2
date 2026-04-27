using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace OQSDrug
{
    public class MonthNumericUpDown : NumericUpDown
    {
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public NumericUpDown YearControl { get; set; }

        public override void UpButton()
        {
            if (Value >= Maximum)
            {
                if (TryIncrementYear(1))
                {
                    Value = Minimum;
                }
                return;
            }

            base.UpButton();
        }

        public override void DownButton()
        {
            if (Value <= Minimum)
            {
                if (TryIncrementYear(-1))
                {
                    Value = Maximum;
                }
                return;
            }

            base.DownButton();
        }

        private bool TryIncrementYear(int delta)
        {
            if (YearControl == null)
            {
                return false;
            }

            decimal next = YearControl.Value + delta;
            if (next < YearControl.Minimum || next > YearControl.Maximum)
            {
                return false;
            }

            YearControl.Value = next;
            return true;
        }
    }
}
