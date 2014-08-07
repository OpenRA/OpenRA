#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Traits
{

	[Desc("Announces any actor movement that occurs.", "This goes on the world actor.")]
	public class MovementAnnouncerInfo : ITraitInfo, Requires<ActorMapInfo>
	{
		public object Create(ActorInitializer init) { return new MovementAnnouncer(init.world, this); }
	}

	public class MovementAnnouncer : ITick
	{
		public readonly MovementAnnouncerInfo Info;
		public readonly HashSet<Actor> MovedPosition = new HashSet<Actor>();
		public readonly HashSet<Actor> MovedCell = new HashSet<Actor>();
		readonly World World;
		// List of all traits that listen for movement, so that trait lookups do not need to be done each tick
		readonly List<IMovementListener> Listeners = new List<IMovementListener>();

		public MovementAnnouncer(World world, MovementAnnouncerInfo info)
		{
			World = world;
			Info = info;
		}

		///<summary>Used by ActorMap to push movement data</summary>
		public void RegisterMovement(Actor movedActor, WPos? oldPos)
		{
			if (oldPos == null || movedActor.CenterPosition != oldPos.Value)
				MovedPosition.Add(movedActor);

			if (oldPos == null || World.Map.CellContaining(movedActor.CenterPosition) != World.Map.CellContaining(oldPos.Value))
				MovedCell.Add(movedActor);
		}

		///<summary>Subscribes a new listener to movement announcements</summary>
		public void RegisterListener(IMovementListener listener)
		{
			if (!Listeners.Contains(listener))
				Listeners.Add(listener);
		}

		///<summary>Unsubscribes a listener</summary>
		public void UnregisterListener(IMovementListener listener)
		{
			if (Listeners.Contains(listener))
				Listeners.Remove(listener);
		}

		///<summary>Each tick it tells all listeners about all movement</summary>
		public void Tick(Actor self)
		{
			if (MovedPosition.Any() || MovedCell.Any())
				foreach (var listener in Listeners)
				{
					listener.PositionMovementAnnouncement(MovedPosition);
					listener.CellMovementAnnouncement(MovedCell);
				}

			MovedPosition.Clear();
			MovedCell.Clear();
		}
	}
}
