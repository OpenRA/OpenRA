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

		[Desc("Offset for contrail's Z sorting.")]
		public readonly int ZOffset = 0;

		[Desc("Length of the contrail (in ticks).")]
		public readonly int TrailLength = 25;

		[Desc("Width of the contrail.")]
		public readonly WDist TrailWidth = new WDist(64);

		[Desc("Delay of the contrail.")]
		public readonly int TrailDelay = 0;

		[Desc("Contrail will fade with contrail width. Set 1.0 to make contrail fades just by length. Can be set with negative value")]
		public readonly float WidthFadeRate = 0;

		[Desc("RGB color when the contrail starts.")]
		public readonly Color StartColor = Color.White;

		[Desc("RGB color when the contrail ends.")]
		public readonly Color EndColor = Color.White;

		[Desc("Use player remap color instead of a custom color when the contrail starts.")]
		public readonly bool StartColorUsePlayerColor = true;

		[Desc("The alpha value [from 0 to 255] of color when the contrail starts.")]
		public readonly int StartColorAlpha = 255;

		[Desc("Use player remap color instead of a custom color when the contrail ends.")]
		public readonly bool EndColorUsePlayerColor = false;

		[Desc("The alpha value [from 0 to 255] of color when the contrail ends.")]
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

			startcolor = info.StartColorUsePlayerColor ? Color.FromArgb(info.StartColorAlpha, self.Owner.Color) : Color.FromArgb(info.StartColorAlpha, info.StartColor);
			endcolor = info.EndColorUsePlayerColor ? Color.FromArgb(info.EndColorAlpha, self.Owner.Color) : Color.FromArgb(info.EndColorAlpha, info.EndColor);

			trail = new ContrailRenderable(self.World, startcolor, endcolor, info.TrailWidth, info.TrailLength, info.TrailDelay, info.ZOffset, info.WidthFadeRate);

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
			trail = new ContrailRenderable(self.World, startcolor, endcolor, info.TrailWidth, info.TrailLength, info.TrailDelay, info.ZOffset, info.WidthFadeRate);
		}
	}
}
