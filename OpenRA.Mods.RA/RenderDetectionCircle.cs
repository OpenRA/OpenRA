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
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class RenderDetectionCircleInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new RenderDetectionCircle(init.self); }
	}

	class RenderDetectionCircle : IPostRenderSelection
	{
		Actor self;

		public RenderDetectionCircle(Actor self) { this.self = self; }

		public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr)
		{
			if (self.Owner != self.World.LocalPlayer)
				yield break;

			yield return new RangeCircleRenderable(
				self.CenterPosition,
				WRange.FromCells(self.Info.Traits.Get<DetectCloakedInfo>().Range),
				0,
				Color.FromArgb(128, Color.LimeGreen),
				Color.FromArgb(96, Color.Black)
			);
		}
	}
}
