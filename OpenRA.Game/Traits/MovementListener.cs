#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Traits
{
	public class MovementListener : IMovementListener, INotifyRemovedFromWorld, ITick
	{
		readonly World world;
		MovementAnnouncer Announcer;
		bool initAnnouncer = false;

		public MovementListener(World world)
		{
			this.world = world;
		}

		///<summary>Finds the Announcer trait and registers</summary>
		public virtual void Tick(Actor self)
		{
			// Cannot use constructor, since WorldActor is not setup yet
			if (!initAnnouncer)
			{
				initAnnouncer = true;
				Announcer = world.WorldActor.TraitOrDefault<MovementAnnouncer>();
				if (Announcer != null)
					Announcer.RegisterListener(this);
			}
		}

		///<summary>Unregisters from the Announcer</summary>
		public virtual void RemovedFromWorld(Actor self)
		{
			if (Announcer != null)
				Announcer.UnregisterListener(this);
		}

		///<summary>Reacts to all actors that changed positions</summary>
		public virtual void PositionMovementAnnouncement(HashSet<Actor> movedActors)
		{
			return;
		}

		///<summary>Reacts to all actors that changed cells</summary>
		public virtual void CellMovementAnnouncement(HashSet<Actor> movedActors)
		{
			return;
		}
	}
}
