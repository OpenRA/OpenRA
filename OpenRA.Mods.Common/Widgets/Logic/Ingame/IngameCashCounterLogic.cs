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
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class IngameCashCounterLogic : ChromeLogic
	{
		[TranslationReference("usage", "capacity")]
		const string SiloUsage = "label-silo-usage";

		const float DisplayFracPerFrame = .07f;
		const int DisplayDeltaPerFrame = 37;

		readonly World world;
		readonly Player player;
		readonly PlayerResources playerResources;
		readonly LabelWithTooltipWidget cashLabel;
		readonly CachedTransform<(int Resources, int Capacity), string> siloUsageTooltipCache;
		readonly string cashTemplate;

		int nextCashTickTime = 0;
		int displayResources;

		string siloUsageTooltip = "";

		[ObjectCreator.UseCtor]
		public IngameCashCounterLogic(Widget widget, ModData modData, World world)
		{
			this.world = world;
			player = world.LocalPlayer;
			playerResources = player.PlayerActor.Trait<PlayerResources>();
			displayResources = playerResources.Cash + playerResources.Resources;

			siloUsageTooltipCache = new CachedTransform<(int Resources, int Capacity), string>(x =>
				modData.Translation.GetString(SiloUsage, Translation.Arguments("usage", x.Resources, "capacity", x.Capacity)));
			cashLabel = widget.Get<LabelWithTooltipWidget>("CASH");
			cashLabel.GetTooltipText = () => siloUsageTooltip;

			cashTemplate = cashLabel.Text;
		}

		public override void Tick()
		{
			if (nextCashTickTime > 0)
				nextCashTickTime--;

			var actual = playerResources.Cash + playerResources.Resources;

			var diff = Math.Abs(actual - displayResources);
			var move = Math.Min(Math.Max((int)(diff * DisplayFracPerFrame), DisplayDeltaPerFrame), diff);

			if (displayResources < actual)
			{
				displayResources += move;

				if (Game.Settings.Sound.CashTicks)
					Game.Sound.PlayNotification(world.Map.Rules, player, "Sounds", playerResources.Info.CashTickUpNotification, player.Faction.InternalName);
			}
			else if (displayResources > actual)
			{
				displayResources -= move;

				if (Game.Settings.Sound.CashTicks && nextCashTickTime == 0)
				{
					Game.Sound.PlayNotification(world.Map.Rules, player, "Sounds", playerResources.Info.CashTickDownNotification, player.Faction.InternalName);
					nextCashTickTime = 2;
				}
			}

			siloUsageTooltip = siloUsageTooltipCache.Update((playerResources.Resources, playerResources.ResourceCapacity));
			cashLabel.Text = cashTemplate.F(displayResources);
		}
	}
}
