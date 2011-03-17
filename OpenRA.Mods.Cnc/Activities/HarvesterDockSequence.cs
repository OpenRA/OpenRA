#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using System.Collections.Generic;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Render;
using System;

namespace OpenRA.Mods.Cnc
{
	public class HarvesterDockSequence : Activity
	{
		enum State
		{
			Wait,
			Turn,
			Dragin,
			Dock,
			Loop,
			Undock,
			Dragout
		};
		
		readonly Actor proc;
		readonly Harvester harv;
		readonly RenderBuilding rb;
		State state;
		
		int2 startDock;
		int2 endDock;
		public HarvesterDockSequence(Actor self, Actor proc)
		{
			this.proc = proc;
			state = State.Turn;
			harv = self.Trait<Harvester>();
			rb = proc.Trait<RenderBuilding>();
			startDock = self.Trait<IHasLocation>().PxPosition;
			endDock = proc.Trait<IHasLocation>().PxPosition + new int2(-15,8);
		}
		
		public override Activity Tick(Actor self)
		{
			switch (state)
			{
				case State.Wait:
					return this;
				case State.Turn:
					state = State.Dragin;
					return Util.SequenceActivities(new Turn(112), this);
				case State.Dragin:
					state = State.Dock;
					return Util.SequenceActivities(new Drag(startDock, endDock, 12), this);
				case State.Dock:
					harv.Visible = false;
					rb.PlayCustomAnimThen(proc, "dock-start", () => {rb.PlayCustomAnimRepeating(proc, "dock-loop"); state = State.Loop;});
					state = State.Wait;
					return this;
				case State.Loop:
					if (harv.TickUnload(self, proc))
						state = State.Undock;
					return this;
				case State.Undock:
					rb.PlayCustomAnimThen(proc, "dock-end", () => {harv.Visible = true; state = State.Dragout;});
					state = State.Wait;
					return this;
				case State.Dragout:
					return Util.SequenceActivities(new Drag(endDock, startDock, 12), NextActivity);
			}
			throw new InvalidOperationException("Invalid harvester dock state");
		}

		protected override bool OnCancel(Actor self)
		{
			state = State.Undock;
			return true;
		}

		public override IEnumerable<Target> GetTargetQueue( Actor self )
		{
			yield return Target.FromActor(proc);
		}
	}
}

