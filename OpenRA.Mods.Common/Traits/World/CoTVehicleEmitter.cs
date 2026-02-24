#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Emits Cursor-on-Target (CoT) messages for vehicle lifecycle events (spawn, damage, killed) and periodic heartbeats while alive.")]
	public sealed class CoTVehicleEmitterInfo : CoTEmitterInfoBase
	{
		public CoTVehicleEmitterInfo()
		{
			Callsign = "OpenRA-Vehicle";
		}

		public override object Create(ActorInitializer init) { return new CoTVehicleEmitter(init, this); }
	}

	public sealed class CoTVehicleEmitter : CoTEmitterBase<CoTVehicleEmitterInfo>
	{
		protected override CoTDomain Domain => CoTDomain.GroundMobile;

		public CoTVehicleEmitter(ActorInitializer init, CoTVehicleEmitterInfo info)
			: base(init, info) { }
	}
}
