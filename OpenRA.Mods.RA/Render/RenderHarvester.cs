#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class RenderHarvesterInfo : RenderUnitInfo, Requires<HarvesterInfo>
	{
		public readonly string[] ImagesByFullness = {"harv"};
		public override object Create(ActorInitializer init) { return new RenderHarvester(init.self, this); }
	}

	class RenderHarvester : RenderUnit, INotifyHarvest
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

		public override void TickRender(WorldRenderer wr, Actor self)
		{
			if (wr.world.Paused == World.PauseState.Paused)
				return;

			var desiredState = harv.Fullness * (info.ImagesByFullness.Length - 1) / 100;
			var desiredImage = info.ImagesByFullness[desiredState];

			if (DefaultAnimation.Name != desiredImage)
				DefaultAnimation.ChangeImage(desiredImage, "idle");

			base.TickRender(wr, self);
		}

		public void Harvested(Actor self, ResourceType resource)
		{
			if (DefaultAnimation.CurrentSequence.Name != "harvest")
				PlayCustomAnim(self, "harvest");
		}
	}
}