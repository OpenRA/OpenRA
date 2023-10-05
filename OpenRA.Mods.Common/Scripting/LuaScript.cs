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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[TraitLocation(SystemActors.World)]
	[Desc("Part of the new Lua API.")]
	public class LuaScriptInfo : TraitInfo, Requires<SpawnMapActorsInfo>, NotBefore<SpawnStartingUnitsInfo>
	{
		[Desc("File names with location relative to the map.")]
		public readonly HashSet<string> Scripts = new();

		public override object Create(ActorInitializer init) { return new LuaScript(this); }
	}

	public class LuaScript : ITick, IWorldLoaded, INotifyActorDisposing
	{
		readonly LuaScriptInfo info;
		public ScriptContext Context;
		bool disposed;

		public LuaScript(LuaScriptInfo info)
		{
			this.info = info;
		}

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer worldRenderer)
		{
			var scripts = info.Scripts ?? Enumerable.Empty<string>();
			Context = new ScriptContext(world, worldRenderer, scripts);
			Context.WorldLoaded();
		}

		void ITick.Tick(Actor self)
		{
			Context.Tick();
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (disposed)
				return;

			Context?.Dispose();

			disposed = true;
		}

		public bool FatalErrorOccurred => Context.FatalErrorOccurred;
	}
}
