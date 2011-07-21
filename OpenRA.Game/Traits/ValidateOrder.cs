#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Network;

namespace OpenRA.Traits
{
	public class ValidateOrderInfo : TraitInfo<ValidateOrder> { }

    public class ValidateOrder : IValidateOrder
    {
        public bool OrderValidation(OrderManager orderManager, World world, int clientId, Order order)
        {            
			if (order.Subject == null || order.Subject.Owner == null)
				return true;

			var subjectClient = order.Subject.Owner.ClientIndex;
			if (subjectClient >= orderManager.LobbyInfo.Clients.Count || subjectClient < 0)
			{
				Game.Debug("Order sent to {0}: resolved ClientIndex `{1}` is out of range", order.Subject.Owner.PlayerName, subjectClient);
				return false;
			}

			// Hack: Assumes bots always run on clientId 0.
			var isBotOrder = orderManager.LobbyInfo.Clients[subjectClient].Bot != null && clientId == 0;

			// Drop exploiting orders
			if (subjectClient != clientId && !isBotOrder)
            {
                Game.Debug("Detected exploit order from client {0}: {1}", clientId, order.OrderString);
                return false;
            }

            return true;
        }
    }
}
