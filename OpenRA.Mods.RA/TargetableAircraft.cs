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

namespace OpenRA.Mods.RA
{
    public class TargetableAircraftInfo : TargetableInfo
    {
        public readonly string[] TargetTypes = { };
        public readonly string[] GroundedTargetTypes = { };
        public override object Create(ActorInitializer init) { return new TargetableAircraft(init.self, this); }
    }

    public class TargetableAircraft : ITargetable
    {
        TargetableAircraftInfo Info;
        Aircraft Aircraft;
        public TargetableAircraft(Actor self, TargetableAircraftInfo info)
        {
            Info = info;
            Aircraft = self.Trait<Aircraft>();
        }

        public string[] TargetTypes
        {
            get { return (Aircraft.Altitude > 0) ? Info.TargetTypes : Info.GroundedTargetTypes; }
        }
    }
}
