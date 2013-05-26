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

namespace OpenRA.Mods.Cnc
{
	class WithFireInfo : ITraitInfo, Requires<RenderSimpleInfo>
	{
		public readonly WVec Offset = new WVec(299,-640,0);

		public object Create(ActorInitializer init) { return new WithFire(init.self, this); }
	}

	class WithFire
	{
		public WithFire(Actor self, WithFireInfo info)
		{
			var rs = self.Trait<RenderSimple>();
			var roof = new Animation(rs.GetImage(self));
			roof.PlayThen("fire-start", () => roof.PlayRepeating("fire-loop"));

			rs.anims.Add("fire", new AnimationWithOffset(roof, () => info.Offset, null, 1024));
		}
	}
}
