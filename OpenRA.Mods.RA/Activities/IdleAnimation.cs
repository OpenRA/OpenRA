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
using OpenRA.Mods.RA.Render;

namespace OpenRA.Mods.RA.Activities
{
	public class IdleAnimation : IActivity
	{
		string sequence;
		int delay;
		
		public IdleAnimation(string sequence, int delay)
		{			
			this.sequence = sequence;
			this.delay = delay;
		}

		IActivity NextActivity { get; set; }

		bool active = true;
		public IActivity Tick(Actor self)
		{
			if (!active) return NextActivity;
			
			if (delay > 0 && --delay == 0)
				self.Trait<RenderInfantry>().anim.PlayThen(sequence, () => active = false);
			
			return this;
		}

		public void Cancel(Actor self)
		{
			active = false;
			NextActivity = null;
		}

		public void Queue( IActivity activity )
		{
			if( NextActivity != null )
				NextActivity.Queue( activity );
			else
				NextActivity = activity;
		}
	}
}
