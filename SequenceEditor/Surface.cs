using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using IjwFramework.Types;

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

		Point mousePos;
		Point clickPos;
		bool isDragging;

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			mousePos = e.Location;
			Invalidate();
		}

		Pair<string, int>? FindFrameAt(Point p)
		{
			if (items.Count > 0)
			{
				foreach (var shp in items)
				{
					var sel = shp.Value.FirstOrDefault(a => a.Value.Contains(mousePos));
					if (!sel.Value.IsEmpty)
						return Pair.New(shp.Key, sel.Key);
				}
			}

			return null;
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				var frameAtPoint = FindFrameAt(e.Location);
				if (frameAtPoint == null) return;
				var seq = Program.Sequences
					.Where(kv => kv.Value.shp == frameAtPoint.Value.First &&
						frameAtPoint.Value.Second.IsInRange( kv.Value.start, kv.Value.length )).ToArray();

				foreach (var s in seq)
					Program.Sequences.Remove(s.Key);

				Invalidate();
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			var x = 0; 
			var y = 0;

			Point? toolPoint = null;
			string toolText = "";

			var frameAtPoint = FindFrameAt(mousePos);
			if (frameAtPoint != null)
			{
				var rect = items[frameAtPoint.Value.First][frameAtPoint.Value.Second];
				e.Graphics.FillRectangle(Brushes.Silver, rect);
				toolPoint = new Point(rect.Left, rect.Bottom);
				toolText = frameAtPoint.Value.Second.ToString();
			}

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

			if (toolPoint.HasValue)
			{
				var size = e.Graphics.MeasureString(toolText, Font);
				e.Graphics.FillRectangle(Brushes.Silver, toolPoint.Value.X, toolPoint.Value.Y, size.Width, size.Height);
				e.Graphics.DrawString(toolText, Font, Brushes.Black, toolPoint.Value.X, toolPoint.Value.Y);
			}
		}
	}

	static class Exts
	{
		public static bool IsInRange(this int x, int start, int len)
		{
			return x >= start && x < start + len;
		}
	}
}
