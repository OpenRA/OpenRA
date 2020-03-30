#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[Desc("Part of the new Lua API.")]
	public class LuaScriptInfo : ITraitInfo, Requires<SpawnMapActorsInfo>
	{
		public readonly HashSet<string> Scripts = new HashSet<string>();

		public object Create(ActorInitializer init) { return new LuaScript(this); }
	}

	public class LuaScript : ITick, IWorldLoaded, INotifyActorDisposing
	{
		readonly LuaScriptInfo info;
		ScriptContext context;
		bool disposed;

		public LuaScript(LuaScriptInfo info)
		{
			this.info = info;
		}

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer worldRenderer)
		{
			var scripts = info.Scripts ?? Enumerable.Empty<string>();
			context = new ScriptContext(world, worldRenderer, scripts);
			context.WorldLoaded();
		}

		void ITick.Tick(Actor self)
		{
			context.Tick(self);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (disposed)
				return;

			if (context != null)
				context.Dispose();

			disposed = true;
		}

		public bool FatalErrorOccurred { get { return context.FatalErrorOccurred; } }
	}
}
