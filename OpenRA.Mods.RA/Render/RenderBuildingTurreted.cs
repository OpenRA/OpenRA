#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class RenderBuildingTurretedInfo : RenderBuildingInfo, Requires<TurretedInfo>
	{
		public override object Create(ActorInitializer init) { return new RenderBuildingTurreted( init, this ); }
	}

	class RenderBuildingTurreted : RenderBuilding
	{
		public RenderBuildingTurreted( ActorInitializer init, RenderBuildingInfo info )
			: base(init, info, MakeTurretFacingFunc(init.self)) { }

		static Func<int> MakeTurretFacingFunc(Actor self)
		{
			// Turret artwork is baked into the sprite, so only the first turret makes sense.
			var turreted = self.TraitsImplementing<Turreted>().FirstOrDefault();
			return () => turreted.turretFacing;
		}
	}
}
