#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("This actor can be mind controlled by other actors.")]
	public class MindControllableInfo : PausableConditionalTraitInfo
	{
		[Desc("Condition to grant when under mindcontrol.")]
		[GrantedConditionReference]
		public readonly string Condition = null;

		[Desc("The sound played when the mindcontrol is revoked.")]
		public readonly string[] RevokeControlSounds = { };

		public override object Create(ActorInitializer init) { return new MindControllable(init.Self, this); }
	}

	public class MindControllable : PausableConditionalTrait<MindControllableInfo>, INotifyKilled, INotifyActorDisposing, INotifyCreated, INotifyOwnerChanged
	{
		readonly MindControllableInfo info;

		Actor master;
		Player creatorOwner;
		bool controlChanging;

		ConditionManager conditionManager;
		int token = ConditionManager.InvalidConditionToken;

		public Actor Master { get { return master; } }

		public MindControllable(Actor self, MindControllableInfo info)
			: base(info)
		{
			this.info = info;
		}

		protected override void Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
		}

		public void LinkMaster(Actor self, Actor master)
		{
			self.CancelActivity();

			if (this.master == null)
				creatorOwner = self.Owner;

			controlChanging = true;

			var oldOwner = self.Owner;
			self.ChangeOwner(master.Owner);

			UnlinkMaster(self, this.master);
			this.master = master;

			if (conditionManager != null && token == ConditionManager.InvalidConditionToken && !string.IsNullOrEmpty(Info.Condition))
				token = conditionManager.GrantCondition(self, Info.Condition);

			if (master.Owner == creatorOwner)
				UnlinkMaster(self, master);

			self.World.AddFrameEndTask(_ => controlChanging = false);
		}

		public void UnlinkMaster(Actor self, Actor master)
		{
			if (master == null)
				return;

			self.World.AddFrameEndTask(_ =>
				{
					if (master.IsDead || master.Disposed)
						return;

					master.Trait<MindController>().UnlinkSlave(master, self);
				});

			this.master = null;

			if (conditionManager != null && token != ConditionManager.InvalidConditionToken)
				token = conditionManager.RevokeCondition(self, token);
		}

		public void RevokeMindControl(Actor self)
		{
			self.CancelActivity();

			controlChanging = true;

			if (creatorOwner.WinState == WinState.Lost)
				self.ChangeOwner(self.World.WorldActor.Owner);
			else
				self.ChangeOwner(creatorOwner);

			UnlinkMaster(self, master);

			if (info.RevokeControlSounds.Any())
				Game.Sound.Play(SoundType.World, info.RevokeControlSounds.Random(self.World.SharedRandom), self.CenterPosition);

			self.World.AddFrameEndTask(_ => controlChanging = false);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			UnlinkMaster(self, master);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			UnlinkMaster(self, master);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (!controlChanging)
				UnlinkMaster(self, master);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (master != null)
				RevokeMindControl(self);
		}
	}
}