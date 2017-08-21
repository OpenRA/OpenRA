#region Copyright & License Information
/*
 * Written by Boolbada of OP Mod.
 * Follows GPLv3 License as the OpenRA engine:
 * 
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

/*
 * Needs base engine modification. (Becaus MobSpawner.cs mods it)
 */

namespace OpenRA.Mods.Yupgi_alert.Traits
{
	[Desc("Can be slaved to a Mob spawner.")]
	public class MobSpawnerSlaveInfo : BaseSpawnerSlaveInfo
	{
		public override object Create(ActorInitializer init) { return new MobSpawnerSlave(init, this); }
	}

	public class MobSpawnerSlave : BaseSpawnerSlave, INotifySelected
	{
		readonly Actor self;
		// readonly MobSpawnerSlaveInfo info;

		public IMove[] Moves { get; private set; }
		public IPositionable Positionable { get; private set; }

		MobSpawnerMaster spawnerMaster;

		// TODO: add more activities for aircrafts
		public bool IsMoving { get { return self.CurrentActivity is Move; } }

		public MobSpawnerSlave(ActorInitializer init, MobSpawnerSlaveInfo info) : base(init, info)
		{
			this.self = init.Self;
			// this.info = info;
		}

		public override void Created(Actor self)
		{
			base.Created(self);

			Moves = self.TraitsImplementing<IMove>().ToArray();

			var positionables = self.TraitsImplementing<IPositionable>();
			if (positionables.Count() != 1)
				throw new InvalidOperationException("Actor {0} has multiple (or no) traits implementing IPositionable.".F(self));

			Positionable = positionables.First();
		}

		public override void LinkMaster(Actor self, Actor master, BaseSpawnerMaster spawnerMaster)
		{
			base.LinkMaster(self, master, spawnerMaster);
			this.spawnerMaster = spawnerMaster as MobSpawnerMaster;
		}

		public void Move(Actor self, CPos location)
		{
			// And tell attack bases to stop attacking.
			if (Moves.Length == 0)
				return;

			foreach (var mv in Moves)
				if (mv.IsTraitEnabled())
				{
					self.QueueActivity(mv.MoveTo(location, 2));
					break;
				}
		}

		public void AttackMove(Actor self, CPos location)
		{
			// And tell attack bases to stop attacking.
			if (Moves.Length == 0)
				return;

			foreach (var mv in Moves)
				if (mv.IsTraitEnabled())
				{
					// Must cancel before queueing as the master's attack move order is
					// issued multiple times on multiple points along the attack move path.
					self.CancelActivity();
					self.QueueActivity(new AttackMoveActivity(self, mv.MoveTo(location, 1)));
					break;
				}
		}

		void INotifySelected.Selected(Actor self)
		{
			if (spawnerMaster.Info.SlavesHaveFreeWill)
				return;

			// I'm assuming these guys are selectable, both slave and the nexus.
			// self.World.Selection.Remove(self.World, self); No need to remove when you don't wee the selection decoration.
			// -SelectionDecorations: is all you need.
			// Also use RejectsOrder if necessary.
			self.World.Selection.Add(self.World, Master);
		}
	}
}
