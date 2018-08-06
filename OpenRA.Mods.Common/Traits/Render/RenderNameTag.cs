#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
	[Desc("Displays the player's name above the actor.")]
	class RenderNameTagInfo : ConditionalTraitInfo, Requires<IDecorationBoundsInfo>
	{
		[Desc("Maximum length of name tag shown.")]
		public readonly int MaxLength = 10;

		[Desc("Font used for name tag.")]
		public readonly string Font = "TinyBold";

		public override object Create(ActorInitializer init) { return new RenderNameTag(init.Self, this); }
	}

	class RenderNameTag : ConditionalTrait<RenderNameTagInfo>, INotifyCapture, IRender
	{
		readonly RenderNameTagInfo info;
		readonly SpriteFont font;
		readonly IDecorationBounds[] decorationBounds;

		string nameTag;
		Color color;

		public RenderNameTag(Actor self, RenderNameTagInfo info)
			: base(info)
		{
			this.info = info;
			font = Game.Renderer.Fonts[info.Font];
			decorationBounds = self.TraitsImplementing<IDecorationBounds>().ToArray();

			UpdateNameTag(self.Owner);
		}

		void UpdateNameTag(Player owner)
		{
			nameTag = owner.PlayerName.Length > info.MaxLength ? owner.PlayerName.Substring(0, info.MaxLength) : owner.PlayerName;
			color = owner.Color.RGB;
		}

		void INotifyCapture.OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			UpdateNameTag(newOwner);
		}

		public IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			if (IsTraitDisabled)
				return SpriteRenderable.None;

			var bounds = decorationBounds.FirstNonEmptyBounds(self, wr);
			var spaceBuffer = (int)(10 / wr.Viewport.Zoom);
			var effectPos = wr.ProjectedPosition(new int2((bounds.Left + bounds.Right) / 2, bounds.Y - spaceBuffer));

			return new IRenderable[] { new TextRenderable(font, effectPos, 4096, color, nameTag) };
		}

		IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr)
		{
			// Name tags don't contribute to actor bounds
			yield break;
		}
	}
}