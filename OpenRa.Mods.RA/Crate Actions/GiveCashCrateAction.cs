using OpenRa.Mods.RA.Effects;
using OpenRa.Traits;

namespace OpenRa.Mods.RA
{
	class GiveCashCrateActionInfo : ITraitInfo
	{
		public int Amount = 2000;
		public int SelectionShares = 10;
		public object Create(Actor self) { return new GiveCashCrateAction(self); }
	}

	class GiveCashCrateAction : ICrateAction
	{
		Actor self;
		public GiveCashCrateAction(Actor self)
		{
			this.self = self;
		}

		public int SelectionShares
		{
			get { return self.Info.Traits.Get<GiveCashCrateActionInfo>().SelectionShares; }
		}

		public void Activate(Actor collector)
		{
			collector.World.AddFrameEndTask(w =>
			{
				var amount = self.Info.Traits.Get<GiveCashCrateActionInfo>().Amount;
				collector.Owner.GiveCash(amount);
				w.Add(new CrateEffect(collector, "dollar"));
			});
		}
	}
}
