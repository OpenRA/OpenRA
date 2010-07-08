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
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
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

			self.World.AddFrameEndTask(_ =>
			{
				self.World.Remove(self);
				foreach (var s in sounds)
					Sound.PlayToPlayer(self.Owner, s, self.CenterLocation);

				var a = self.World.CreateActor(actor, self.Location + offset, self.Owner);
				a.Health = GetHealthToTransfer(self, a, transferPercentage);
			});
			return this;
		}
		
		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }

		public static int GetHealthToTransfer(Actor from, Actor to, bool transferPercentage)
		{
			var oldHP = from.GetMaxHP();
			var newHP = to.GetMaxHP();
			return (transferPercentage) 
				? (int)((float)from.Health / oldHP * newHP) 
				: Math.Min(from.Health, newHP);
		}
	}
}
