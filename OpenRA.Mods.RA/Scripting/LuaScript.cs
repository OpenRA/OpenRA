#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Scripting
{
	[Desc("Part of the new Lua API.")]
	public class LuaScriptInfo : ITraitInfo, Requires<SpawnMapActorsInfo>
	{
		public readonly string[] Scripts = { };

		public object Create(ActorInitializer init) { return new LuaScript(this); }
	}

	public sealed class LuaScript : ITick, IWorldLoaded, IDisposable
	{
		readonly LuaScriptInfo info;
		ScriptContext context;

		public LuaScript(LuaScriptInfo info)
		{
			this.info = info;
		}

		public void WorldLoaded(World world, WorldRenderer worldRenderer)
		{
			var scripts = info.Scripts ?? new string[0];
			context = new ScriptContext(world, worldRenderer, scripts);
			context.WorldLoaded();
		}

		public void Tick(Actor self)
		{
			context.Tick(self);
		}

		public void Dispose()
		{
			if (context != null)
				context.Dispose();
		}

		public bool FatalErrorOccurred { get { return context.FatalErrorOccurred; } }
	}
}
