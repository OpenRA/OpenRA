#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Threading;

namespace OpenRA.Platforms.Default
{
	abstract class ThreadAffine
	{
		readonly int managedThreadId;

		protected ThreadAffine()
		{
			managedThreadId = Thread.CurrentThread.ManagedThreadId;
		}

		protected void VerifyThreadAffinity()
		{
			if (managedThreadId != Thread.CurrentThread.ManagedThreadId)
				throw new InvalidOperationException("Cross-thread operation not valid: This method must be called from the same thread that created this object.");
		}
	}
}
