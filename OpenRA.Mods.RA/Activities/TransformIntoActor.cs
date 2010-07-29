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
using System.Linq;
using System.Collections.Generic;
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

			self.World.AddFrameEndTask(w =>
			{
				var selected = w.Selection.Contains(self);
				
				self.World.Remove(self);
				foreach (var s in sounds)
					Sound.PlayToPlayer(self.Owner, s, self.CenterLocation);

				var a = w.CreateActor(actor, self.Location + offset, self.Owner);
				var health = a.traits.GetOrDefault<Health>();
				if (health != null)
				{
					health.TransferHPFromActor(a, self, transferPercentage);
				}
								
				if (selected)
					w.Selection.Add(w, a);
			});
			return this;
		}
		
		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
