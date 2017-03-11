#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using Eluant;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Scripting
{
	[ScriptPropertyGroup("Support Powers")]
	public class ChronsphereProperties : ScriptActorProperties, Requires<ChronoshiftPowerInfo>
	{
		public ChronsphereProperties(ScriptContext context, Actor self)
			: base(context, self) { }

		[ScriptContext(ScriptContextType.Mission)]
		[Desc("Chronoshift a group of actors. A duration of 0 will teleport the actors permanently.")]
		public void Chronoshift(LuaTable unitLocationPairs, int duration = 0, bool killCargo = false)
		{
			foreach (var kv in unitLocationPairs)
			{
				Actor actor;
				CPos cell;
				using (kv.Key)
				using (kv.Value)
				{
					if (!kv.Key.TryGetClrValue(out actor) || !kv.Value.TryGetClrValue(out cell))
						throw new LuaException("Chronoshift requires a table of Actor,CPos pairs. Received {0},{1}".F(
							kv.Key.WrappedClrType().Name, kv.Value.WrappedClrType().Name));
				}

				var cs = actor.TraitOrDefault<Chronoshiftable>();
				if (cs != null && cs.CanChronoshiftTo(actor, cell))
					cs.Teleport(actor, cell, duration, killCargo, Self);
			}
		}
	}
}