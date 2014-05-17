#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class RAHarvesterDockSequence : Activity
	{
		enum State { Wait, Turn, Dock, Loop, Undock, Complete };

		readonly Actor proc;
		readonly int angle;
		readonly Harvester harv;
		readonly RenderUnit ru;
		State state;

		public RAHarvesterDockSequence(Actor self, Actor proc, int angle)
		{
			this.proc = proc;
			this.angle = angle;
			state = State.Turn;
			harv = self.Trait<Harvester>();
			ru = self.Trait<RenderUnit>();
		}

		public override Activity Tick(Actor self)
		{
			switch (state)
			{
				case State.Wait:
					return this;
				case State.Turn:
					state = State.Dock;
					return Util.SequenceActivities(new Turn(angle), this);
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
					ru.PlayCustomAnimBackwards(self, "dock", () => state = State.Complete);
					state = State.Wait;
					return this;
				case State.Complete:
					harv.LastLinkedProc = harv.LinkedProc;
					harv.LinkProc(self, null);
					if (proc.IsInWorld && !proc.IsDead())
						foreach (var nd in proc.TraitsImplementing<INotifyDocking>())
							nd.Undocked(proc, self);
					return NextActivity;
			}

			throw new InvalidOperationException("Invalid harvester dock state");
		}

		public override void Cancel(Actor self)
		{
			state = State.Undock;
			base.Cancel(self);
		}

		public override IEnumerable<Target> GetTargets( Actor self )
		{
			yield return Target.FromActor(proc);
		}
	}
}

