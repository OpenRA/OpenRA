#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Grants a condition periodically.")]
	public class GrantPeriodicConditionInfo : ConditionalTraitInfo
	{
		[GrantedConditionReference, FieldLoader.Require]
		[Desc("The condition to grant.")]
		public readonly string Condition = null;

		[Desc("The range of time (in ticks) with the condition being disabled.")]
		public readonly int[] CooldownDuration = { 1000 };

		[Desc("The range of time (in ticks) with the condition being enabled.")]
		public readonly int[] ActiveDuration = { 100 };

		public readonly bool StartsGranted = false;

		public readonly bool ResetTraitOnEnable = false;

		public readonly bool ShowSelectionBar = false;
		public readonly Color CooldownColor = Color.DarkRed;
		public readonly Color ActiveColor = Color.DarkMagenta;

		public override object Create(ActorInitializer init) { return new GrantPeriodicCondition(init, this); }
	}

	public class GrantPeriodicCondition : ConditionalTrait<GrantPeriodicConditionInfo>, INotifyCreated, ISelectionBar, ITick, ISync
	{
		readonly Actor self;
		readonly GrantPeriodicConditionInfo info;

		ConditionManager manager;
		[Sync] int ticks;
		int cooldown, active;
		bool isSuspended;
		int token = ConditionManager.InvalidConditionToken;

		bool IsEnabled { get { return token != ConditionManager.InvalidConditionToken; } }

		public GrantPeriodicCondition(ActorInitializer init, GrantPeriodicConditionInfo info)
			: base(info)
		{
			self = init.Self;
			this.info = info;
		}

		void SetDefaultState()
		{
			if (info.StartsGranted)
			{
				ticks = info.ActiveDuration.Length == 2
					? self.World.SharedRandom.Next(info.ActiveDuration[0], info.ActiveDuration[1])
					: info.ActiveDuration[0];
				active = ticks;
				if (info.StartsGranted != IsEnabled)
					EnableCondition();
			}
			else
			{
				ticks = info.CooldownDuration.Length == 2
					? self.World.SharedRandom.Next(info.CooldownDuration[0], info.CooldownDuration[1])
					: info.CooldownDuration[0];
				cooldown = ticks;
				if (info.StartsGranted != IsEnabled)
					DisableCondition();
			}
		}

		void INotifyCreated.Created(Actor self)
		{
			manager = self.Trait<ConditionManager>();

			if (!IsTraitDisabled)
				SetDefaultState();
		}

		void ITick.Tick(Actor self)
		{
			if (!IsTraitDisabled && --ticks < 0)
			{
				if (IsEnabled)
				{
					ticks = info.CooldownDuration.Length == 2
						? self.World.SharedRandom.Next(info.CooldownDuration[0], info.CooldownDuration[1])
						: info.CooldownDuration[0];
					cooldown = ticks;
					DisableCondition();
				}
				else
				{
					ticks = info.ActiveDuration.Length == 2
						? self.World.SharedRandom.Next(info.ActiveDuration[0], info.ActiveDuration[1])
						: info.ActiveDuration[0];
					active = ticks;
					EnableCondition();
				}
			}
		}

		protected override void TraitEnabled(Actor self)
		{
			if (info.ResetTraitOnEnable)
				SetDefaultState();
			else if (isSuspended)
				EnableCondition();

			isSuspended = false;
		}

		protected override void TraitDisabled(Actor self)
		{
			if (IsEnabled)
			{
				DisableCondition();
				isSuspended = true;
			}
		}

		void EnableCondition()
		{
			if (token == ConditionManager.InvalidConditionToken)
				token = manager.GrantCondition(self, info.Condition);
		}

		void DisableCondition()
		{
			if (token != ConditionManager.InvalidConditionToken)
				token = manager.RevokeCondition(self, token);
		}

		float ISelectionBar.GetValue()
		{
			if (!info.ShowSelectionBar)
				return 0f;

			return IsEnabled
				? (float)(active - ticks) / active
					: (float)ticks / cooldown;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return info.ShowSelectionBar; } }

		Color ISelectionBar.GetColor() { return IsEnabled ? info.ActiveColor : info.CooldownColor; }
	}
}
