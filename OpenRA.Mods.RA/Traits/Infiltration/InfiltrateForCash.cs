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
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	[Desc("This structure can be infiltrated causing funds to be stolen.")]
	class InfiltrateForCashInfo : ITraitInfo
	{
		[Desc("Percentage of the victim's resources that will be stolen.")]
		public readonly int Percentage = 100;

		[Desc("Amount of guaranteed funds to claim when the victim does not have enough resources.",
			"When negative, the production price of the infiltrating actor will be used instead.")]
		public readonly int Minimum = -1;

		[Desc("Maximum amount of funds which will be stolen.")]
		public readonly int Maximum = int.MaxValue;

		[Desc("Sound the victim will hear when they get robbed.")]
		public readonly string Notification = null;

		public object Create(ActorInitializer init) { return new InfiltrateForCash(this); }
	}

	class InfiltrateForCash : INotifyInfiltrated
	{
		readonly InfiltrateForCashInfo info;

		public InfiltrateForCash(InfiltrateForCashInfo info) { this.info = info; }

		public void Infiltrated(Actor self, Actor infiltrator)
		{
			var targetResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			var spyResources = infiltrator.Owner.PlayerActor.Trait<PlayerResources>();
			var spyValue = infiltrator.Info.TraitInfoOrDefault<ValuedInfo>();

			var toTake = Math.Min(info.Maximum, (targetResources.Cash + targetResources.Resources) * info.Percentage / 100);
			var toGive = Math.Max(toTake, info.Minimum >= 0 ? info.Minimum : spyValue != null ? spyValue.Cost : 0);

			targetResources.TakeCash(toTake);
			spyResources.GiveCash(toGive);

			if (info.Notification != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.Notification, self.Owner.Faction.InternalName);

			self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, infiltrator.Owner.Color.RGB, FloatingText.FormatCashTick(toGive), 30)));
		}
	}
}
