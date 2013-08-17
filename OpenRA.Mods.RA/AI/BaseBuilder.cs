#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;
using OpenRA.Mods.RA.Activities;
using XRandom = OpenRA.Thirdparty.Random;

namespace OpenRA.Mods.RA.AI
{
	class BaseBuilder
	{
		enum BuildState	{ ChooseItem, WaitForProduction, WaitForFeedback }

		BuildState state = BuildState.WaitForFeedback;
		string category;
		HackyAI ai;
		int lastThinkTick;
		Func<ProductionQueue, ActorInfo> chooseItem;

		public BaseBuilder(HackyAI ai, string category, Func<ProductionQueue, ActorInfo> chooseItem)
		{
			this.ai = ai;
			this.category = category;
			this.chooseItem = chooseItem;
		}

		public void Tick()
		{
			// Pick a free queue
			var queue = ai.FindQueues( category ).FirstOrDefault();
			if (queue == null)
				return;

			var currentBuilding = queue.CurrentItem();
			switch (state)
			{
				case BuildState.ChooseItem:
					{
						var item = chooseItem(queue);
						if (item == null)
						{
							state = BuildState.WaitForFeedback;
							lastThinkTick = ai.Ticks;
						}
						else
						{
							HackyAI.BotDebug("AI: Starting production of {0}".F(item.Name));
							state = BuildState.WaitForProduction;
							ai.BotWorld.IssueOrder(Order.StartProduction(queue.self, item.Name, 1));
						}
					}
					break;

				case BuildState.WaitForProduction:
					if (currentBuilding == null) return;	/* let it happen.. */

					else if (currentBuilding.Paused)
						ai.BotWorld.IssueOrder(Order.PauseProduction(queue.self, currentBuilding.Item, false));
					else if (currentBuilding.Done)
					{
						state = BuildState.WaitForFeedback;
						lastThinkTick = ai.Ticks;

						/* place the building */
						BuildingType type = BuildingType.Building;
						if(Rules.Info[currentBuilding.Item].Traits.Contains<AttackBaseInfo>())
							type = BuildingType.Defense;
						else if(Rules.Info[currentBuilding.Item].Traits.Contains<OreRefineryInfo>())
							type = BuildingType.Refinery;

						var location = ai.ChooseBuildLocation(currentBuilding.Item, type);
						if (location == null)
						{
							HackyAI.BotDebug("AI: Nowhere to place {0}".F(currentBuilding.Item));
							ai.BotWorld.IssueOrder(Order.CancelProduction(queue.self, currentBuilding.Item, 1));
						}
						else
						{
							ai.BotWorld.IssueOrder(new Order("PlaceBuilding", ai.BotPlayer.PlayerActor, false)
								{
									TargetLocation = location.Value,
									TargetString = currentBuilding.Item
								});
						}
					}
					break;

				case BuildState.WaitForFeedback:
					if (ai.Ticks - lastThinkTick > HackyAI.FeedbackTime)
						state = BuildState.ChooseItem;
					break;
			}
		}
	}
}

