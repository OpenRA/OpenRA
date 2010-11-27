#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;
using OpenRA.Graphics;

namespace OpenRA.Mods.Cnc
{
	class WithFireInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new WithFire(init.self); }
	}

	class WithFire
	{
		public WithFire(Actor self)
		{
			var rs = self.Trait<RenderSimple>();
			var roof = new Animation(rs.GetImage(self));
			roof.PlayThen("fire-start", () => roof.PlayRepeating("fire-loop"));
			rs.anims.Add( "fire", new RenderSimple.AnimationWithOffset( roof, () => new float2(7,-15), null ) { ZOffset = 24 } );
		}
	}
}
