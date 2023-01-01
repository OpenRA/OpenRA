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

using System;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Funds are transferred from the owner to the infiltrator.")]
	class InfiltrateForCashInfo : TraitInfo
	{
		[Desc("The `TargetTypes` from `Targetable` that are allowed to enter.")]
		public readonly BitSet<TargetableType> Types = default;

		[Desc("Percentage of the victim's resources that will be stolen.")]
		public readonly int Percentage = 100;

		[Desc("Amount of guaranteed funds to claim when the victim does not have enough resources.",
			"When negative, the production price of the infiltrating actor will be used instead.")]
		public readonly int Minimum = -1;

		[Desc("Maximum amount of funds which will be stolen.")]
		public readonly int Maximum = int.MaxValue;

		[NotificationReference("Speech")]
		[Desc("Sound the victim will hear when they get robbed.")]
		public readonly string InfiltratedNotification = null;

		[Desc("Text notification the victim will see when they get robbed.")]
		public readonly string InfiltratedTextNotification = null;

		[NotificationReference("Speech")]
		[Desc("Sound the perpetrator will hear after successful infiltration.")]
		public readonly string InfiltrationNotification = null;

		[Desc("Text notification the perpetrator will see after successful infiltration.")]
		public readonly string InfiltrationTextNotification = null;

		[Desc("Whether to show the cash tick indicators rising from the actor.")]
		public readonly bool ShowTicks = true;

		public override object Create(ActorInitializer init) { return new InfiltrateForCash(this); }
	}

	class InfiltrateForCash : INotifyInfiltrated
	{
		readonly InfiltrateForCashInfo info;

		public InfiltrateForCash(InfiltrateForCashInfo info) { this.info = info; }

		void INotifyInfiltrated.Infiltrated(Actor self, Actor infiltrator, BitSet<TargetableType> types)
		{
			if (!info.Types.Overlaps(types))
				return;

			var targetResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			var spyResources = infiltrator.Owner.PlayerActor.Trait<PlayerResources>();
			var spyValue = infiltrator.Info.TraitInfoOrDefault<ValuedInfo>();

			var toTake = Math.Min(info.Maximum, (targetResources.Cash + targetResources.Resources) * info.Percentage / 100);
			var toGive = Math.Max(toTake, info.Minimum >= 0 ? info.Minimum : spyValue != null ? spyValue.Cost : 0);

			targetResources.TakeCash(toTake);
			spyResources.GiveCash(toGive);

			if (info.InfiltratedNotification != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.InfiltratedNotification, self.Owner.Faction.InternalName);

			if (info.InfiltrationNotification != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, infiltrator.Owner, "Speech", info.InfiltrationNotification, infiltrator.Owner.Faction.InternalName);

			TextNotificationsManager.AddTransientLine(info.InfiltratedTextNotification, self.Owner);
			TextNotificationsManager.AddTransientLine(info.InfiltrationTextNotification, infiltrator.Owner);

			if (info.ShowTicks)
				self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, infiltrator.Owner.Color, FloatingText.FormatCashTick(toGive), 30)));
		}
	}
}
