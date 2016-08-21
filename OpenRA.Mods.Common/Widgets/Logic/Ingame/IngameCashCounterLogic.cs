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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class IngameCashCounterLogic : ChromeLogic
	{
		const float DisplayFracPerFrame = .07f;
		const int DisplayDeltaPerFrame = 37;

		readonly World world;
		readonly Player player;
		readonly PlayerResources playerResources;
		readonly string cashLabel;

		int nextCashTickTime = 0;
		int displayResources;
		string displayLabel;

		[ObjectCreator.UseCtor]
		public IngameCashCounterLogic(Widget widget, World world)
		{
			var cash = widget.Get<LabelWithTooltipWidget>("CASH");

			this.world = world;
			player = world.LocalPlayer;
			playerResources = player.PlayerActor.Trait<PlayerResources>();
			displayResources = playerResources.Cash + playerResources.Resources;
			cashLabel = cash.Text;
			displayLabel = cashLabel.F(displayResources);

			cash.GetText = () => displayLabel;
			cash.GetTooltipText = () => "Silo Usage: {0}/{1}".F(playerResources.Resources, playerResources.ResourceCapacity);
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
					Game.Sound.PlayNotification(world.Map.Rules, player, "Sounds", "CashTickUp", player.Faction.InternalName);
			}
			else if (displayResources > actual)
			{
				displayResources -= move;

				if (Game.Settings.Sound.CashTicks && nextCashTickTime == 0)
				{
					Game.Sound.PlayNotification(world.Map.Rules, player, "Sounds", "CashTickDown", player.Faction.InternalName);
					nextCashTickTime = 2;
				}
			}

			displayLabel = cashLabel.F(displayResources);
		}
	}
}
