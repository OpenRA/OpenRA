#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class SeedsResourceInfo : TraitInfo<SeedsResource>
	{
		public readonly int Interval = 75;
		public readonly string ResourceType = "Ore";
		public readonly int MaxRange = 100;
		public readonly int AnimationInterval = 750;
	}

	class SeedsResource : ITick
	{
		int ticks;
		int animationTicks;

		public void Tick(Actor self)
		{
			if (--ticks <= 0)
			{
				var info = self.Info.Traits.Get<SeedsResourceInfo>();
				var resourceType = self.World.WorldActor.traits
					.WithInterface<ResourceType>()
					.FirstOrDefault(t => t.info.Name == info.ResourceType);

				if (resourceType == null)
					throw new InvalidOperationException("No such resource type `{0}`".F(info.ResourceType));

				var resLayer = self.World.WorldActor.traits.Get<ResourceLayer>();

				var cell = RandomWalk(self.Location, self.World.SharedRandom)
					.Take(info.MaxRange)
					.SkipWhile(p => resLayer.GetResource(p) == resourceType && resLayer.IsFull(p.X, p.Y))
					.Cast<int2?>().FirstOrDefault();

				if (cell != null &&
					(resLayer.GetResource(cell.Value) == resourceType || resLayer.GetResource(cell.Value) == null) &&
					self.World.IsCellBuildable(cell.Value, false))
					resLayer.AddResource(resourceType, cell.Value.X, cell.Value.Y, 1);

				ticks = info.Interval;
			}

			if (--animationTicks <= 0)
			{
				var info = self.Info.Traits.Get<SeedsResourceInfo>();
				self.traits.Get<RenderBuilding>().PlayCustomAnim(self, "active");
				animationTicks = info.AnimationInterval;
			}
		}

		static IEnumerable<int2> RandomWalk(int2 p, Thirdparty.Random r)
		{
			for (; ; )
			{
				var dx = r.Next(-1, 2);
				var dy = r.Next(-1, 2);

				if (dx == 0 && dy == 0)
					continue;

				p.X += dx;
				p.Y += dy;
				yield return p;
			}
		}
	}
}
