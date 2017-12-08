#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Displays the player name above the unit")]
	class RenderNameTagInfo : ITraitInfo, Requires<IDecorationBoundsInfo>
	{
		public readonly int MaxLength = 10;

		public readonly string Font = "TinyBold";

		public object Create(ActorInitializer init) { return new RenderNameTag(init.Self, this); }
	}

	class RenderNameTag : IRender
	{
		readonly SpriteFont font;
		readonly Color color;
		readonly string name;
		readonly IDecorationBounds[] decorationBounds;

		public RenderNameTag(Actor self, RenderNameTagInfo info)
		{
			font = Game.Renderer.Fonts[info.Font];
			color = self.Owner.Color.RGB;

			if (self.Owner.PlayerName.Length > info.MaxLength)
				name = self.Owner.PlayerName.Substring(0, info.MaxLength);
			else
				name = self.Owner.PlayerName;

			decorationBounds = self.TraitsImplementing<IDecorationBounds>().ToArray();
		}

		public IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			var bounds = decorationBounds.Select(b => b.DecorationBounds(self, wr)).FirstOrDefault(b => !b.IsEmpty);
			var spaceBuffer = (int)(10 / wr.Viewport.Zoom);
			var effectPos = wr.ProjectedPosition(new int2((bounds.Left + bounds.Right) / 2, bounds.Y - spaceBuffer));

			return new IRenderable[] { new TextRenderable(font, effectPos, 0, color, name) };
		}

		IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr)
		{
			// Name tags don't contribute to actor bounds
			yield break;
		}
	}
}
