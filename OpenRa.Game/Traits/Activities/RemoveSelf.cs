
namespace OpenRa.Traits.Activities
{
	public class RemoveSelf : IActivity
	{
		bool isCanceled;
		public IActivity NextActivity { get; set; }

		public IActivity Tick(Actor self)
		{
			if (isCanceled) return NextActivity;
			self.World.AddFrameEndTask(w => w.Remove(self));
			return null;
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
