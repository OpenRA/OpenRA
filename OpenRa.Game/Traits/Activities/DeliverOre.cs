using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits.Activities
{
	class DeliverOre : Activity
	{
		public Activity NextActivity { get; set; }

		bool isDone;

		public void Tick(Actor self, Mobile mobile)
		{
			if (isDone)
			{
				mobile.InternalSetActivity(NextActivity);
				/* todo: return to the ore patch */
				return;
			}

			var renderUnit = self.traits.WithInterface<RenderUnit>().First();
			if (renderUnit.anim.CurrentSequence.Name != "empty")
				renderUnit.PlayCustomAnimation(self, "empty", 
					() => isDone = true);
		}

		public void Cancel(Actor self, Mobile mobile)
		{
			mobile.InternalSetActivity(null);
		}
	}
}
