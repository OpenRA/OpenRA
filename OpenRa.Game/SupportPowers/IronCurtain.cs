using OpenRa.Game.Orders;

namespace OpenRa.Game.SupportPowers
{
	class IronCurtain : ISupportPowerImpl
	{
		public void Activate(SupportPower p)
		{
			// todo: someone has to call SupportPower.FinishActivate when we're done!

			if (Game.controller.ToggleInputMode<IronCurtainOrderGenerator>())
				Sound.Play("slcttgt1.aud");
		}
	}
}
