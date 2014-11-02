#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc
{
	[Desc("Renders a flame sprite on top of the actor.")]
	class WithFireInfo : ITraitInfo, Requires<RenderSpritesInfo>
	{
		public readonly string StartSequence = "fire-start";
		public readonly string LoopSequence = "fire-loop";

		public object Create(ActorInitializer init) { return new WithFire(init.self, this); }
	}

	class WithFire
	{
		public WithFire(Actor self, WithFireInfo info)
		{
			var rs = self.Trait<RenderSprites>();
			var fire = new Animation(self.World, rs.GetImage(self));
			fire.PlayThen(info.StartSequence, () => fire.PlayRepeating(info.LoopSequence));
			rs.Add("fire", new AnimationWithOffset(fire, null, null, 1024));
		}
	}
}
