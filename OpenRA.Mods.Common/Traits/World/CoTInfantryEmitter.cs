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
	[Desc("Emits Cursor-on-Target (CoT) messages for infantry lifecycle events (spawn, damage, killed) and periodic heartbeats while alive.")]
	public sealed class CoTInfantryEmitterInfo : CoTEmitterInfoBase
	{
		public CoTInfantryEmitterInfo()
		{
			Callsign = "OpenRA-Infantry";
		}

		public override object Create(ActorInitializer init) { return new CoTInfantryEmitter(init, this); }
	}

	public sealed class CoTInfantryEmitter : CoTEmitterBase<CoTInfantryEmitterInfo>
	{
		protected override CoTDomain Domain => CoTDomain.GroundMobile;

		public CoTInfantryEmitter(ActorInitializer init, CoTInfantryEmitterInfo info)
			: base(init, info) { }
	}
}
