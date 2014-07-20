#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	[Desc("Displays the player name above the unit")]
	class RenderNameTagInfo : ITraitInfo
	{
		public readonly int MaxLength = 10;

		public readonly string Font = "TinyBold";

		public object Create(ActorInitializer init) { return new RenderNameTag(init.self, this); }
	}

	class RenderNameTag : IRender
	{
		readonly SpriteFont font;
		readonly Color color;
		readonly string name;

		public RenderNameTag(Actor self, RenderNameTagInfo info)
		{
			font = Game.Renderer.Fonts[info.Font];
			color = self.Owner.Color.RGB;

			if (self.Owner.PlayerName.Length > info.MaxLength)
				name = self.Owner.PlayerName.Substring(0, info.MaxLength);
			else
				name = self.Owner.PlayerName;
		}

		public IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			var pos = wr.ScreenPxPosition(self.CenterPosition);
			var bounds = self.Bounds.Value;
			bounds.Offset(pos.X, pos.Y);
			var spaceBuffer = (int)(10 / wr.Viewport.Zoom);
			var effectPos = wr.Position(new int2(pos.X, bounds.Y - spaceBuffer));

			yield return new TextRenderable(font, effectPos, 0, color, name);
		}
	}
}
