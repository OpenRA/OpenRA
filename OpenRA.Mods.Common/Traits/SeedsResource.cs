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

using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Lets the actor spread resources around it in a circle.")]
	class SeedsResourceInfo : ConditionalTraitInfo
	{
		public readonly int Interval = 75;
		public readonly string ResourceType = "Ore";
		public readonly int MaxRange = 100;

		public override object Create(ActorInitializer init) { return new SeedsResource(init.Self, this); }
	}

	class SeedsResource : ConditionalTrait<SeedsResourceInfo>, ITick, ISeedableResource
	{
		readonly SeedsResourceInfo info;

		readonly ResourceType resourceType;
		readonly IResourceLayer resourceLayer;

		public SeedsResource(Actor self, SeedsResourceInfo info)
			: base(info)
		{
			this.info = info;

			resourceType = self.World.WorldActor.TraitsImplementing<ResourceType>()
				.FirstOrDefault(t => t.Info.Type == info.ResourceType);

			if (resourceType == null)
				throw new InvalidOperationException("No such resource type `{0}`".F(info.ResourceType));

			resourceLayer = self.World.WorldActor.Trait<IResourceLayer>();
		}

		int ticks;

		void ITick.Tick(Actor self)
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
				.SkipWhile(p => resourceLayer.GetResource(p).Type == resourceType && !resourceLayer.CanAddResource(resourceType, p))
				.Cast<CPos?>().FirstOrDefault();

			if (cell != null && resourceLayer.CanAddResource(resourceType, cell.Value))
				resourceLayer.AddResource(resourceType, cell.Value);
		}
	}
}
