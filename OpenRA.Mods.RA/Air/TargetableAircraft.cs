#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Graphics;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{
	public class TargetableAircraftInfo : TargetableUnitInfo, Requires<AircraftInfo>
	{
		public readonly string[] GroundedTargetTypes = { };
		public override object Create(ActorInitializer init) { return new TargetableAircraft(init.self, this); }
	}

	public class TargetableAircraft : TargetableUnit
	{
		readonly TargetableAircraftInfo info;
		readonly Aircraft Aircraft;

		public TargetableAircraft(Actor self, TargetableAircraftInfo info)
			: base(self, info)
		{
			this.info = info;
			Aircraft = self.Trait<Aircraft>();
		}

		public override string[] TargetTypes
		{
			get { return (Aircraft.Altitude > 0) ? info.TargetTypes
									             : info.GroundedTargetTypes; }
		}
	}
}
