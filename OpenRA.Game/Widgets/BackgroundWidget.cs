#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Drawing.Imaging;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public class BackgroundWidget : Widget
	{
		public readonly string Background = "dialog";
		public readonly bool ClickThrough = false;
		public readonly bool Draggable = false;

		public bool MixColorFadeToBlack = false;
		public Color MixColorFadeToBlackColor = Color.Black;

		protected Bitmap mixColorFadeToBlackBitmap = null;
		protected Sprite mixColorFadeToBlackSprite = null;

		protected void InitFadeToBlackSprite()
		{
			mixColorFadeToBlackBitmap = new Bitmap(256, 256);
			mixColorFadeToBlackSprite = new Sprite(new Sheet(new Size(256, 256), true), new Rectangle(0, 0, 256, 1), TextureChannel.Alpha);

			var bitmapData = mixColorFadeToBlackBitmap.LockBits(mixColorFadeToBlackBitmap.Bounds(),
			ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
			unsafe
			{
				int* c = (int*)bitmapData.Scan0;
				for (var h = 0; h < 256; h++)
				{
					Color r = Color.FromArgb(
					255 - h,
					MixColorFadeToBlackColor);

					*(c + h) = r.ToArgb();
				}
			}

			mixColorFadeToBlackBitmap.UnlockBits(bitmapData);

			mixColorFadeToBlackSprite.sheet.Texture.SetData(mixColorFadeToBlackBitmap);
		}

		public override void Draw()
		{
			if (Background != "none")
			{
				WidgetUtils.DrawPanel(Background, RenderBounds);

				if (MixColorFadeToBlack)
				{
					if (mixColorFadeToBlackBitmap == null)
					{
						InitFadeToBlackSprite();
					}

					Game.Renderer.RgbaSpriteRenderer.DrawSprite(mixColorFadeToBlackSprite, new float2(RenderBounds.X + 1, RenderBounds.Y + 1), new float2(new Size(255, RenderBounds.Height - 2)));
				}
			}
		}

		public BackgroundWidget() { }

		public BackgroundWidget(string background)
		{
			this.Background = background;
		}

		bool moving;
		int2? prevMouseLocation;

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (ClickThrough || !RenderBounds.Contains(mi.Location))
				return false;

			if (!Draggable || moving && (!TakeMouseFocus(mi) || mi.Button != MouseButton.Left))
				return true;

			if (prevMouseLocation == null)
				prevMouseLocation = mi.Location;
			var vec = mi.Location - (int2)prevMouseLocation;
			prevMouseLocation = mi.Location;
			switch (mi.Event)
			{
				case MouseInputEvent.Up:
					moving = false;
					YieldMouseFocus(mi);
					break;
				case MouseInputEvent.Down:
					moving = true;
					Bounds = new Rectangle(Bounds.X + vec.X, Bounds.Y + vec.Y, Bounds.Width, Bounds.Height);
					break;
				case MouseInputEvent.Move:
					if (moving)
						Bounds = new Rectangle(Bounds.X + vec.X, Bounds.Y + vec.Y, Bounds.Width, Bounds.Height);
					break;
			}

			return true;
		}

		protected BackgroundWidget(BackgroundWidget other)
			: base(other)
		{
			Background = other.Background;
			ClickThrough = other.ClickThrough;
			Draggable = other.Draggable;

			MixColorFadeToBlack = other.MixColorFadeToBlack;
			MixColorFadeToBlackColor = other.MixColorFadeToBlackColor;
		}

		public override Widget Clone() { return new BackgroundWidget(this); }
	}
}