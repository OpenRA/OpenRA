#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Mods.RA.Orders;

namespace OpenRA.Mods.RA.Render
{
	class RenderHarvesterInfo : RenderUnitInfo, Requires<HarvesterInfo>
	{
		public readonly string[] ImagesByFullness = { "harvempty", "harvhalf", "harv" };
		public override object Create(ActorInitializer init) { return new RenderHarvester(init.self, this); }
	}

	class RenderHarvester : RenderUnit
	{
		Harvester harv;
		RenderHarvesterInfo info;

		public RenderHarvester(Actor self, RenderHarvesterInfo info) : base(self)
		{
			this.info = info;

			harv = self.Trait<Harvester>();
			foreach( var image in info.ImagesByFullness )
				new Animation( image );	/* just force these to get loaded upfront */
		}

		public override void Tick(Actor self)
		{
			var desiredState = harv.Fullness * (info.ImagesByFullness.Length - 1) / 100;
			var desiredImage = info.ImagesByFullness[desiredState];

			if (anim.Name != desiredImage)
				anim.ChangeImage( desiredImage, "idle" );

			base.Tick(self);
		}
	}
}