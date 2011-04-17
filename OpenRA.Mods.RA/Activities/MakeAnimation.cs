#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using System;

namespace OpenRA.Mods.RA.Activities
{
	class MakeAnimation : Activity
	{
		readonly bool Reversed;
		readonly Action OnComplete;
		
		public MakeAnimation(Actor self, Action onComplete) : this(self, false, onComplete) {}
		public MakeAnimation(Actor self, bool reversed, Action onComplete)
		{
			Reversed = reversed;
			OnComplete = onComplete;
		}
		
		bool complete = false;
		bool started = false;
		public override Activity Tick( Actor self )
		{
			if (!started)
			{
				var rb = self.Trait<RenderBuilding>();
				started = true;
				if (Reversed)
				{
					var bi = self.Info.Traits.GetOrDefault<BuildingInfo>();
					if (bi != null)
						foreach (var s in bi.SellSounds)
							Sound.PlayToPlayer(self.Owner, s, self.CenterLocation);
					
					rb.PlayCustomAnimBackwards(self, "make", () => { OnComplete(); complete = true;});
				}
				else
					rb.PlayCustomAnimThen(self, "make", () => { OnComplete(); complete = true;});
			}
			return complete ? NextActivity : this;
		}
		
		// Cannot be cancelled
		public override void Cancel( Actor self ) { }
	}
}
