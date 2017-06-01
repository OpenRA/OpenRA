#region Copyright & License Information
/*
 * Modded by Boolbada of OP mod, from Engineer repair enter activity.
 * 
 * Note: You can still use this without modifying the OpenRA engine itself by deleting 
 * FindAndTransitionToNextState. I just deleted a few lines of "movement" recovery code so that
 * interceptors can enter moving carrier.
 * However, for better results, consider modding the engine, as in the following commit:
 * https://github.com/forcecore/OpenRA/commit/fd36f63e508b7ad28e7d320355b7d257654b33ee
 * 
 * Also, interceptors sometimes try to land on ground level.
 * To mitigate that, I added LnadingDistance in Spawned trait.
 * However, that isn't perfect. For perfect results, Land.cs of the engine must be modified:
 * https://github.com/forcecore/OpenRA/commit/45970f57283150bc57ce86b8ce8a555018c6ca14
 * I couldn't make it independent as it relies on other stuff in Enter.cs too much.
 * 
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.yupgi_alert.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.yupgi_alert.Activities
{
	class EnterSpawner : Enter
	{
		readonly Actor master; // remember spawner.
		readonly Spawner spawner;
		readonly AmmoPool[] ammoPools;
		//readonly Dictionary<AmmoPool, int> ammoPoolsReloadTimes;

		public EnterSpawner(Actor self, Actor target, EnterBehaviour enterBehaviour)
			: base(self, target, enterBehaviour)
		{
			this.master = target;
			spawner = target.Trait<Spawner>();

			ammoPools = self.TraitsImplementing<AmmoPool>().Where(p => !p.Info.SelfReloads).ToArray();

			if (ammoPools == null)
				return;

			//ammoPoolsReloadTimes = ammoPools.ToDictionary(x => x, y => y.Info.ReloadDelay);
		}

		protected override bool CanReserve(Actor self)
		{
			return true;
		}

		protected override ReserveStatus Reserve(Actor self)
		{
			// TryReserveElseTryAlternateReserve calls Reserve and
			// the default inplementation of Reserve() returns TooFar when
			// the aircraft carrier is hovering over a building.
			// Since spawners don't need reservation (and have no reservation trait) just return Ready
			// so that spawner can enter no matter where the spawner is.
			return ReserveStatus.Ready;
		}

		protected override EnterState FindAndTransitionToNextState(Actor self)
		{
			switch (nextState)
			{
				case EnterState.ApproachingOrEntering:

					// Reserve to enter or approach
					isEnteringOrInside = false;
					switch (TryReserveElseTryAlternateReserve(self))
					{
						case ReserveStatus.None:
							return EnterState.Done; // No available target -> abort to next activity
						case ReserveStatus.TooFar:
							inner = move.MoveToTarget(self, targetCenter ? Target.FromPos(Target.CenterPosition) : Target); // Approach
							return EnterState.ApproachingOrEntering;
						case ReserveStatus.Pending:
							return EnterState.ApproachingOrEntering; // Retry next tick
						case ReserveStatus.Ready:
							break; // Reserved target -> start entering target
					}

					// Entering
					isEnteringOrInside = true;
					savedPos = self.CenterPosition; // Save position of self, before entering, for returning on exit

					inner = move.MoveIntoTarget(self, Target); // Enter

					if (inner != null)
					{
						nextState = EnterState.Inside; // Should be inside once inner activity is null
						return EnterState.ApproachingOrEntering;
					}

					// Can enter but there is no activity for it, so go inside without one
					goto case EnterState.Inside;

				case EnterState.Inside:
					// Might as well teleport into target if there is no MoveIntoTarget activity
					if (nextState == EnterState.ApproachingOrEntering)
						nextState = EnterState.Inside;

					// Boolbada: removed moving target recovery.
					// I'm assuming Carrier is not too fast!

					OnInside(self);

					if (enterBehaviour == EnterBehaviour.Suicide)
						self.Kill(self);
					else if (enterBehaviour == EnterBehaviour.Dispose)
						self.Dispose();

					// Return if Abort(Actor) or Done(self) was called from OnInside.
					if (nextState >= EnterState.Exiting)
						return EnterState.Inside;

					inner = this; // Start inside activity
					nextState = EnterState.Exiting; // Exit once inner activity is null (unless Done(self) is called)
					return EnterState.Inside;

				// TODO: Handle target moved while inside or always call done for movable targets and use a separate exit activity
				case EnterState.Exiting:
					inner = move.MoveIntoWorld(self, self.World.Map.CellContaining(savedPos));

					// If not successfully exiting, retry on next tick
					if (inner == null)
						return EnterState.Exiting;
					isEnteringOrInside = false;
					nextState = EnterState.Done;
					return EnterState.Exiting;

				case EnterState.Done:
					return EnterState.Done;
			}

			return EnterState.Done; // dummy to quiet dumb compiler
		}

		protected override void OnInside(Actor self)
		{
			if (master.IsDead)
				// entered the nydus canal but the entrance is dead immediately. haha;;
				return;

			Done(self); // no exit shit.

			// Load this thingy.
			// Issue attack move to the rally point.
			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead || master.IsDead || !spawner.CanLoad(master, self))
					return;

				spawner.Load(master, self);
				w.Remove(self);

				// Insta repair.
				var info = master.Info.TraitInfo<SpawnerInfo>();
				if (info.InstaRepair)
				{
					var health = self.Trait<Health>();
					self.InflictDamage(self, new Damage(-health.MaxHP));
				}

				// Insta re-arm. (Delayed launching is handled at spawner.)
				foreach (var pool in ammoPools)
				{
					while (pool.GiveAmmo()); // fill 'er up.
				}
			});
		}
	}
}
