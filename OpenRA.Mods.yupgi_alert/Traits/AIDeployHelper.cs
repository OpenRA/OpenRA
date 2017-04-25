#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Mods.AS.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Flags]
	public enum DeployType
	{
		None = 0,
		Attacked = 1,
		Attack = 2,
		Damage = 4,
	}

	[Desc("This unit can deploy automatically, when AI is the owner.")]
	public class AIDeployHelperInfo: ITraitInfo
	{
		[Desc("Events leading to the actor getting uncloaked. Possible values are: None, Attacked, Attack, Damage.")]
		public readonly DeployType DeployOn = DeployType.None;
		public readonly int UndeployTicks = 450;

		public object Create(ActorInitializer init) { return new AIDeployHelper(this); }
	}

	public class AIDeployHelper : INotifyDamageStateChanged, INotifyAttack, ITick
	{
		AIDeployHelperInfo Info;
		[Sync] int undeploy_ticks;

		public AIDeployHelper(AIDeployHelperInfo info)
		{
			Info = info;
		}

		void Deploy(Actor self)
		{
			undeploy_ticks = Info.UndeployTicks;

			if (!self.Owner.IsBot)
				return;

			// Issue deploy order to self.
			self.CancelActivity();

			var gc = self.TraitOrDefault<GrantConditionOnDeploy>();
			if (gc != null)
				gc.AIDeploy();

			var gct = self.TraitOrDefault<GrantTimedConditionOnDeploy>();
			if (gct != null)
				gct.AIDeploy();
		}

		void Undeploy(Actor self)
		{
			if (!self.Owner.IsBot)
				return;

			self.CancelActivity();

			// Issue undeploy order to self.
			var gc = self.TraitOrDefault<GrantConditionOnDeploy>();
			if (gc != null)
			{
				gc.AIUndeploy();
			}
		}

		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (Info.DeployOn.HasFlag(DeployType.Attack))
				Deploy(self);
		}
		
		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }

		void ITick.Tick(Actor self)
		{
			if (--undeploy_ticks < 0)
			{
				undeploy_ticks = Info.UndeployTicks;
				Undeploy(self);
			}
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			if (Info.DeployOn.HasFlag(DeployType.Damage))
			{
				Deploy(self);
			}
		}
	}
}
