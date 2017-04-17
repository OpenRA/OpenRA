#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.AS.Traits;
using OpenRA.Scripting;

namespace OpenRA.Mods.AS.Scripting
{
	[ScriptGlobal("ActorTag")]
	public class ActorTagGlobal : ScriptGlobal
	{
		public ActorTagGlobal(ScriptContext context) : base(context) { }

		[Desc("Returns all actor types which has the specified actorTag string in their actorTag trait.")]
		public string[] ReturnActorTypes(string actorTag)
		{
			return Context.World.Map.Rules.Actors.Values.Where(a => a.HasTraitInfo<ActorTagInfo>() && !a.Name.StartsWith("^")
				&& a.TraitInfos<ActorTagInfo>().Any(c => c.Type.Contains(actorTag))).Select(a => a.Name).ToArray();
		}
	}
}
