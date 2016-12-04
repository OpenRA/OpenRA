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

using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Produces an actor without using the standard production queue.")]
	public class PeriodicProducerInfo : UpgradableTraitInfo
	{
		[ActorReference, FieldLoader.Require]
		[Desc("Actors to produce.")]
		public readonly string[] Actors = null;

		[FieldLoader.Require]
		[Desc("Production queue type to use")]
		public readonly string Type = null;

		[Desc("Notification played when production is activated.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string ReadyAudio = null;

		[Desc("Notification played when the exit is jammed.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string BlockedAudio = null;

		[Desc("Duration between productions.")]
		public readonly int ChargeDuration = 1000;

		public readonly bool ResetTraitOnEnable = false;

		public readonly bool ShowSelectionBar = false;
		public readonly Color ChargeColor = Color.DarkOrange;

		public readonly bool PauseOnLowPower = false;

		public override object Create(ActorInitializer init) { return new PeriodicProducer(init, this); }
	}

	public class PeriodicProducer : UpgradableTrait<PeriodicProducerInfo>, ISelectionBar, ITick, ISync
	{
		readonly string faction;
		readonly PeriodicProducerInfo info;

		[Sync] int ticks;

		public PeriodicProducer(ActorInitializer init, PeriodicProducerInfo info)
			: base(info)
		{
			faction = init.Contains<FactionInit>() ? init.Get<FactionInit, string>() : init.Self.Owner.Faction.InternalName;
			this.info = info;
			ticks = info.ChargeDuration;
		}

		void ITick.Tick(Actor self)
		{
			if (info.PauseOnLowPower && self.IsDisabled())
				return;

			if (!IsTraitDisabled && --ticks < 0)
			{
				var sp = self.TraitsImplementing<Production>()
				.FirstOrDefault(p => p.Info.Produces.Contains(info.Type));

				var activated = false;

				if (sp != null)
					foreach (var name in info.Actors)
						activated |= sp.Produce(self, self.World.Map.Rules.Actors[name], faction);

				if (activated)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ReadyAudio, self.Owner.Faction.InternalName);
				else
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.BlockedAudio, self.Owner.Faction.InternalName);

				ticks = info.ChargeDuration;
			}
		}

		protected override void UpgradeEnabled(Actor self)
		{
			if (info.ResetTraitOnEnable)
				ticks = info.ChargeDuration;
		}

		float ISelectionBar.GetValue()
		{
			if (!info.ShowSelectionBar || IsTraitDisabled)
				return 0f;

			return (float)(info.ChargeDuration - ticks) / info.ChargeDuration;
		}

		Color ISelectionBar.GetColor()
		{
			return info.ChargeColor;
		}

		bool ISelectionBar.DisplayWhenEmpty
		{
			get { return info.ShowSelectionBar && !IsTraitDisabled; }
		}
	}
}
