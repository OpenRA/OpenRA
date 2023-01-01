#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Graphics;

namespace OpenRA.Effects
{
	public class AsyncAction : IEffect
	{
		readonly Action a;
		readonly IAsyncResult ar;

		public AsyncAction(IAsyncResult ar, Action a)
		{
			this.a = a;
			this.ar = ar;
		}

		public void Tick(World world)
		{
			if (ar.IsCompleted)
			{
				world.AddFrameEndTask(w => { w.Remove(this); a(); });
			}
		}

		public IEnumerable<IRenderable> Render(WorldRenderer r) { yield break; }
	}
}
