using System;
using System.Linq;
using OpenRA.Orders;

namespace OpenRA.Traits
{
	class PowerDownButtonInfo : StatelessTraitInfo<PowerDownButton> { }

	class PowerDownButton : IChromeButton
	{
		public string Image { get { return "power"; } }
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

	class RepairButtonInfo : ITraitInfo
	{
		public readonly bool RequiresConstructionYard = true;
		public object Create(Actor self) { return new RepairButton(this); }
	}

	class RepairButton : IChromeButton
	{
		RepairButtonInfo info;
		public RepairButton( RepairButtonInfo info ) { this.info = info; }

		public string Image { get { return "repair"; } }
		public bool Enabled
		{
			get
			{
				return !info.RequiresConstructionYard ||
					Game.world.Queries.OwnedBy[Game.world.LocalPlayer]
						.WithTrait<ConstructionYard>().Any();
			}
		}

		public bool Pressed { get { return Game.controller.orderGenerator is RepairOrderGenerator; } }
		public void OnClick() { Game.controller.ToggleInputMode<RepairOrderGenerator>(); }
	}
}
