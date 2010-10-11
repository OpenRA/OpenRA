#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
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
		
		public Func<string> GetImage;
		public Func<int> GetFrame;
		public Func<string> GetPalette;
		
		public ShpImageWidget()
			: base()
		{
			GetImage = () => { return Image; };
			GetFrame = () => { return Frame; };
			GetPalette = () => { return Palette; };
		}

		protected ShpImageWidget(ShpImageWidget other)
			: base(other)
		{
			Image = other.Image;
			Frame = other.Frame;
			Palette = other.Palette;
			GetImage = other.GetImage;
			GetFrame = other.GetFrame;
			GetPalette = other.GetPalette;
		}

		public override Widget Clone() { return new ShpImageWidget(this); }

		
		Sprite sprite = null;
		string cachedImage = null;
		int cachedFrame= -1;
		public override void DrawInner(World world)
		{
			var image = GetImage();
			var frame = GetFrame();
			var palette = GetPalette();
		
			if (image != cachedImage || frame != cachedFrame)
			{
				sprite = SpriteSheetBuilder.LoadAllSprites(image)[frame];
				cachedImage = image;
				cachedFrame = frame;
			}
			
			Game.Renderer.WorldSpriteRenderer.DrawSprite(sprite,RenderOrigin, Game.world.WorldRenderer, palette);
		}
	}
}
