using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace SequenceEditor
{
	public class Surface : Control
	{
		public Surface()
		{
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.ResizeRedraw, true);
			UpdateStyles();
		}

		Dictionary<string, Dictionary<int, Rectangle>> items 
			= new Dictionary<string, Dictionary<int, Rectangle>>();

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			var x = 0; 
			var y = 0;

			items.Clear();

			foreach (var shp in Program.Shps)
			{
				x = 0;
				e.Graphics.DrawString(shp.Key + ".shp", Font, Brushes.Black, x, y);
				y += Font.Height;
				var u = 0;
				var i = 0;

				var dict = items[shp.Key] = new Dictionary<int, Rectangle>();

				foreach (var frame in shp.Value)
				{
					if (x + frame.Width >= ClientSize.Width)
					{
						x = 0;
						y += u;
						u = 0;
					}

					dict[i++] = new Rectangle(x, y, frame.Width, frame.Height);

					e.Graphics.DrawImage(frame, x, y);
					x += frame.Width;
					u = Math.Max(u, frame.Height);
				}

				y += u;
				x = 0;
			}
		}
	}
}
