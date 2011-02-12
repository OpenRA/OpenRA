#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;

namespace OpenRA.FileFormats
{
	/// <summary>
	/// A thread-safe action queue, suitable for passing units of work between threads.
	/// </summary>
	public class ActionQueue
	{
		object syncRoot = new object();
		Action actions = () => { };

		public void Add(Action a)
		{
			lock (syncRoot)
				actions += a;
		}

		public void PerformActions()
		{
			Action a;
			lock (syncRoot)
			{
				a = actions; 
				actions = () => { };
			}
			a();
		}
	}
}
