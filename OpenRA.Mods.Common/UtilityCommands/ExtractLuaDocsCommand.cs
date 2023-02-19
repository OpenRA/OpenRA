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
using System.Linq;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class ExtractLuaDocsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--lua-docs";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return true;
		}

		[Desc("Generate Lua API documentation in MarkDown format.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = utility.ModData;

			var version = utility.ModData.Manifest.Metadata.Version;
			if (args.Length > 1)
				version = args[1];

			Console.WriteLine("This is an automatically generated listing of the Lua map scripting API for version {0} of OpenRA.", version);
			Console.WriteLine();
			Console.WriteLine("OpenRA allows custom maps and missions to be scripted using Lua 5.1.");
			Console.WriteLine("These scripts run in a sandbox that prevents access to unsafe functions (e.g. OS or file access), " +
				"and limits the memory and CPU usage of the scripts.");
			Console.WriteLine();
			Console.WriteLine("You can access this interface by adding the [LuaScript](../traits/#luascript) trait to the world actor in your map rules " +
				"(note, you must replace the spaces in the snippet below with a single tab for each level of indentation):");
			Console.WriteLine("```\nRules:\n\tWorld:\n\t\tLuaScript:\n\t\t\tScripts: myscript.lua\n```");
			Console.WriteLine();
			Console.WriteLine("Map scripts can interact with the game engine in three ways:");
			Console.WriteLine();
			Console.WriteLine("* Global tables provide functions for interacting with the global world state, or performing general helper tasks.");
			Console.WriteLine("They exist in the global namespace, and can be called directly using ```<table name>.<function name>```.");
			Console.WriteLine("* Individual actors expose a collection of properties and commands that query information or modify their state.");
			Console.WriteLine("  * Some commands, marked as <em>queued activity</em>, are asynchronous. Activities are queued on the actor, and will run in " +
				"sequence until the queue is empty or the Stop command is called. Actors that are not performing an activity are Idle " +
				"(actor.IsIdle will return true). The properties and commands available on each actor depends on the traits that the actor " +
				"specifies in its rule definitions.");
			Console.WriteLine("* Individual players expose a collection of properties and commands that query information or modify their state.");
			Console.WriteLine("The properties and commands available on each actor depends on the traits that the actor specifies in its rule definitions.");
			Console.WriteLine();
			Console.WriteLine("For a basic guide about map scripts see the [`Map Scripting` wiki page](https://github.com/OpenRA/OpenRA/wiki/Map-scripting).");
			Console.WriteLine();

			var tables = utility.ModData.ObjectCreator.GetTypesImplementing<ScriptGlobal>()
				.OrderBy(t => t.Name);

			Console.WriteLine("## Global Tables");

			foreach (var t in tables)
			{
				var name = t.GetCustomAttributes<ScriptGlobalAttribute>(true).First().Name;
				var members = ScriptMemberWrapper.WrappableMembers(t);

				Console.WriteLine();
				Console.WriteLine("### " + name);
				Console.WriteLine();
				Console.WriteLine("| Function | Description |");
				Console.WriteLine("|---------:|-------------|");
				foreach (var m in members.OrderBy(m => m.Name))
				{
					var desc = m.HasAttribute<DescAttribute>() ? m.GetCustomAttributes<DescAttribute>(true).First().Lines.JoinWith("<br />") : "";
					Console.WriteLine($"| **{m.LuaDocString()}** | {desc} |");
				}
			}

			Console.WriteLine();

			Console.WriteLine("## Actor Properties / Commands");

			var actorCategories = utility.ModData.ObjectCreator.GetTypesImplementing<ScriptActorProperties>().SelectMany(cg =>
			{
				var catAttr = cg.GetCustomAttributes<ScriptPropertyGroupAttribute>(false).FirstOrDefault();
				var category = catAttr != null ? catAttr.Category : "Unsorted";

				var required = ScriptMemberWrapper.RequiredTraitNames(cg);
				return ScriptMemberWrapper.WrappableMembers(cg).Select(mi => (category, mi, required));
			}).GroupBy(g => g.category).OrderBy(g => g.Key);

			foreach (var kv in actorCategories)
			{
				Console.WriteLine();
				Console.WriteLine("### " + kv.Key);
				Console.WriteLine();
				Console.WriteLine("| Function | Description |");
				Console.WriteLine("|---------:|-------------|");

				foreach (var property in kv.OrderBy(p => p.mi.Name))
				{
					var mi = property.mi;
					var required = property.required;
					var hasDesc = mi.HasAttribute<DescAttribute>();
					var hasRequires = required.Length > 0;
					var isActivity = mi.HasAttribute<ScriptActorPropertyActivityAttribute>();

					Console.Write($"| **{mi.LuaDocString()}**");

					if (isActivity)
						Console.Write("<br />*Queued Activity*");

					Console.Write(" | ");

					if (hasDesc)
						Console.Write(mi.GetCustomAttributes<DescAttribute>(false).First().Lines.JoinWith("<br />"));

					if (hasDesc && hasRequires)
						Console.Write("<br />");

					if (hasRequires)
						Console.Write($"**Requires {(required.Length == 1 ? "Trait" : "Traits")}:** {required.JoinWith(", ")}");

					Console.WriteLine(" |");
				}
			}

			Console.WriteLine();

			Console.WriteLine("## Player Properties / Commands");

			var playerCategories = utility.ModData.ObjectCreator.GetTypesImplementing<ScriptPlayerProperties>().SelectMany(cg =>
			{
				var catAttr = cg.GetCustomAttributes<ScriptPropertyGroupAttribute>(false).FirstOrDefault();
				var category = catAttr != null ? catAttr.Category : "Unsorted";

				var required = ScriptMemberWrapper.RequiredTraitNames(cg);
				return ScriptMemberWrapper.WrappableMembers(cg).Select(mi => (category, mi, required));
			}).GroupBy(g => g.category).OrderBy(g => g.Key);

			foreach (var kv in playerCategories)
			{
				Console.WriteLine();
				Console.WriteLine("### " + kv.Key);
				Console.WriteLine();
				Console.WriteLine("| Function | Description |");
				Console.WriteLine("|---------:|-------------|");

				foreach (var property in kv.OrderBy(p => p.mi.Name))
				{
					var mi = property.mi;
					var required = property.required;
					var hasDesc = mi.HasAttribute<DescAttribute>();
					var hasRequires = required.Length > 0;
					var isActivity = mi.HasAttribute<ScriptActorPropertyActivityAttribute>();

					Console.Write($"| **{mi.LuaDocString()}**");

					if (isActivity)
						Console.Write("<br />*Queued Activity*");

					Console.Write(" | ");

					if (hasDesc)
						Console.Write(mi.GetCustomAttributes<DescAttribute>(false).First().Lines.JoinWith("<br />"));

					if (hasDesc && hasRequires)
						Console.Write("<br />");

					if (hasRequires)
						Console.Write($"**Requires {(required.Length == 1 ? "Trait" : "Traits")}:** {required.JoinWith(", ")}");

					Console.WriteLine(" |");
				}

				Console.WriteLine();
			}
		}
	}
}
