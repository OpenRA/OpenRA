using OpenRa.Game.Orders;

namespace OpenRa.Game.SupportPowers
{
	class Chronoshift : ISupportPowerImpl
	{
		public void Activate(SupportPower p)
		{
			// todo: someone has to call SupportPower.FinishActivate when we're done!

			if (Game.controller.ToggleInputMode<ChronosphereSelectOrderGenerator>())
				Sound.Play("slcttgt1.aud");
		}
	}
}
