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

namespace OpenRA.Mods.TS
{
	public class VoxelHarvesterDockSequence : Activity
	{
		enum State { Turn, Dock, Loop, Undock }

		readonly Actor proc;
		readonly Harvester harv;
		readonly WithVoxelUnloadBody body;
		State state;

		public VoxelHarvesterDockSequence(Actor self, Actor proc)
		{
			this.proc = proc;
			state = State.Turn;
			harv = self.Trait<Harvester>();
			body = self.Trait<WithVoxelUnloadBody>();
		}

		public override Activity Tick(Actor self)
		{
			switch (state)
			{
				case State.Turn:
					state = State.Dock;
					return Util.SequenceActivities(new Turn(self, 160), this);
				case State.Dock:
					if (proc.Flagged(ActorFlag.InWorld) && !proc.Flagged(ActorFlag.Dead))
						foreach (var nd in proc.TraitsImplementing<INotifyDocking>())
							nd.Docked(proc, self);
					state = State.Loop;
					body.Docked = true;
					return this;
				case State.Loop:
					if (!proc.Flagged(ActorFlag.InWorld) || proc.Flagged(ActorFlag.Dead) || harv.TickUnload(self, proc))
						state = State.Undock;
					return this;
				case State.Undock:
					if (proc.Flagged(ActorFlag.InWorld) && !proc.Flagged(ActorFlag.Dead))
						foreach (var nd in proc.TraitsImplementing<INotifyDocking>())
							nd.Undocked(proc, self);
					body.Docked = false;
					return NextActivity;
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
