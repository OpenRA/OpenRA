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

namespace OpenRA
{
	public class DisposableAction : IDisposable
	{
		public DisposableAction(Action a) { this.a = a; }

		Action a;
		bool disposed;

		public void Dispose()
		{
			if (disposed) return;
			disposed = true;
			a();
			GC.SuppressFinalize(this);
		}

		~DisposableAction()
		{
			Dispose();
		}
	}
}
