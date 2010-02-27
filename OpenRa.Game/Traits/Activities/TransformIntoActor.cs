#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;

namespace OpenRA.Traits.Activities
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
