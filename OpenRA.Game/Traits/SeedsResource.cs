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
using System.Linq;

namespace OpenRA.Traits
{
	class SeedsResourceInfo : ITraitInfo
	{
		public readonly float Chance = .05f;
		public readonly int Interval = 5;
		public readonly string ResourceType = "Ore";

		public object Create(Actor self) { return new SeedsResource(); }
	}

	class SeedsResource : ITick
	{
		int ticks;

		public void Tick(Actor self)
		{
			if (--ticks <= 0)
			{
				var info = self.Info.Traits.Get<SeedsResourceInfo>();
				var resourceType = self.World.WorldActor.Info.Traits
					.WithInterface<ResourceTypeInfo>()
					.FirstOrDefault(t => t.Name == info.ResourceType);

				if (resourceType == null)
					throw new InvalidOperationException("No such resource type `{0}`".F(info.ResourceType));

				var resLayer = self.World.WorldActor.traits.Get<ResourceLayer>();

				for (var j = -1; j < 2; j++)
					for (var i = -1; i < 2; i++)
						if (self.World.SharedRandom.NextDouble() < info.Chance)
							if (self.World.IsCellBuildable(self.Location + new int2(i, j), false))
								resLayer.AddResource(resourceType, self.Location.X + i, self.Location.Y + j, 1);

				ticks = info.Interval;
			}
		}
	}
}
