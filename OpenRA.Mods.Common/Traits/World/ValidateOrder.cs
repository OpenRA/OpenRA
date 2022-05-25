#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Used to detect exploits. Attach this to the world actor.")]
	public class ValidateOrderInfo : TraitInfo<ValidateOrder> { }

	public class ValidateOrder : IValidateOrder
	{
		public bool OrderValidation(OrderManager orderManager, World world, int clientId, Order order)
		{
			if (order.Subject == null || order.Subject.Owner == null)
				return true;

			var subjectClientId = order.Subject.Owner.ClientIndex;
			var subjectClient = orderManager.LobbyInfo.ClientWithIndex(subjectClientId);
			var playerClient = orderManager.LobbyInfo.ClientWithIndex(clientId);
			var player = world.Players.FirstOrDefault(p => p.ClientIndex == playerClient.Index && p.PlayerReference.Playable);

			if (subjectClient == null)
			{
				Log.Write("debug", "Tick {0}: Order sent to {1}: resolved ClientIndex `{2}` doesn't exist", world.WorldTick, order.Subject.Owner.PlayerName, subjectClientId);
				return false;
			}

			if (player == null)
				return false;

			var isBotOrder = subjectClient.Bot != null && clientId == subjectClient.BotControllerClientIndex;

			// Drop orders from players who shouldn't be able to control this actor
			// This may be because the owner changed within the last net tick,
			// or, less likely, the client may be trying to do something malicious.
			if (!player.CanControlUnitsOf(order.Subject.Owner) && !isBotOrder)
				return false;

			return order.Subject.AcceptsOrder(order.OrderString);
		}
	}
}
