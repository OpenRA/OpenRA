#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class RenderHarvesterInfo : RenderUnitInfo, Requires<HarvesterInfo>
	{
		public readonly string[] ImagesByFullness = { "harv" };

		public readonly string HarvestSequence = "harvest";

		public override object Create(ActorInitializer init) { return new RenderHarvester(init, this); }
	}

	class RenderHarvester : RenderUnit, INotifyHarvesterAction
	{
		Harvester harv;
		RenderHarvesterInfo info;

		public RenderHarvester(ActorInitializer init, RenderHarvesterInfo info)
			: base(init, info)
		{
			this.info = info;
			harv = init.Self.Trait<Harvester>();

			// HACK: Force images to be loaded up-front
			foreach (var image in info.ImagesByFullness)
				new Animation(init.World, image);
		}

		public override void Tick(Actor self)
		{
			var desiredState = harv.Fullness * (info.ImagesByFullness.Length - 1) / 100;
			var desiredImage = info.ImagesByFullness[desiredState];

			if (DefaultAnimation.Name != desiredImage)
				DefaultAnimation.ChangeImage(desiredImage, info.Sequence);

			base.Tick(self);
		}

		public void Harvested(Actor self, ResourceType resource)
		{
			if (DefaultAnimation.CurrentSequence.Name != info.HarvestSequence)
				PlayCustomAnim(self, info.HarvestSequence);
		}

		public void MovingToResources(Actor self, CPos targetCell, Activity next) { }
		public void MovingToRefinery(Actor self, CPos targetCell, Activity next) { }
		public void MovementCancelled(Actor self) { }
	}
}
