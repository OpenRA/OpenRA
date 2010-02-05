using System;

namespace OpenRa.Traits.Activities
{
	class TransformIntoActor : IActivity
	{
		string actor = null;
		int2 offset;
		string[] sounds = null;
		bool transferPercentage;
		
		bool isCanceled;
		
		public TransformIntoActor(string actor, int2 offset, bool transferHealthPercentage, string[] sounds)
		{
			this.actor = actor;
			this.offset = offset;
			this.sounds = sounds;
			this.transferPercentage = transferHealthPercentage;
		}
		
		public IActivity NextActivity { get; set; }

		public IActivity Tick( Actor self )
		{
			if (isCanceled) return NextActivity;
			
			self.World.AddFrameEndTask( _ =>
			{
				var oldHP = self.GetMaxHP();
				var newHP = Rules.Info[actor].Traits.Get<OwnedActorInfo>().HP;
				var newHealth = (transferPercentage) ? (int)((float)self.Health/oldHP*newHP) : Math.Min(self.Health, newHP);
				
				self.Health = 0;
				self.World.Remove( self );
				foreach (var s in sounds)
					Sound.PlayToPlayer(self.Owner, s);

				var a = self.World.CreateActor( actor, self.Location + offset, self.Owner );
				a.Health = newHealth;
			} );
			return this;
		}
		
		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
