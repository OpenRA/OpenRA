#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
{
	public class ColorMixerWidget : Widget
	{
		public float[] SRange = {0.2f, 1.0f};
		public float[] VRange = {0.2f, 1.0f};
		public event Action OnChange = () => {};

		float H, S, V;
		Bitmap frontBitmap, swapBitmap, backBitmap;
		Sprite mixerSprite;
		bool isMoving;

		bool updateFront, updateBack;
		object syncWorker = new object();
		Thread workerThread;
		bool workerAlive;

		public ColorMixerWidget() : base() {}
		public ColorMixerWidget(ColorMixerWidget other)
			: base(other)
		{
			OnChange = other.OnChange;
			H = other.H;
			S = other.S;
			V = other.V;
		}

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			// Bitmap data is generated in a background thread and then flipped
			frontBitmap = new Bitmap(256, 256);
			swapBitmap = new Bitmap(256, 256);
			backBitmap = new Bitmap(256, 256);

			var rect = new Rectangle((int)(255*SRange[0]), (int)(255*(1 - VRange[1])), (int)(255*(SRange[1] - SRange[0]))+1, (int)(255*(VRange[1] - VRange[0])) + 1);
			mixerSprite = new Sprite(new Sheet(new Size(256, 256)), rect, TextureChannel.Alpha);
			mixerSprite.sheet.Texture.SetData(frontBitmap);
		}

		void GenerateBitmap()
		{
			// Generating the selection bitmap is slow,
			// so we do it in a background thread
			lock (syncWorker)
			{
				updateBack = true;

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
					if (!updateBack)
					{
						workerAlive = false;
						break;
					}
					updateBack = false;

					// Take a local copy of the hue to generate to avoid tearing
					hue = H;
				}

				var bitmapData = backBitmap.LockBits(backBitmap.Bounds(),
					ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

				unsafe
				{
					int* c = (int*)bitmapData.Scan0;

					// Generate palette in HSV
					for (var v = 0; v < 256; v++)
						for (var s = 0; s < 256; s++)
							*(c + (v * bitmapData.Stride >> 2) + s) = HSLColor.FromHSV(hue, s / 255f, (255 - v) / 255f).ToColor().ToArgb();
				}

				backBitmap.UnlockBits(bitmapData);
				lock (syncWorker)
				{
					var swap = swapBitmap;
					swapBitmap = backBitmap;
					backBitmap = swap;
					updateFront = true;
				}
			}
		}

		public override void Draw()
		{
			lock (syncWorker)
			{
				if (updateFront)
				{
					var swap = swapBitmap;
					swapBitmap = frontBitmap;
					frontBitmap = swap;

					mixerSprite.sheet.Texture.SetData(frontBitmap);
					updateFront = false;
				}
			}

			Game.Renderer.RgbaSpriteRenderer.DrawSprite(mixerSprite, RenderOrigin, new float2(RenderBounds.Size));

			var sprite = ChromeProvider.GetImage("lobby-bits", "colorpicker");
			var pos = RenderOrigin + PxFromValue() - new int2(sprite.bounds.Width/2, sprite.bounds.Height/2);
			WidgetUtils.FillRectWithColor(new Rectangle(pos.X + 3, pos.Y + 3, 10, 10), Color.ToColor());
			Game.Renderer.RgbaSpriteRenderer.DrawSprite(sprite, pos);
		}

		void SetValueFromPx(int2 xy)
		{
			var rb = RenderBounds;
			var s = SRange[0] + xy.X*(SRange[1] - SRange[0])/rb.Width;
			var v = SRange[1] - xy.Y*(VRange[1] - VRange[0])/rb.Height;
			S = s.Clamp(SRange[0], SRange[1]);
			V = v.Clamp(VRange[0], VRange[1]);
		}

		int2 PxFromValue()
		{
			var rb = RenderBounds;
			var x = RenderBounds.Width*(S - SRange[0])/(SRange[1] - SRange[0]);
			var y = RenderBounds.Height*(1 - (V - VRange[0])/(VRange[1] - VRange[0]));
			return new int2((int)x.Clamp(0, rb.Width), (int)y.Clamp(0, rb.Height));
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Button != MouseButton.Left)
				return false;
			if (mi.Event == MouseInputEvent.Down && !TakeFocus(mi))
				return false;
			if (!Focused)
				return false;

			switch (mi.Event)
			{
			case MouseInputEvent.Up:
				isMoving = false;
				LoseFocus(mi);
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
			float h,s,v;
			color.ToHSV(out h, out s, out v);

			if (H != h || S != s || V != v)
			{
				if (H != h)
				{
					H = h;
					GenerateBitmap();
				}

				S = s.Clamp(SRange[0], SRange[1]);
				V = v.Clamp(VRange[0], VRange[1]);
				OnChange();
			}
		}
	}
}
