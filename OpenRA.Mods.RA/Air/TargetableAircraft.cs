#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Mods.RA.Air
{
	public class TargetableAircraftInfo : TargetableUnitInfo
	{
		public readonly string[] GroundedTargetTypes = { };
		public override object Create(ActorInitializer init) { return new TargetableAircraft(init.self, this); }
	}

	public class TargetableAircraft : TargetableUnit
	{
		readonly TargetableAircraftInfo info;
		readonly Actor self;

		public TargetableAircraft(Actor self, TargetableAircraftInfo info)
			: base(self, info)
		{
			this.info = info;
			this.self = self;
		}

		public override string[] TargetTypes
		{
			get { return (self.CenterPosition.Z > 0) ? info.TargetTypes
				: info.GroundedTargetTypes; }
		}
	}
}
