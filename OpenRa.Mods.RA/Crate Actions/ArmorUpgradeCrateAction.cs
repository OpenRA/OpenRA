using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Traits;
using OpenRa.Mods.RA.Effects;

namespace OpenRa.Mods.RA
{
	class ArmorUpgradeCrateActionInfo : ITraitInfo
	{
		public float Multiplier = 2.0f;
		public int SelectionShares = 10;
		public object Create(Actor self) { return new ArmorUpgradeCrateAction(self); }
	}

	class ArmorUpgradeCrateAction : ICrateAction
	{
		Actor self;
		public ArmorUpgradeCrateAction(Actor self)
		{
			this.self = self;
		}

		public int SelectionShares
		{
			get { return self.Info.Traits.Get<ArmorUpgradeCrateActionInfo>().SelectionShares; }
		}

		public void Activate(Actor collector)
		{
			Sound.PlayToPlayer(collector.Owner, "armorup1.aud");
			collector.World.AddFrameEndTask(w =>
			{
				var multiplier = self.Info.Traits.Get<ArmorUpgradeCrateActionInfo>().Multiplier;
				collector.traits.Add(new ArmorUpgrade(multiplier));
				w.Add(new CrateEffect(collector, "armor"));
			});
		}
	}

	class ArmorUpgrade : IDamageModifier
	{
		float multiplier;
		public ArmorUpgrade(float multiplier) { this.multiplier = 1/multiplier; }
		public float GetArmorModifier() { return multiplier; }
	}
}
