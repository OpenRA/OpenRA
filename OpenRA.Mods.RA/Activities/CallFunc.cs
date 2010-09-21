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
	public class CallFunc : IActivity
	{
		public CallFunc(Action a) { this.a = a; }
		public CallFunc(Action a, bool interruptable)
		{
			this.a = a;
			this.interruptable = interruptable;
		}
		
		Action a;
		bool interruptable;
		IActivity NextActivity { get; set; }

		public IActivity Tick(Actor self)
		{
			if (a != null) a();
			return NextActivity;
		}

		public void Cancel(Actor self)
		{
			if (!interruptable)
				return;
			
			a = null;
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
