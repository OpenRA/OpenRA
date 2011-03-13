#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Orders
{
    public class PaletteOnlyOrderTargeter : IOrderTargeter
    {
        string orderName;
        public PaletteOnlyOrderTargeter(string orderName) { this.orderName = orderName; }

        public string OrderID { get { return orderName; } }
        public int OrderPriority { get { return 255; } }

        public bool CanTargetActor(Actor self, Actor target, bool forceAttack, bool forceMove, bool forceQueue, ref string cursor)
        {
            return false;
        }

        public bool CanTargetLocation(Actor self, int2 location, List<Actor> actorsAtLocation, bool forceAttack, bool forceQueue, bool forceMove, ref string cursor)
        {
            return false;
        }

        public bool IsQueued { get { return false; } }
    }
}
