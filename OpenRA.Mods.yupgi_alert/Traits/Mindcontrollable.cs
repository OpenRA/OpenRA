#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

/* Works without base engine modification */

namespace OpenRA.Mods.Yupgi_alert.Traits
{
	[Desc("Can be mindcontrolled by mindcontrollers?")]
	public class MindcontrollableInfo : ConditionalTraitInfo
	{
		[Desc("The sound played when the unit is unmindcontrolled.")]
		public readonly string[] UnmindcontrolSound = null;

		public override object Create(ActorInitializer init) { return new Mindcontrollable(init.Self, this); }
	}

	class Mindcontrollable : ConditionalTrait<MindcontrollableInfo>, INotifyKilled, INotifyActorDisposing
	{
		readonly MindcontrollableInfo info;

		Actor master; // The actor who mindcontrolled this unit

		// who produced this unit?
		// After produced by Player A and mindcontrolled by player B then C,
		// this unit should return to player A when the most recent controller dies.
		Player creatorOwner;

		public Actor Master { get { return master; } }

		public Mindcontrollable(Actor self, MindcontrollableInfo info)
			: base(info)
		{
			this.info = info;
			creatorOwner = self.Owner;
		}

		// Transfer ownership
		public void LinkMaster(Actor self, Actor master)
		{
			// Reset anything it was doing.
			self.CancelActivity();

			// Current owner, most likely to be createrOwner but could be some other guy who MC'ed me before.
			var oldOwner = self.Owner;
			self.ChangeOwner(master.Owner);

			foreach (var t in self.TraitsImplementing<INotifyOwnerChanged>())
				t.OnOwnerChanged(self, self.Owner, master.Owner);

			UnlinkMaster(self, this.master); // Unlink old master.
			this.master = master; // Link new master.

			// In Kane's Wrath, when the MC'ed unit gets MC'ed back to the creatorOwner,
			// then all the MC stuff is cancelled.
			// Be sure to check with master.owner because self.Owner is not committed yet.
			if (master.Owner == creatorOwner)
				UnlinkMaster(self, master);
		}

		// Notify owner and let the owner get his capacity back.
		public void UnlinkMaster(Actor self, Actor master)
		{
			System.Diagnostics.Debug.Assert(this.master == null || this.master == master, "UnlinkMaster call from not-my-master!");

			this.master = null;
			if (master == null || master.Disposed || master.IsDead)
				return;
			master.Trait<Mindcontroller>().UnlinkSlave(master, self);
		}

		// Give this unit back to the creator owner.
		public void UnMindcontrol(Actor self, Player oldOwner)
		{
			// Reset anything it was doing.
			self.CancelActivity();

			// Current owner, most likely to be createrOwner but could be some other guy who MC'ed me before.
			self.ChangeOwner(creatorOwner);

			foreach (var t in self.TraitsImplementing<INotifyOwnerChanged>())
				t.OnOwnerChanged(self, oldOwner, creatorOwner);

			// Unlink old master.
			UnlinkMaster(self, master);

			// Play sound
			if (info.UnmindcontrolSound != null && info.UnmindcontrolSound.Any())
				Game.Sound.Play(SoundType.World, info.UnmindcontrolSound.Random(self.World.SharedRandom), self.CenterPosition);
		}

		public void Killed(Actor self, AttackInfo e)
		{
			UnlinkMaster(self, master);
		}

		public void Disposing(Actor self)
		{
			UnlinkMaster(self, master);
		}
	}
}