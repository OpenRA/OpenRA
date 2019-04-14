#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits.Render
{
	public class WithHarvesterSpriteBodyInfo : WithFacingSpriteBodyInfo, Requires<HarvesterInfo>
	{
		[Desc("Images switched between depending on fullness of harvester. Overrides RenderSprites.Image.")]
		public readonly string[] ImageByFullness = { };

		public override object Create(ActorInitializer init) { return new WithHarvesterSpriteBody(init, this); }
	}

	public class WithHarvesterSpriteBody : WithFacingSpriteBody, ITick
	{
		readonly WithHarvesterSpriteBodyInfo info;
		readonly Harvester harv;

		public WithHarvesterSpriteBody(ActorInitializer init, WithHarvesterSpriteBodyInfo info)
			: base(init, info)
		{
			this.info = info;
			harv = init.Self.Trait<Harvester>();
		}

		void ITick.Tick(Actor self)
		{
			if (harv == null || info.ImageByFullness.Length == 0)
				return;

			var desiredState = harv.Fullness * (info.ImageByFullness.Length - 1) / 100;
			var desiredImage = info.ImageByFullness[desiredState];

			DefaultAnimation.ChangeImage(desiredImage, DefaultAnimation.CurrentSequence.Name);
		}
	}
}
