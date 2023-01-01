#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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

		[SequenceReference(nameof(Image))]
		[Desc("Sprite sequence used to render the control group 0-9 numbers.")]
		public readonly string GroupSequence = "groups";

		[Desc("Position in the actor's selection box to draw the decoration.")]
		public readonly string Position = "TopLeft";

		[Desc("Offset sprite center position from the selection box edge.")]
		public readonly int2 Margin = int2.Zero;

		public override object Create(ActorInitializer init) { return new WithSpriteControlGroupDecoration(init.Self, this); }
	}

	public class WithSpriteControlGroupDecoration : IDecoration
	{
		public readonly WithSpriteControlGroupDecorationInfo Info;
		readonly Animation anim;

		public WithSpriteControlGroupDecoration(Actor self, WithSpriteControlGroupDecorationInfo info)
		{
			Info = info;
			anim = new Animation(self.World, Info.Image);
		}

		bool IDecoration.RequiresSelection => true;

		IEnumerable<IRenderable> IDecoration.RenderDecoration(Actor self, WorldRenderer wr, ISelectionDecorations container)
		{
			var group = self.World.ControlGroups.GetControlGroupForActor(self);
			if (group == null)
				return Enumerable.Empty<IRenderable>();

			anim.PlayFetchIndex(Info.GroupSequence, () => (int)group);

			var screenPos = container.GetDecorationOrigin(self, wr, Info.Position, Info.Margin) - (0.5f * anim.Image.Size.XY).ToInt2();
			var palette = wr.Palette(Info.Palette);
			return new IRenderable[]
			{
				new UISpriteRenderable(anim.Image, self.CenterPosition, screenPos, 0, palette)
			};
		}
	}
}
