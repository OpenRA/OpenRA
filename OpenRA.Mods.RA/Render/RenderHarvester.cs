#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	class RenderHarvesterInfo : RenderUnitInfo, Requires<HarvesterInfo>
	{
		public readonly string[] ImagesByFullness = {"harv"};
		public override object Create(ActorInitializer init) { return new RenderHarvester(init.self, this); }
	}

	class RenderHarvester : RenderUnit, INotifyHarvesterAction
	{
		Harvester harv;
		RenderHarvesterInfo info;

		public RenderHarvester(Actor self, RenderHarvesterInfo info)
			: base(self)
		{
			this.info = info;
			harv = self.Trait<Harvester>();

			// HACK: Force images to be loaded up-front
			foreach (var image in info.ImagesByFullness)
				new Animation(self.World, image);
		}

		public override void Tick(Actor self)
		{
			var desiredState = harv.Fullness * (info.ImagesByFullness.Length - 1) / 100;
			var desiredImage = info.ImagesByFullness[desiredState];

			if (DefaultAnimation.Name != desiredImage)
				DefaultAnimation.ChangeImage(desiredImage, "idle");

			base.Tick(self);
		}

		public void Harvested(Actor self, ResourceType resource)
		{
			if (DefaultAnimation.CurrentSequence.Name != "harvest")
				PlayCustomAnim(self, "harvest");
		}

		public void MovingToResources(Actor self, CPos targetCell, Activity next) { }
		public void MovingToRefinery(Actor self, CPos targetCell, Activity next) { }
		public void MovementCancelled(Actor self) { }
	}
}