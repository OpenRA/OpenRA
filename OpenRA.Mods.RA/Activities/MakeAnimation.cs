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

namespace OpenRA.Mods.RA.Activities
{
	class MakeAnimation : CancelableActivity
	{
		readonly bool Reversed;
		
		public MakeAnimation(Actor self) : this(self, false) {}
		public MakeAnimation(Actor self, bool reversed)
		{
			Reversed = reversed;
		}
		
		bool complete = false;
		bool started = false;
		public override IActivity Tick( Actor self )
		{
			if (IsCanceled) return NextActivity;
			if (!started)
			{
				var rb = self.Trait<RenderBuilding>();
				started = true;
				if (Reversed)
				{
					foreach (var s in self.Info.Traits.Get<BuildingInfo>().SellSounds)
						Sound.PlayToPlayer(self.Owner, s, self.CenterLocation);
					
					// PlayCustomAnim is required to stop a frame of the normal state after the anim completes
					rb.PlayCustomAnimBackwards(self, "make", () => {rb.PlayCustomAnim(self, "make"); complete = true;});
				}
				else
					rb.PlayCustomAnimThen(self, "make", () => complete = true);
			}
			return complete ? NextActivity : this;
		}
		
		// Not actually cancellable
		protected override bool OnCancel( Actor self ) { return false; }
	}
}
