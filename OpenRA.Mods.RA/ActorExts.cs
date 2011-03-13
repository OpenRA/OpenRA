using System;
using OpenRA.Traits;
namespace OpenRA.Mods.RA
{
	public static class ActorExts
	{
		public static bool AppearsFriendlyTo(this Actor self, Player toPlayer) 
		{ 
			if (self.HasTrait<Spy>())
			{
				if (self.Trait<Spy>().Disguised)
				{
					//TODO: check if we can see through disguise
					if ( toPlayer.Stances[self.Trait<Spy>().disguisedAsPlayer] == Stance.Ally)
						return true;
				}
				else 
				{
					if (toPlayer.Stances[self.Owner] == Stance.Ally)
						return true;
				}
				return false;
			}
			return toPlayer.Stances[self.Owner] == Stance.Ally;
		}
		
		public static bool AppearsHostileTo(this Actor self, Player toPlayer) 
		{ 
			if (self.HasTrait<Spy>())
			{
				if (toPlayer.Stances[self.Owner] == Stance.Ally)
					return false;
				
				if (self.Trait<Spy>().Disguised)
				{
					//TODO: check if we can see through disguise
                    if (toPlayer.Stances[self.Trait<Spy>().disguisedAsPlayer] == Stance.Enemy)
						return true;
				}
				else 
				{
                    if (toPlayer.Stances[self.Owner] == Stance.Enemy)
						return true;
				}
				return false;
			}
            return toPlayer.Stances[self.Owner] == Stance.Enemy;
		}

        public static bool AppearsHostileTo(this Actor self, Actor toActor)
        {
            return AppearsHostileTo(self, toActor.Owner);
        }

        public static bool AppearsFriendlyTo(this Actor self, Actor toActor)
        {
            return AppearsFriendlyTo(self, toActor.Owner);
        }
	}
}

