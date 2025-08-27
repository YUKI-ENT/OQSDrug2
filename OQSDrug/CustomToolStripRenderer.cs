using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OQSDrug
{
    public class CustomToolStripRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            ToolStripButton btn = e.Item as ToolStripButton;
            if (btn != null)
            {
                Color backColor = btn.Checked ? Color.LightSkyBlue : SystemColors.Control; // Checked 時の背景色
                using (SolidBrush brush = new SolidBrush(backColor))
                {
                    e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, btn.Size));
                }
            }
            // 立体感のある枠線を描画
            if (btn.Checked)
            {
                // 立体的な枠線（右・下を明るくする）
                ControlPaint.DrawBorder(e.Graphics, new Rectangle(Point.Empty, btn.Size),
                    Color.Black, 2, ButtonBorderStyle.Solid,    // 上
                    Color.Black, 2, ButtonBorderStyle.Solid,    // 左
                    SystemColors.ControlLight, 2, ButtonBorderStyle.Solid, // 下（薄い色）
                    SystemColors.ControlLight, 2, ButtonBorderStyle.Solid  // 右（薄い色）
                );
            }
            else
            {
                // 通常時の枠線（必要なら追加）
                ControlPaint.DrawBorder(e.Graphics, new Rectangle(Point.Empty, btn.Size),
                    Color.White, 1, ButtonBorderStyle.Solid,    // 上
                    Color.White, 1, ButtonBorderStyle.Solid,    // 左
                    SystemColors.ControlDark, 1, ButtonBorderStyle.Solid, // 下（薄い色）
                    SystemColors.ControlDark, 1, ButtonBorderStyle.Solid  // 右（薄い色）
                );
            }
            base.OnRenderButtonBackground(e);
        }
    }
}
