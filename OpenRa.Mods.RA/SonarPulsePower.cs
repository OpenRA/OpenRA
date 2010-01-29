using OpenRa.Traits;

namespace OpenRa.Mods.RA
{
	public class SonarPulsePowerInfo : SupportPowerInfo
	{
		public override object Create(Actor self) { return new SonarPulsePower(self, this); }
	}

	public class SonarPulsePower : SupportPower, IResolveOrder
	{
		public SonarPulsePower(Actor self, SonarPulsePowerInfo info) : base(self, info) { }

		protected override void OnBeginCharging() { }
		protected override void OnFinishCharging() { Sound.PlayToPlayer(Owner, "pulse1.aud"); }

		protected override void OnActivate()
		{
			Game.IssueOrder(new Order("SonarPulse", Owner.PlayerActor));
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "SonarPulse")
			{
				// TODO: Reveal submarines

				// Should this play for all players?
				Sound.Play("sonpulse.aud");
				FinishActivate();
			}
		}
	}
}
