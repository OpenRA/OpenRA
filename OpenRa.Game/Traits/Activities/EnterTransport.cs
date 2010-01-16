using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Traits.Activities
{
	class EnterTransport : IActivity
	{
		public IActivity NextActivity { get; set; }
		bool isCanceled;
		public Actor transport;

		public EnterTransport(Actor self, Actor transport)
		{
			this.transport = transport;
		}

		public IActivity Tick(Actor self)
		{
			if (isCanceled) return NextActivity;
			if (transport == null || !transport.IsInWorld) return NextActivity;

			var cargo = transport.traits.Get<Cargo>();
			if (cargo.IsFull(transport)) 
				return NextActivity;

			cargo.Load(transport, self);
			Game.world.AddFrameEndTask(w => w.Remove(self));

			return this;
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
