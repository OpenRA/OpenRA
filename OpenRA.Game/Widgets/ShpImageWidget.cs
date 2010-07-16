#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
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
			
			Game.chrome.renderer.WorldSpriteRenderer.DrawSprite(sprite,RenderOrigin, palette);
		}
	}
}
