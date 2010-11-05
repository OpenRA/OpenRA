#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using OpenRA.Graphics;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{
    public class TargetableAircraftInfo : TargetableUnitInfo, ITraitPrerequisite<AircraftInfo>
    {
        public readonly string[] GroundedTargetTypes = { };
        public override object Create(ActorInitializer init) { return new TargetableAircraft(init.self, this); }
    }

    public class TargetableAircraft : TargetableUnit<TargetableAircraftInfo>
    {
        Aircraft Aircraft;
        public TargetableAircraft(Actor self, TargetableAircraftInfo info)
            : base(info)
        {
            Aircraft = self.Trait<Aircraft>();
        }

        public override string[] TargetTypes
        {
            get { return (Aircraft.Altitude > 0) ? info.TargetTypes 
                                                 : info.GroundedTargetTypes; }
        }
    }
}
