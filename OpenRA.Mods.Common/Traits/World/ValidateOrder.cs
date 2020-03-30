#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
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

			if (subjectClient == null)
			{
				Log.Write("debug", "Order sent to {0}: resolved ClientIndex `{1}` doesn't exist", order.Subject.Owner.PlayerName, subjectClientId);
				return false;
			}

			var isBotOrder = subjectClient.Bot != null && clientId == subjectClient.BotControllerClientIndex;

			// Drop orders from players who shouldn't be able to control this actor
			// This may be because the owner changed within the last net tick,
			// or, less likely, the client may be trying to do something malicious.
			if (subjectClientId != clientId && !isBotOrder)
				return false;

			return order.Subject.AcceptsOrder(order.OrderString);
		}
	}
}
