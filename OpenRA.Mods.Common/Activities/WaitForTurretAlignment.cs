#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Activities
{
    public class WaitForTurretAlignment : Activity
    {
        readonly Turreted turreted;
        readonly int desiredAlignment;

        public WaitForTurretAlignment(Actor self, int desiredAlignment)
        {
            this.turreted = self.Trait<Turreted>();
            this.desiredAlignment = desiredAlignment;
        }

        protected override void OnFirstRun(Actor self)
        {
            turreted.StopAiming(self);
        }

        public override Activity Tick(Actor self)
        {
            turreted.DesiredFacing = desiredAlignment;
            if (turreted.HasAchievedDesiredFacing)
                return NextActivity;
            return this;
        }
    }
}
