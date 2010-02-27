#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

namespace OpenRA.Traits
{
	public class CanPowerDownInfo : ITraitInfo
	{
		public object Create(Actor self) { return new CanPowerDown(); }
	}

	public class CanPowerDown : IDisable, IPowerModifier, IResolveOrder
	{
		[Sync]
		bool IsDisabled = false;
		
		public bool Disabled
		{
			get { return IsDisabled; }
			set { IsDisabled = value; }
		}
		
		public float GetPowerModifier() { return (IsDisabled) ? 0.0f : 1.0f; }	
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "PowerDown")
			{
				IsDisabled = !IsDisabled;
				var eva = self.Owner.PlayerActor.Info.Traits.Get<EvaAlertsInfo>();
				Sound.PlayToPlayer(self.Owner, IsDisabled ? eva.EnablePower : eva.DisablePower);
			}
		}
	}
}
