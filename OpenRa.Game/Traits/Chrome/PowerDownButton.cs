using System;
using System.Linq;
using OpenRA.Orders;

namespace OpenRA.Traits
{
	class PowerDownButtonInfo : StatelessTraitInfo<PowerDownButton> { }

	class PowerDownButton : IChromeButton
	{
		public string Image { get { return "repair"; } }	// todo: art
		public bool Enabled { get { return true; } }
		public bool Pressed { get { return Game.controller.orderGenerator is PowerDownOrderGenerator; } }
		public void OnClick() { Game.controller.ToggleInputMode<PowerDownOrderGenerator>(); }
	}

	class SellButtonInfo : StatelessTraitInfo<SellButton> { }

	class SellButton : IChromeButton
	{
		public string Image { get { return "sell"; } }
		public bool Enabled { get { return true; } }
		public bool Pressed { get { return Game.controller.orderGenerator is SellOrderGenerator; } }
		public void OnClick() { Game.controller.ToggleInputMode<SellOrderGenerator>(); }
	}

	class RepairButtonInfo : StatelessTraitInfo<RepairButton> { }

	class RepairButton : IChromeButton
	{
		public string Image { get { return "repair"; } }	// todo: art
		public bool Enabled
		{
			get
			{
				if (!Game.Settings.RepairRequiresConyard)
					return true;

				return Game.world.Queries.OwnedBy[Game.world.LocalPlayer]
					.WithTrait<ConstructionYard>().Any();
			}
		}

		public bool Pressed { get { return Game.controller.orderGenerator is RepairOrderGenerator; } }
		public void OnClick() { Game.controller.ToggleInputMode<RepairOrderGenerator>(); }
	}
}
