#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Threading;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ColorMixerWidget : Widget
	{
		public float STrim = 0.025f;
		public float VTrim = 0.025f;

		public event Action OnChange = () => { };

		public float H { get; private set; }
		public float S { get; private set; }
		public float V { get; private set; }

		byte[] front, back;
		Sprite mixerSprite;
		bool isMoving;

		bool update;
		readonly object syncWorker = new object();
		readonly object bufferSync = new object();
		Thread workerThread;
		bool workerAlive;

		float[] sRange = { 0.0f, 1.0f };
		float[] vRange = { 0.0f, 1.0f };

		public ColorMixerWidget() { }
		public ColorMixerWidget(ColorMixerWidget other)
			: base(other)
		{
			OnChange = other.OnChange;
			H = other.H;
			S = other.S;
			V = other.V;

			sRange = (float[])other.sRange.Clone();
			vRange = (float[])other.vRange.Clone();

			STrim = other.STrim;
			VTrim = other.VTrim;
		}

		public void SetPaletteRange(float sMin, float sMax, float vMin, float vMax)
		{
			sRange[0] = sMin + STrim;
			sRange[1] = sMax - STrim;
			vRange[0] = vMin + VTrim;
			vRange[1] = vMax - VTrim;

			var rect = new Rectangle((int)(255 * sRange[0]), (int)(255 * (1 - vRange[1])), (int)(255 * (sRange[1] - sRange[0])) + 1, (int)(255 * (vRange[1] - vRange[0])) + 1);
			mixerSprite = new Sprite(mixerSprite.Sheet, rect, TextureChannel.Alpha);
		}

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			sRange[0] += STrim;
			sRange[1] -= STrim;
			vRange[0] += VTrim;
			vRange[1] -= VTrim;

			// Bitmap data is generated in a background thread and then flipped
			front = new byte[4 * 256 * 256];
			back = new byte[4 * 256 * 256];

			var rect = new Rectangle((int)(255 * sRange[0]), (int)(255 * (1 - vRange[1])), (int)(255 * (sRange[1] - sRange[0])) + 1, (int)(255 * (vRange[1] - vRange[0])) + 1);
			var mixerSheet = new Sheet(SheetType.BGRA, new Size(256, 256));
			mixerSheet.GetTexture().SetData(front, 256, 256);
			mixerSprite = new Sprite(mixerSheet, rect, TextureChannel.Alpha);
			GenerateBitmap();
		}

		void GenerateBitmap()
		{
			// Generating the selection bitmap is slow,
			// so we do it in a background thread
			lock (syncWorker)
			{
				update = true;

				if (workerThread == null || !workerAlive)
				{
					workerThread = new Thread(GenerateBitmapWorker);
					workerThread.Start();
				}
			}
		}

		void GenerateBitmapWorker()
		{
			lock (syncWorker)
				workerAlive = true;

			for (;;)
			{
				float hue;
				lock (syncWorker)
				{
					if (!update)
					{
						workerAlive = false;
						break;
					}

					update = false;

					// Take a local copy of the hue to generate to avoid tearing
					hue = H;
				}

				unsafe
				{
					// Generate palette in HSV
					fixed (byte* cc = &back[0])
					{
						var c = (int*)cc;
						for (var v = 0; v < 256; v++)
							for (var s = 0; s < 256; s++)
								*(c + (v * 256) + s) = HSLColor.FromHSV(hue, s / 255f, (255 - v) / 255f).RGB.ToArgb();
					}
				}

				lock (bufferSync)
				{
					var swap = front;
					front = back;
					back = swap;
				}
			}
		}

		public override void Draw()
		{
			if (Monitor.TryEnter(bufferSync))
			{
				try
				{
					mixerSprite.Sheet.GetTexture().SetData(front, 256, 256);
				}
				finally
				{
					Monitor.Exit(bufferSync);
				}
			}

			Game.Renderer.RgbaSpriteRenderer.DrawSprite(mixerSprite, RenderOrigin, new float2(RenderBounds.Size));

			var sprite = ChromeProvider.GetImage("lobby-bits", "colorpicker");
			var pos = RenderOrigin + PxFromValue() - new int2(sprite.Bounds.Width, sprite.Bounds.Height) / 2;
			WidgetUtils.FillEllipseWithColor(new Rectangle(pos.X + 1, pos.Y + 1, sprite.Bounds.Width - 2, sprite.Bounds.Height - 2), Color.RGB);
			Game.Renderer.RgbaSpriteRenderer.DrawSprite(sprite, pos);
		}

		void SetValueFromPx(int2 xy)
		{
			var rb = RenderBounds;
			var s = sRange[0] + xy.X * (sRange[1] - sRange[0]) / rb.Width;
			var v = sRange[1] - xy.Y * (vRange[1] - vRange[0]) / rb.Height;
			S = s.Clamp(sRange[0], sRange[1]);
			V = v.Clamp(vRange[0], vRange[1]);
		}

		int2 PxFromValue()
		{
			var rb = RenderBounds;
			var x = RenderBounds.Width * (S - sRange[0]) / (sRange[1] - sRange[0]);
			var y = RenderBounds.Height * (1 - (V - vRange[0]) / (vRange[1] - vRange[0]));
			return new int2((int)x.Clamp(0, rb.Width), (int)y.Clamp(0, rb.Height));
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Button != MouseButton.Left)
				return false;
			if (mi.Event == MouseInputEvent.Down && !TakeMouseFocus(mi))
				return false;
			if (!HasMouseFocus)
				return false;

			switch (mi.Event)
			{
				case MouseInputEvent.Up:
					isMoving = false;
					YieldMouseFocus(mi);
					break;

				case MouseInputEvent.Down:
					isMoving = true;
					SetValueFromPx(mi.Location - RenderOrigin);
					OnChange();
					break;

				case MouseInputEvent.Move:
					if (isMoving)
					{
						SetValueFromPx(mi.Location - RenderOrigin);
						OnChange();
					}

					break;
			}

			return true;
		}

		public HSLColor Color { get { return HSLColor.FromHSV(H, S, V); } }

		public void Set(float hue)
		{
			if (H != hue)
			{
				H = hue;
				GenerateBitmap();
				OnChange();
			}
		}

		public void Set(HSLColor color)
		{
			float h, s, v;
			color.ToHSV(out h, out s, out v);

			if (H != h || S != s || V != v)
			{
				if (H != h)
				{
					H = h;
					GenerateBitmap();
				}

				S = s.Clamp(sRange[0], sRange[1]);
				V = v.Clamp(vRange[0], vRange[1]);
				OnChange();
			}
		}
	}
}
