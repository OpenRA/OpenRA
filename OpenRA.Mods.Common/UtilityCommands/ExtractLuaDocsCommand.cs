#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class ExtractLuaDocsCommand : IUtilityCommand
	{
		public string Name { get { return "--lua-docs"; } }

		public bool ValidateArguments(string[] args)
		{
			return true;
		}

		[Desc("Generate Lua API documentation in MarkDown format.")]
		public void Run(ModData modData, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = modData;

			Console.WriteLine("This is an automatically generated listing of the new Lua map scripting API, generated for {0} of OpenRA.", Game.ModData.Manifest.Mod.Version);
			Console.WriteLine();
			Console.WriteLine("OpenRA allows custom maps and missions to be scripted using Lua 5.1.\n" +
				"These scripts run in a sandbox that prevents access to unsafe functions (e.g. OS or file access), " +
				"and limits the memory and CPU usage of the scripts.");
			Console.WriteLine();
			Console.WriteLine("You can access this interface by adding the [LuaScript](Traits#luascript) trait to the world actor in your map rules " +
				"(note, you must replace the spaces in the snippet below with a single tab for each level of indentation):");
			Console.WriteLine("```\nRules:\n\tWorld:\n\t\tLuaScript:\n\t\t\tScripts: myscript.lua\n```");
			Console.WriteLine();
			Console.WriteLine("Map scripts can interact with the game engine in three ways:\n" +
				"* Global tables provide functions for interacting with the global world state, or performing general helper tasks.\n" +
				"They exist in the global namespace, and can be called directly using ```<table name>.<function name>```.\n" +
				"* Individual actors expose a collection of properties and commands that query information or modify their state.\n" +
				"  * Some commands, marked as <em>queued activity</em>, are asynchronous. Activities are queued on the actor, and will run in " +
				"sequence until the queue is empty or the Stop command is called. Actors that are not performing an activity are Idle " +
				"(actor.IsIdle will return true). The properties and commands available on each actor depends on the traits that the actor " +
				"specifies in its rule definitions.\n" +
				"* Individual players expose a collection of properties and commands that query information or modify their state.\n" +
				"The properties and commands available on each actor depends on the traits that the actor specifies in its rule definitions.\n");
			Console.WriteLine();
			Console.WriteLine("For a basic guide about map scripts see the [`Map Scripting` wiki page](https://github.com/OpenRA/OpenRA/wiki/Map-scripting).");
			Console.WriteLine();

			var tables = Game.ModData.ObjectCreator.GetTypesImplementing<ScriptGlobal>()
				.OrderBy(t => t.Name);

			Console.WriteLine("<h3>Global Tables</h3>");

			foreach (var t in tables)
			{
				var name = t.GetCustomAttributes<ScriptGlobalAttribute>(true).First().Name;
				var members = ScriptMemberWrapper.WrappableMembers(t);

				Console.WriteLine("<table align=\"center\" width=\"1024\"><tr><th colspan=\"2\" width=\"1024\">{0}</th></tr>", name);
				foreach (var m in members.OrderBy(m => m.Name))
				{
					var desc = m.HasAttribute<DescAttribute>() ? m.GetCustomAttributes<DescAttribute>(true).First().Lines.JoinWith("\n") : "";
					Console.WriteLine("<tr><td align=\"right\" width=\"50%\"><strong>{0}</strong></td><td>{1}</td></tr>".F(m.LuaDocString(), desc));
				}

				Console.WriteLine("</table>");
			}

			Console.WriteLine("<h3>Actor Properties / Commands</h3>");

			var actorCategories = Game.ModData.ObjectCreator.GetTypesImplementing<ScriptActorProperties>().SelectMany(cg =>
			{
				var catAttr = cg.GetCustomAttributes<ScriptPropertyGroupAttribute>(false).FirstOrDefault();
				var category = catAttr != null ? catAttr.Category : "Unsorted";

				var required = RequiredTraitNames(cg);
				return ScriptMemberWrapper.WrappableMembers(cg).Select(mi => Tuple.Create(category, mi, required));
			}).GroupBy(g => g.Item1).OrderBy(g => g.Key);

			foreach (var kv in actorCategories)
			{
				Console.WriteLine("<table align=\"center\" width=\"1024\"><tr><th colspan=\"2\" width=\"1024\">{0}</th></tr>", kv.Key);

				foreach (var property in kv.OrderBy(p => p.Item2.Name))
				{
					var mi = property.Item2;
					var required = property.Item3;
					var hasDesc = mi.HasAttribute<DescAttribute>();
					var hasRequires = required.Any();
					var isActivity = mi.HasAttribute<ScriptActorPropertyActivityAttribute>();

					Console.WriteLine("<tr><td width=\"50%\" align=\"right\"><strong>{0}</strong>", mi.LuaDocString());

					if (isActivity)
						Console.WriteLine("<br /><em>Queued Activity</em>");

					Console.WriteLine("</td><td>");

					if (hasDesc)
						Console.WriteLine(mi.GetCustomAttributes<DescAttribute>(false).First().Lines.JoinWith("\n"));

					if (hasDesc && hasRequires)
						Console.WriteLine("<br />");

					if (hasRequires)
						Console.WriteLine("<b>Requires {1}:</b> {0}".F(required.JoinWith(", "), required.Length == 1 ? "Trait" : "Traits"));

					Console.WriteLine("</td></tr>");
				}

				Console.WriteLine("</table>");
			}

			Console.WriteLine("<h3>Player Properties / Commands</h3>");

			var playerCategories = Game.ModData.ObjectCreator.GetTypesImplementing<ScriptPlayerProperties>().SelectMany(cg =>
			{
				var catAttr = cg.GetCustomAttributes<ScriptPropertyGroupAttribute>(false).FirstOrDefault();
				var category = catAttr != null ? catAttr.Category : "Unsorted";

				var required = RequiredTraitNames(cg);
				return ScriptMemberWrapper.WrappableMembers(cg).Select(mi => Tuple.Create(category, mi, required));
			}).GroupBy(g => g.Item1).OrderBy(g => g.Key);

			foreach (var kv in playerCategories)
			{
				Console.WriteLine("<table align=\"center\" width=\"1024\"><tr><th colspan=\"2\" width=\"1024\">{0}</th></tr>", kv.Key);

				foreach (var property in kv.OrderBy(p => p.Item2.Name))
				{
					var mi = property.Item2;
					var required = property.Item3;
					var hasDesc = mi.HasAttribute<DescAttribute>();
					var hasRequires = required.Any();
					var isActivity = mi.HasAttribute<ScriptActorPropertyActivityAttribute>();

					Console.WriteLine("<tr><td width=\"50%\" align=\"right\"><strong>{0}</strong>", mi.LuaDocString());

					if (isActivity)
						Console.WriteLine("<br /><em>Queued Activity</em>");

					Console.WriteLine("</td><td>");

					if (hasDesc)
						Console.WriteLine(mi.GetCustomAttributes<DescAttribute>(false).First().Lines.JoinWith("\n"));

					if (hasDesc && hasRequires)
						Console.WriteLine("<br />");

					if (hasRequires)
						Console.WriteLine("<b>Requires {1}:</b> {0}".F(required.JoinWith(", "), required.Length == 1 ? "Trait" : "Traits"));

					Console.WriteLine("</td></tr>");
				}

				Console.WriteLine("</table>");
			}
		}

		static string[] RequiredTraitNames(Type t)
		{
			// Returns the inner types of all the Requires<T> interfaces on this type
			var outer = t.GetInterfaces()
				.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(Requires<>));

			// Get the inner types
			var inner = outer.SelectMany(i => i.GetGenericArguments()).ToArray();

			// Remove the namespace and the trailing "Info"
			return inner.Select(i => i.Name.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault())
				.Select(s => s.EndsWith("Info") ? s.Remove(s.Length - 4, 4) : s)
				.ToArray();
		}
	}
}
