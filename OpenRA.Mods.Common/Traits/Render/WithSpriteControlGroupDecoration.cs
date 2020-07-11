#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Renders Ctrl groups using pixel art.")]
	public class WithSpriteControlGroupDecorationInfo : TraitInfo
	{
		[PaletteReference]
		public readonly string Palette = "chrome";

		public readonly string Image = "pips";

		[SequenceReference("Image")]
		[Desc("Sprite sequence used to render the control group 0-9 numbers.")]
		public readonly string GroupSequence = "groups";

		[Desc("Position in the actor's selection box to draw the decoration.")]
		public readonly DecorationPosition Position = DecorationPosition.TopLeft;

		[Desc("Offset sprite center position from the selection box edge.")]
		public readonly int2 Margin = int2.Zero;

		public override object Create(ActorInitializer init) { return new WithSpriteControlGroupDecoration(init.Self, this); }
	}

	public class WithSpriteControlGroupDecoration : IDecoration
	{
		public readonly WithSpriteControlGroupDecorationInfo Info;
		readonly Actor self;
		readonly Animation anim;

		public WithSpriteControlGroupDecoration(Actor self, WithSpriteControlGroupDecorationInfo info)
		{
			Info = info;
			this.self = self;

			anim = new Animation(self.World, Info.Image);
		}

		DecorationPosition IDecoration.Position { get { return Info.Position; } }

		bool IDecoration.Enabled { get { return self.Owner == self.World.LocalPlayer && self.World.Selection.GetControlGroupForActor(self) != null; } }

		bool IDecoration.RequiresSelection { get { return true; } }

		IEnumerable<IRenderable> IDecoration.RenderDecoration(Actor self, WorldRenderer wr, int2 pos)
		{
			var group = self.World.Selection.GetControlGroupForActor(self);
			if (group == null)
				return Enumerable.Empty<IRenderable>();

			anim.PlayFetchIndex(Info.GroupSequence, () => (int)group);

			var screenPos = wr.Viewport.WorldToViewPx(pos) + Info.Position.CreateMargin(Info.Margin) - (0.5f * anim.Image.Size.XY).ToInt2();
			var palette = wr.Palette(Info.Palette);
			return new IRenderable[]
			{
				new UISpriteRenderable(anim.Image, self.CenterPosition, screenPos, 0, palette, 1f)
			};
		}
	}
}
