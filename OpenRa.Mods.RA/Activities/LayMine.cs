using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Traits.Activities;

namespace OpenRa.Mods.RA.Activities
{
	class LayMine : IActivity
	{
		public IActivity NextActivity { get; set; }

		public IActivity Tick( Actor self )
		{
			self.World.AddFrameEndTask(
				w => w.CreateActor(self.Info.Traits.Get<MinelayerInfo>().Mine, self.Location, self.Owner));
			return NextActivity;
		}

		public void Cancel( Actor self )
		{
		}
	}
}
