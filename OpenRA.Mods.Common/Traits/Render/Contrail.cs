#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Draw a colored contrail behind this actor when they move.")]
	public class ContrailInfo : ConditionalTraitInfo, Requires<BodyOrientationInfo>
	{
		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Offset for Z sorting.")]
		public readonly int ZOffset = 0;

		[Desc("Length of the trail (in ticks).")]
		public readonly int TrailLength = 25;

		[Desc("Width of the trail.")]
		public readonly WDist TrailWidth = new WDist(64);

		[Desc("RGB color of the contrail.")]
		public readonly Color Color = Color.White;

		[Desc("Use player remap color instead of a custom color?")]
		public readonly bool UsePlayerColor = true;

		public override object Create(ActorInitializer init) { return new Contrail(init.Self, this); }
	}

	public class Contrail : ConditionalTrait<ContrailInfo>, ITick, IRender, INotifyAddedToWorld
	{
		readonly ContrailInfo info;
		readonly BodyOrientation body;
		readonly Color color;

		// This is a mutable struct, so it can't be readonly.
		ContrailRenderable trail;

		public Contrail(Actor self, ContrailInfo info)
			: base(info)
		{
			this.info = info;

			color = info.UsePlayerColor ? ContrailRenderable.ChooseColor(self) : info.Color;
			trail = new ContrailRenderable(self.World, color, info.TrailWidth, info.TrailLength, 0, info.ZOffset);

			body = self.Trait<BodyOrientation>();
		}

		void ITick.Tick(Actor self)
		{
			// We want to update the trails' position even while the trait is disabled,
			// otherwise we might get visual 'jumps' when the trait is re-enabled.
			var local = info.Offset.Rotate(body.QuantizeOrientation(self.Orientation));
			trail.Update(self.CenterPosition + body.LocalToWorld(local));
		}

		IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer wr)
		{
			if (IsTraitDisabled)
				return Enumerable.Empty<IRenderable>();

			return new IRenderable[] { trail };
		}

		IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr)
		{
			// Contrails don't contribute to actor bounds
			yield break;
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			trail = new ContrailRenderable(self.World, color, info.TrailWidth, info.TrailLength, 0, info.ZOffset);
		}
	}
}
