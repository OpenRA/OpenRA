using System;
using OpenRA.Traits;
namespace OpenRA.Mods.RA
{
	public static class ActorExts
	{
		public static bool AppearsFriendlyTo(this Actor self, Actor toActor) 
		{ 
			if (self.HasTrait<Spy>())
			{
				if (self.Trait<Spy>().Disguised)
				{
					//TODO: check if we can see through disguise
					if ( toActor.Owner.Stances[self.Trait<Spy>().disguisedAsPlayer] == Stance.Ally)
						return true;
				}
				else 
				{
					if (toActor.Owner.Stances[self.Owner] == Stance.Ally)
						return true;
				}
				return false;
			}
			return toActor.Owner.Stances[self.Owner] == Stance.Ally;
		}
		
		public static bool AppearsHostileTo(this Actor self, Actor toActor) 
		{ 
			if (self.HasTrait<Spy>())
			{
				if (toActor.Owner.Stances[self.Owner] == Stance.Ally)
					return false;
				
				if (self.Trait<Spy>().Disguised)
				{
					//TODO: check if we can see through disguise
					if ( toActor.Owner.Stances[self.Trait<Spy>().disguisedAsPlayer] == Stance.Enemy)
						return true;
				}
				else 
				{
					if (toActor.Owner.Stances[self.Owner] == Stance.Enemy)
						return true;
				}
				return false;
			}
			return toActor.Owner.Stances[self.Owner] == Stance.Enemy;
		}
	}
}

