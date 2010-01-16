
namespace OpenRa.Traits.Activities
{
	public class Teleport : IActivity
	{
		public IActivity NextActivity { get; set; }

		int2 destination;

		public Teleport(int2 destination)
		{
			this.destination = destination;
		}

		public IActivity Tick(Actor self)
		{
			var mobile = self.traits.Get<Mobile>();
			mobile.TeleportTo(self, destination);
			return NextActivity;
		}

		public void Cancel(Actor self) { }
	}
}
