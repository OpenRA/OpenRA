using System;
using System.Linq;
using OpenRA.Orders;

namespace OpenRA.Traits
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
