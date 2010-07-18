#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
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
