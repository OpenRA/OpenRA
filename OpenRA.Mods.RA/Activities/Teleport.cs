#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class Teleport : IActivity
	{
		public IActivity NextActivity { get; set; }

		int2 destination;

		public Teleport(int2 destination)
		{
			this.destination = destination;
		}

		public IActivity Tick(Actor self)
		{
			self.TraitsImplementing<IMove>().FirstOrDefault().SetPosition(self, destination);
			return NextActivity;
		}

		public void Cancel(Actor self) { }
	}
}
