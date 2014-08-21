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
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc
{
	public class HarvesterDockSequence : Activity
	{
		enum State { Wait, Turn, DragIn, Dock, Loop, Undock, DragOut }
		static readonly WVec DockOffset = new WVec(-640, 341, 0);

		readonly Actor proc;
		readonly Harvester harv;
		readonly RenderUnit ru;
		State state;

		WPos startDock, endDock;
		public HarvesterDockSequence(Actor self, Actor proc)
		{
			this.proc = proc;
			state = State.Turn;
			harv = self.Trait<Harvester>();
			ru = self.Trait<RenderUnit>();
			startDock = self.CenterPosition;
			endDock = proc.CenterPosition + DockOffset;
		}

		public override Activity Tick(Actor self)
		{
			switch (state)
			{
				case State.Wait:
					return this;
				case State.Turn:
					state = State.DragIn;
					return Util.SequenceActivities(new Turn(112), this);
				case State.DragIn:
					state = State.Dock;
					return Util.SequenceActivities(new Drag(startDock, endDock, 12), this);
				case State.Dock:
					ru.PlayCustomAnimation(self, "dock", () => {
						ru.PlayCustomAnimRepeating(self, "dock-loop");
						if (proc.IsInWorld && !proc.IsDead())
							foreach (var nd in proc.TraitsImplementing<INotifyDocking>())
								nd.Docked(proc, self);
						state = State.Loop;
					});
					state = State.Wait;
					return this;
				case State.Loop:
					if (!proc.IsInWorld || proc.IsDead() || harv.TickUnload(self, proc))
						state = State.Undock;
					return this;
				case State.Undock:
					ru.PlayCustomAnimBackwards(self, "dock", () => state = State.DragOut);
					if (proc.IsInWorld && !proc.IsDead())
						foreach (var nd in proc.TraitsImplementing<INotifyDocking>())
							nd.Undocked(proc, self);
					state = State.Wait;
					return this;
				case State.DragOut:
					return Util.SequenceActivities(new Drag(endDock, startDock, 12), NextActivity);
			}

			throw new InvalidOperationException("Invalid harvester dock state");
		}

		public override void Cancel(Actor self)
		{
			state = State.Undock;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromActor(proc);
		}
	}
}
