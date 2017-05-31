#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;

namespace OpenRA.Mods.Common.Traits
{
	public class DockInfo : ITraitInfo
	{
		[Desc("Docking offset relative to top-left cell. Can be used as WVec or CVec.",
			"*Use CPosOffset to \"cast\" this as CPos.")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("When undocking, move this direction from the DOCK to avoid cluttering with other dock locations")]
		public readonly CVec ExitOffset = CVec.Zero;

		[Desc("Override Offset value and use center of the host actor as the dock offset?")]
		public readonly bool Center = true;

		[Desc("Just a waiting slot, not a dock that allows reloading / unloading / fixing")]
		public readonly bool WaitingPlace = false;

		[Desc("Dock angle. If < 0, the docker doesn't need to turn.")]
		public readonly int Angle = -1;

		[Desc("Does the refinery require the harvester to be dragged in?")]
		public readonly bool IsDragRequired = false;

		[Desc("Vector by which the harvester will be dragged when docking.")]
		public readonly WVec DragOffset = WVec.Zero;

		[Desc("In how many steps to perform the dragging?")]
		public readonly int DragLength = 0;

		[Desc("Priority of the docks, when managed by DockManager.")]
		public readonly int Order = 0;

		// "Cast" Offset as CVec.
		public CVec CPosOffset { get { return new CVec(Offset.X, Offset.Y); } }

		public object Create(ActorInitializer init) { return new Dock(init, this); }
	}

	public class Dock
	{
		public readonly DockInfo Info;
		readonly Actor self;

		public Actor Occupier;

		// Returns the location of the dock, interpreting Offset as CVec.
		public CPos Location { get { return self.Location + Info.CPosOffset; } }

		// blocked by some immoble obstacle?
		public bool IsBlocked;

		public Dock(ActorInitializer init, DockInfo info)
		{
			Info = info;
			self = init.Self;
		}

		// Update IsBlocked on request...
		// Could have made IsBlocked a property but that will make the game slow.
		// Only checks for immobile objects so if there is a permanently EMP'ed mobile actor there,
		// then this will not work...
		public void CheckObstacle()
		{
			foreach (var a in self.World.ActorMap.GetActorsAt(Location))
				if (a != self && a.TraitOrDefault<Mobile>() == null)
				{
					IsBlocked = true;
					return;
				}
			IsBlocked = false;
		}
	}
}
