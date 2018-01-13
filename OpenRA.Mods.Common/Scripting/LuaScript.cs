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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[Desc("Part of the new Lua API.")]
	public class LuaScriptInfo : ConditionalTraitInfo, ITraitInfo, Requires<SpawnMapActorsInfo>
	{
		public readonly HashSet<string> Scripts = new HashSet<string>();

		public override object Create(ActorInitializer init) { return new LuaScript(this); }
	}

	public class LuaScript : ConditionalTrait<LuaScriptInfo>, ITick, IWorldLoaded, INotifyActorDisposing
	{
		readonly LuaScriptInfo info;
		ScriptContext context;
		bool disposed;
		bool initialized;

		public LuaScript(LuaScriptInfo info)
			: base(info)
		{
			this.info = info;
		}

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer worldRenderer)
		{
			var scripts = IsTraitDisabled ? Enumerable.Empty<string>() : info.Scripts ?? Enumerable.Empty<string>();
			context = new ScriptContext(world, worldRenderer, scripts);
			if (!IsTraitDisabled)
				context.WorldLoaded();
		}

		void ITick.Tick(Actor self)
		{
			if (!IsTraitDisabled)
				context.Tick(self);
		}

		protected override void TraitEnabled(Actor self)
		{
			if (initialized)
				throw new InvalidOperationException("Enabling Lua scripts mid-game is not supported.");
			initialized = true;
		}

		protected override void TraitDisabled(Actor self)
		{
			if (initialized)
				throw new InvalidOperationException("Disabling Lua scripts mid-game is not supported.");
			initialized = true;
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
