#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;
using OpenRA.FileFormats;
using System.Linq;

namespace OpenRA.Mods.Cnc
{
	class SpawnViceroidInfo : ITraitInfo
	{
		[ActorReference]
		public readonly string ViceroidActor = "vice";
		public readonly int Probability = 10;
		public readonly string Owner = "Creeps";
		public readonly int InfDeath = 5;
		
		public object Create(ActorInitializer init) { return new SpawnViceroid(this); }
	}

	class SpawnViceroid : INotifyDamage
	{
		readonly SpawnViceroidInfo Info;
		
		public SpawnViceroid(SpawnViceroidInfo info)
		{
			Info = info;
		}
		
		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead && e.Warhead.InfDeath == Info.InfDeath
			    	&& self.World.SharedRandom.Next(100) <= Info.Probability)
				self.World.AddFrameEndTask(w =>
					{
						var td = new TypeDictionary 
						{
							new LocationInit( self.Location ),
							new OwnerInit( self.World.players.Values.First(p => p.InternalName == Info.Owner) )
						};
						
						if (self.HasTrait<IFacing>())
							td.Add(new FacingInit( self.Trait<IFacing>().Facing ));
						w.CreateActor(Info.ViceroidActor, td);
					});
		}
	}
}
