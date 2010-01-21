using OpenRa.Effects;
using OpenRa.Traits.Activities;

namespace OpenRa.Mods.RA.Activities
{
	class Demolish : IActivity
	{
		Actor target;
		public IActivity NextActivity { get; set; }

		public Demolish( Actor target )
		{
			this.target = target;
		}

		public IActivity Tick(Actor self)
		{
			if (target == null || target.IsDead) return NextActivity;
			self.World.AddFrameEndTask(w => w.Add(new DelayedAction(25*2, 
				() => target.InflictDamage(self, target.Health, Rules.WarheadInfo["DemolishWarhead"]))));
			return NextActivity;
		}

		public void Cancel(Actor self) { target = null; NextActivity = null; }
	}
}
