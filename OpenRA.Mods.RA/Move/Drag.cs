#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Move
{
	public class Drag : IActivity
	{
		IActivity NextActivity { get; set; }

		int2 endLocation;
		int2 startLocation;
		int length;

		public Drag(int2 start, int2 end, int length)
		{
			startLocation = start;
			endLocation = end;
			this.length = length;
		}
		
		int ticks = 0;
		public IActivity Tick( Actor self )
		{
			var mobile = self.Trait<Mobile>();
			mobile.PxPosition = int2.Lerp(startLocation, endLocation, ticks, length - 1);
			
			if (++ticks >= length)
			{
				mobile.IsMoving = false;
				return NextActivity;
			}
			mobile.IsMoving = true;
			return this;
		}

		public void Cancel(Actor self) {	}

		public void Queue( IActivity activity )
		{
			if( NextActivity != null )
				NextActivity.Queue( activity );
			else
				NextActivity = activity;
		}

		public IEnumerable<float2> GetCurrentPath()
		{
			yield return endLocation;
		}
	}
}
