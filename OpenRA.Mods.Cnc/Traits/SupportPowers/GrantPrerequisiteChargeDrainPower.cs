#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Grants a prerequisite while discharging at a configurable rate.")]
	public class GrantPrerequisiteChargeDrainPowerInfo : SupportPowerInfo, ITechTreePrerequisiteInfo
	{
		[Desc("Rate at which the power discharges compared to charging")]
		public readonly int DischargeModifier = 300;

		[FieldLoader.Require]
		[Desc("The prerequisite type that this provides.")]
		public readonly string Prerequisite = null;

		[Desc("Label to display over the support power icon and in its tooltip while the power is active.")]
		public readonly string ActiveText = "ACTIVE";

		[Desc("Label to display over the support power icon and in its tooltip while the power is available but not active.")]
		public readonly string AvailableText = "READY";

		IEnumerable<string> ITechTreePrerequisiteInfo.Prerequisites(ActorInfo info)
		{
			yield return Prerequisite;
		}

		public override object Create(ActorInitializer init) { return new GrantPrerequisiteChargeDrainPower(init.Self, this); }
	}

	public class GrantPrerequisiteChargeDrainPower : SupportPower, ITechTreePrerequisite, INotifyOwnerChanged
	{
		readonly GrantPrerequisiteChargeDrainPowerInfo info;
		TechTree techTree;
		bool active;

		public GrantPrerequisiteChargeDrainPower(Actor self, GrantPrerequisiteChargeDrainPowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		protected override void Created(Actor self)
		{
			techTree = self.Owner.PlayerActor.Trait<TechTree>();

			base.Created(self);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			techTree = newOwner.PlayerActor.Trait<TechTree>();
			active = false;
		}

		public override SupportPowerInstance CreateInstance(string key, SupportPowerManager manager)
		{
			return new DischargeableSupportPowerInstance(key, info, manager);
		}

		public void Activate(Actor self)
		{
			active = true;
			techTree.ActorChanged(self);
		}

		public void Deactivate(Actor self)
		{
			active = false;
			techTree.ActorChanged(self);
		}

		IEnumerable<string> ITechTreePrerequisite.ProvidesPrerequisites
		{
			get
			{
				if (!active)
					yield break;

				yield return info.Prerequisite;
			}
		}

		public class DischargeableSupportPowerInstance : SupportPowerInstance
		{
			// Whether the power is available to activate (even if not fully charged)
			bool available;

			// Whether the power is active right now
			// Note that this is fundamentally different to SupportPowerInstance.Active
			// which has a much closer meaning to available above.
			bool active;

			// Additional discharge rate accrued from damage
			int additionalDischargeSubTicks = 0;

			public DischargeableSupportPowerInstance(string key, GrantPrerequisiteChargeDrainPowerInfo info, SupportPowerManager manager)
				: base(key, info, manager) { }

			void Deactivate()
			{
				active = false;
				notifiedCharging = false;

				// Fully depleting the charge disables the power until it is again fully charged
				if (!Active || remainingSubTicks >= TotalTicks * 100)
					available = false;

				foreach (var p in Instances)
					((GrantPrerequisiteChargeDrainPower)p).Deactivate(p.Self);
			}

			public override void Tick()
			{
				var orig = remainingSubTicks;
				base.Tick();

				if (Ready)
					available = true;

				if (active && !Active)
					Deactivate();

				if (active)
				{
					remainingSubTicks = orig + ((GrantPrerequisiteChargeDrainPowerInfo)Info).DischargeModifier + additionalDischargeSubTicks;
					additionalDischargeSubTicks = 0;

					if (remainingSubTicks > TotalTicks * 100)
					{
						remainingSubTicks = TotalTicks * 100;
						Deactivate();
					}
				}
			}

			public void Discharge(int subTicks)
			{
				additionalDischargeSubTicks += subTicks;
			}

			public override void Target()
			{
				if (available && Active)
					Manager.Self.World.IssueOrder(new Order(Key, Manager.Self, false) { ExtraData = active ? 0U : 1U });
			}

			public override void Activate(Order order)
			{
				if (active && order.ExtraData == 0)
				{
					Deactivate();
					return;
				}

				if (!available || order.ExtraData != 1)
					return;

				var power = Instances.FirstOrDefault(i => !i.IsTraitPaused);
				if (power == null)
					return;

				active = true;

				// Only play the activation sound once!
				power.PlayLaunchSounds();

				foreach (var p in Instances)
					((GrantPrerequisiteChargeDrainPower)p).Activate(p.Self);
			}

			public override string IconOverlayTextOverride()
			{
				if (!Active)
					return null;

				var info = (GrantPrerequisiteChargeDrainPowerInfo)Info;
				return active ? info.ActiveText : available ? info.AvailableText : null;
			}

			public override string TooltipTimeTextOverride()
			{
				if (!Active)
					return null;

				var info = (GrantPrerequisiteChargeDrainPowerInfo)Info;
				return active ? info.ActiveText : available ? info.AvailableText : null;
			}
		}
	}
}
