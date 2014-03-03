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
using System.Linq;
using System.Reflection;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class CheckSyncAnnotations : ILintPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Map map)
		{
			/* first, check all the types implementing ISync */
			foreach (var t in Game.modData.ObjectCreator.GetTypesImplementing<ISync>())
				if (!HasAnySyncFields(t))
					emitWarning("{0} has ISync but nothing marked with [Sync]".F(t.Name));
		}

		bool HasAnySyncFields(Type t)
		{
			var flags = BindingFlags.Public | BindingFlags.NonPublic
				| BindingFlags.Instance;

			var fs = t.GetFields(flags);
			var ps = t.GetProperties(flags);

			return fs.Any(f => f.HasAttribute<SyncAttribute>()) ||
				ps.Any(p => p.HasAttribute<SyncAttribute>()) ||
				HasAnySyncFields(t.BaseType);
		}
	}
}
