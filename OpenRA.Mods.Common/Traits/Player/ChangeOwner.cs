using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public abstract class ChangeOwnerInfo : ITraitInfo
	{
		public abstract object Create(ActorInitializer init);
	}

	public abstract class ChangeOwner
	{
		protected void NeedChangeOwner(Actor self, Actor actor, Player newOwner)
		{
			var oldOwner = self.Owner;
			self.ChangeOwner(newOwner);

			foreach (var t in self.TraitsImplementing<INotifyCapture>())
				t.OnCapture(self, actor, oldOwner, newOwner);
		}
	}
}