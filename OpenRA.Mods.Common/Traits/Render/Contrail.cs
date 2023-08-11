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

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ZOffset = 0;

		[Desc("When set, display a line behind the actor. Length is measured in ticks after appearing.")]
		public readonly int TrailLength = 25;

		[Desc("Time (in ticks) after which the line should appear. Controls the distance to the actor.")]
		public readonly int TrailDelay = 0;

		[Desc("Thickness of the emitted line at the start of the contrail.")]
		public readonly WDist StartWidth = new(64);

		[Desc("Thickness of the emitted line at the end of the contrail. Will default to " + nameof(StartWidth) + " if left undefined")]
		public readonly WDist? EndWidth = null;

		[Desc("RGB color at the contrail start.")]
		public readonly Color StartColor = Color.White;

		[Desc("Use player remap color instead of a custom color at the contrail the start.")]
		public readonly bool StartColorUsePlayerColor = true;

		[Desc("The alpha value [from 0 to 255] of color at the contrail the start.")]
		public readonly int StartColorAlpha = 255;

		[Desc("RGB color at the contrail end. Will default to " + nameof(StartColor) + " if left undefined")]
		public readonly Color? EndColor;

		[Desc("Use player remap color instead of a custom color at the contrail end.")]
		public readonly bool EndColorUsePlayerColor = false;

		[Desc("The alpha value [from 0 to 255] of color at the contrail end.")]
		public readonly int EndColorAlpha = 0;

		public override object Create(ActorInitializer init) { return new Contrail(init.Self, this); }
	}

	public class Contrail : ConditionalTrait<ContrailInfo>, ITick, IRender, INotifyAddedToWorld
	{
		readonly ContrailInfo info;
		readonly BodyOrientation body;
		readonly Color startcolor;
		readonly Color endcolor;

		// This is a mutable struct, so it can't be readonly.
		ContrailRenderable trail;

		public Contrail(Actor self, ContrailInfo info)
			: base(info)
		{
			this.info = info;

			startcolor = Color.FromArgb(info.StartColorAlpha, info.StartColor);
			endcolor = Color.FromArgb(info.EndColorAlpha, info.EndColor ?? startcolor);
			trail = new ContrailRenderable(self.World, self,
				startcolor, info.StartColorUsePlayerColor,
				endcolor, info.EndColor == null ? info.StartColorUsePlayerColor : info.EndColorUsePlayerColor,
				info.StartWidth,
				info.EndWidth ?? info.StartWidth,
				info.TrailLength, info.TrailDelay, info.ZOffset);

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
			trail = new ContrailRenderable(self.World, self,
				startcolor, info.StartColorUsePlayerColor,
				endcolor, info.EndColor == null ? info.StartColorUsePlayerColor : info.EndColorUsePlayerColor,
				info.StartWidth,
				info.EndWidth ?? info.StartWidth,
				info.TrailLength, info.TrailDelay, info.ZOffset);
		}
	}
}
