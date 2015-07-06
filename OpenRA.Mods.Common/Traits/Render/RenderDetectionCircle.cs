#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class RenderDetectionCircleInfo : ITraitInfo, Requires<DetectCloakedInfo>
	{
		public object Create(ActorInitializer init) { return new RenderDetectionCircle(init.Self); }
	}

	class RenderDetectionCircle : IPostRenderSelection
	{
		Actor self;

		public RenderDetectionCircle(Actor self) { this.self = self; }

		public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr)
		{
			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				yield break;

			var range = self.TraitsImplementing<DetectCloaked>()
				.Where(a => !a.IsTraitDisabled)
				.Select(a => WDist.FromCells(a.Info.Range))
				.Append(WDist.Zero).Max();

			if (range == WDist.Zero)
				yield break;

			yield return new RangeCircleRenderable(
				self.CenterPosition,
				range,
				0,
				Color.FromArgb(128, Color.LimeGreen),
				Color.FromArgb(96, Color.Black));
		}
	}
}
