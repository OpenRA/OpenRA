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
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public class ShpImageWidget : Widget
	{
		public string Image = "";
		public int Frame = 0;
		public string Palette = "chrome";
		public bool LoopAnimation = false;

		public Func<string> GetImage;
		public Func<int> GetFrame;
		public Func<string> GetPalette;

		readonly WorldRenderer worldRenderer;

		[ObjectCreator.UseCtor]
		public ShpImageWidget(WorldRenderer worldRenderer)
		{
			GetImage = () => { return Image; };
			GetFrame = () => { return Frame; };
			GetPalette = () => { return Palette; };

			this.worldRenderer = worldRenderer;
		}

		protected ShpImageWidget(ShpImageWidget other)
			: base(other)
		{
			Image = other.Image;
			Frame = other.Frame;
			Palette = other.Palette;
			LoopAnimation = other.LoopAnimation;

			GetImage = other.GetImage;
			GetFrame = other.GetFrame;
			GetPalette = other.GetPalette;

			worldRenderer = other.worldRenderer;
		}

		public override Widget Clone() { return new ShpImageWidget(this); }

		Sprite sprite = null;
		string cachedImage = null;
		int cachedFrame = -1;
		float2 cachedOffset = float2.Zero;

		public override void Draw()
		{
			var image = GetImage();
			var frame = GetFrame();
			var palette = GetPalette();

			if (image != cachedImage || frame != cachedFrame)
			{
				sprite = Game.modData.SpriteLoader.LoadAllSprites(image)[frame];
				cachedImage = image;
				cachedFrame = frame;
				cachedOffset = 0.5f * (new float2(RenderBounds.Size) - sprite.size);
			}

			Game.Renderer.SpriteRenderer.DrawSprite(sprite, RenderOrigin + cachedOffset, worldRenderer.Palette(palette));
		}

		public int FrameCount
		{
			get { return Game.modData.SpriteLoader.LoadAllSprites(Image).Length-1; }
		}

		public void RenderNextFrame()
		{
			if (Frame < FrameCount)
				Frame++;
			else
				Frame = 0;
		}

		public void RenderPreviousFrame()
		{
			if (Frame > 0)
				Frame--;
			else
				Frame = FrameCount;
		}

		public override void Tick()
		{
			if (LoopAnimation)
				RenderNextFrame();
		}
	}
}
