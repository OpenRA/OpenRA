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

			var brushes = new[] { Brushes.Green, Brushes.Red, Brushes.Blue, Brushes.Magenta, Brushes.DarkOrange, Brushes.Navy };

			var seqid = 0;
			foreach (var seq in Program.Sequences)
			{
				var firstFrame = seq.Value.start;
				var r = items[seq.Value.shp][firstFrame];

				for (var i = 0; i < seq.Value.length; i++)
				{
					var q = items[seq.Value.shp][i + firstFrame];
					e.Graphics.FillRectangle(brushes[seqid], q.Left, q.Top, q.Width, 2);
				}

				var z = e.Graphics.MeasureString(seq.Key, Font);
				e.Graphics.FillRectangle(brushes[seqid], r.Left, r.Top, z.Width, z.Height);
				e.Graphics.DrawString(seq.Key, Font, Brushes.White, r.Left, r.Top);

				seqid = ++seqid % brushes.Length;
			}
		}
	}
}
