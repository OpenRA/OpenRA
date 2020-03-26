#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Activities;

namespace OpenRA.Mods.Common.Traits
{
    [Desc("This actor can be occupied by an actor that is IDockable.")]
    public class DockInfo : ConditionalTraitInfo/*, Requires<DockingManagerInfo>*/
    {
        [Desc("Offset at which that the entering actor relative to the center of the dockable actor.")]
        public readonly WVec EnterOffset = WVec.Zero;
        [Desc("Cell offset where the actor enters the dock relative to top left cell of the dockable actor.")]
        public readonly CVec EntranceCell = CVec.Zero;

        public readonly int Facing = -1;

		/*[FieldLoader.Require]
		[Desc("DockingTypes (from the Dockers trait) that are able to dock this.")]
        public readonly BitSet<DockingType> Types = default(BitSet<DockingType>);
        */

        public override object Create(ActorInitializer init) { return new Dock(init.Self, this); }
    }

    public class Dock : ConditionalTrait<DockInfo>
    {
        //readonly DockingManager dockingManager;

        public Dock(Actor self, DockInfo info)
            : base(info)
        {
            //dockingManager = self.Trait<DockingManager>();
        }
    }
}