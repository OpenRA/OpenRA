#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;

// TODO: Migrate these to be real widgets, and kill all the weird infrastructure that's holding these up.

namespace OpenRA.Mods.RA
{
	class PowerDownButtonInfo : TraitInfo<PowerDownButton> { }

	class PowerDownButton : IChromeButton
	{
		public string Image { get { return "power"; } }
		public bool Enabled { get { return true; } }
		public bool Pressed { get { return Game.controller.orderGenerator is PowerDownOrderGenerator; } }
		public void OnClick() { Game.controller.ToggleInputMode<PowerDownOrderGenerator>(); }

		public string Description { get { return "Powerdown"; } }
		public string LongDesc { get { return "Disable unneeded structures so their \npower can be used elsewhere"; } }
	}

	class SellButtonInfo : TraitInfo<SellButton> { }

	class SellButton : IChromeButton
	{
		public string Image { get { return "sell"; } }
		public bool Enabled { get { return true; } }
		public bool Pressed { get { return Game.controller.orderGenerator is SellOrderGenerator; } }
		public void OnClick() { Game.controller.ToggleInputMode<SellOrderGenerator>(); }

		public string Description { get { return "Sell"; } }
		public string LongDesc { get { return "Sell buildings, reclaiming a \nproportion of their build cost"; } }
	}

	class RepairButtonInfo : ITraitInfo
	{
		public readonly bool RequiresConstructionYard = true;
		public object Create(ActorInitializer init) { return new RepairButton(); }
	}

	class RepairButton : IChromeButton
	{
		public RepairButton() { }

		public string Image { get { return "repair"; } }
		public bool Enabled
		{
			get
			{
				// WTF: why are these buttons even traits?
				return RepairOrderGenerator.PlayerIsAllowedToRepair( Game.world );
			}
		}

		public bool Pressed { get { return Game.controller.orderGenerator is RepairOrderGenerator; } }
		public void OnClick() { Game.controller.ToggleInputMode<RepairOrderGenerator>(); }

		public string Description { get { return "Repair"; } }
		public string LongDesc
		{
			get
			{
				var s = "Repair damaged buildings";
				return Enabled ? s : s + "\n\nRequires: Construction Yard";
			}
		}
	}

	
}
