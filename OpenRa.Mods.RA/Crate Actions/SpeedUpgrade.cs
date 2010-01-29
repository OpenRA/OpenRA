using OpenRa.Mods.RA.Effects;
using OpenRa.Traits;

namespace OpenRa.Mods.RA
{
	class SpeedUpgradeCrateActionInfo : ITraitInfo
	{
		public float Multiplier = 1.7f;
		public int SelectionShares = 10;
		public object Create(Actor self) { return new SpeedUpgradeCrateAction(self); }
	}
	class SpeedUpgradeCrateAction : ICrateAction
	{
		Actor self;
		public SpeedUpgradeCrateAction(Actor self)
		{
			this.self = self;
		}
		
		public int SelectionShares
		{
			get { return self.Info.Traits.Get<SpeedUpgradeCrateActionInfo>().SelectionShares; }
		}
		
		public void Activate(Actor collector)
		{
			Sound.PlayToPlayer(collector.Owner, "unitspd1.aud");
			collector.World.AddFrameEndTask(w => 
			{
				float multiplier = self.Info.Traits.Get<SpeedUpgradeCrateActionInfo>().Multiplier;
				collector.traits.Add(new SpeedUpgrade(multiplier));
				w.Add(new CrateEffect(collector, "speed"));
			});
		}
	}
	
	class SpeedUpgrade : ISpeedModifier
	{
		float multiplier;
		public SpeedUpgrade(float multiplier) {	this.multiplier = multiplier; }
		public float GetSpeedModifier()	{ return multiplier; }
	}
}
