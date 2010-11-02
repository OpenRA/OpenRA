#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Network;

namespace OpenRA.Traits
{
	public class ValidateOrderInfo : TraitInfo<ValidateOrder> { }

    public class ValidateOrder : IValidateOrder
    {
        public bool OrderValidation(OrderManager orderManager, World world, int clientId, Order order)
        {            
            // Drop exploiting orders
            if (order.Subject != null && order.Subject.Owner.ClientIndex != clientId)
            {
                Game.Debug("Detected exploit order from {0}: {1}".F(clientId, order.OrderString));
                return false;
            }

            return true;
        }
    }
}
