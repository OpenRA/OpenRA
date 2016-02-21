#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Lets the actor spread resources around it in a circle.")]
	class SeedsResourceInfo : UpgradableTraitInfo
	{
		public readonly int Interval = 75;
		public readonly string ResourceType = "Ore";
		public readonly int MaxRange = 100;

		public override object Create(ActorInitializer init) { return new SeedsResource(init.Self, this); }
	}

	class SeedsResource : UpgradableTrait<SeedsResourceInfo>, ITick, ISeedableResource
	{
		readonly SeedsResourceInfo info;

		readonly ResourceType resourceType;
		readonly ResourceLayer resLayer;

		public SeedsResource(Actor self, SeedsResourceInfo info)
			: base(info)
		{
			this.info = info;

			resourceType = self.World.WorldActor.TraitsImplementing<ResourceType>()
				.FirstOrDefault(t => t.Info.Name == info.ResourceType);

			if (resourceType == null)
				throw new InvalidOperationException("No such resource type `{0}`".F(info.ResourceType));

			resLayer = self.World.WorldActor.Trait<ResourceLayer>();
		}

		int ticks;

		public void Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (--ticks <= 0)
			{
				Seed(self);
				ticks = info.Interval;
			}
		}

		public void Seed(Actor self)
		{
			var cell = Util.RandomWalk(self.Location, self.World.SharedRandom)
				.Take(info.MaxRange)
				.SkipWhile(p => !self.World.Map.Contains(p) ||
					(resLayer.GetResource(p) == resourceType && resLayer.IsFull(p)))
				.Cast<CPos?>().FirstOrDefault();

			if (cell != null && resLayer.CanSpawnResourceAt(resourceType, cell.Value))
				resLayer.AddResource(resourceType, cell.Value, 1);
		}
	}
}
