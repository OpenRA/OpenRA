#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA
{
	/// <summary>
	/// A unit/building inside the game. Every rules starts with one and adds trait to it.
	/// Special actors like world or player are usually defined in system.yaml and affect everything.
	/// </summary>
	public class ActorInfo
	{
		/// <summary>
		/// The actor name can be anything, but the sprites used in the Render*: traits default to this one.
		/// If you add an ^ in front of the name, the engine will recognize this as a collection of traits
		/// that can be inherited by others (using Inherits:) and not a real unit.
		/// You can remove inherited traits by adding a - infront of them as in -TraitName: to inherit everything, but this trait.
		/// </summary>
		public readonly string Name;
		public readonly TypeDictionary Traits = new TypeDictionary();
		List<ITraitInfo> constructOrderCache = null;

		public ActorInfo(string name, MiniYaml node, Dictionary<string, MiniYaml> allUnits)
		{
			try
			{
				var allParents = new HashSet<string>();

				// Guard against circular inheritance
				allParents.Add(name);
				var mergedNode = MergeWithParents(node, allUnits, allParents).ToDictionary();

				Name = name;

				foreach (var t in mergedNode)
				{
					if (t.Key[0] == '-')
						throw new YamlException("Bogus trait removal: " + t.Key);

					if (t.Key != "Inherits" && !t.Key.StartsWith("Inherits@"))
						Traits.Add(LoadTraitInfo(t.Key.Split('@')[0], t.Value));
				}
			}
			catch (YamlException e)
			{
				throw new YamlException("Actor type {0}: {1}".F(name, e.Message));
			}
		}

		static Dictionary<string, MiniYaml> GetParents(MiniYaml node, Dictionary<string, MiniYaml> allUnits)
		{
			return node.Nodes.Where(n => n.Key == "Inherits" || n.Key.StartsWith("Inherits@"))
				.ToDictionary(n => n.Value.Value, n =>
			{
				MiniYaml i;
					if (!allUnits.TryGetValue(n.Value.Value, out i))
						throw new YamlException(
							"Bogus inheritance -- parent type {0} does not exist".F(n.Value.Value));

				return i;
			});
		}

		static MiniYaml MergeWithParents(MiniYaml node, Dictionary<string, MiniYaml> allUnits, HashSet<string> allParents)
		{
			var parents = GetParents(node, allUnits);

			foreach (var kv in parents)
			{
				if (!allParents.Add(kv.Key))
					throw new YamlException(
						"Bogus inheritance -- duplicate inheritance of {0}.".F(kv.Key));

				node = MiniYaml.MergeStrict(node, MergeWithParents(kv.Value, allUnits, allParents));
			}

			return node;
		}

		static ITraitInfo LoadTraitInfo(string traitName, MiniYaml my)
		{
			if (!string.IsNullOrEmpty(my.Value))
				throw new YamlException("Junk value `{0}` on trait node {1}"
				.F(my.Value, traitName));
			var info = Game.CreateObject<ITraitInfo>(traitName + "Info");
			FieldLoader.Load(info, my);
			return info;
		}

		public IEnumerable<ITraitInfo> TraitsInConstructOrder()
		{
			if (constructOrderCache != null)
				return constructOrderCache;

			var source = Traits.WithInterface<ITraitInfo>().Select(i => new
			{
				Trait = i,
				Type = i.GetType(),
				Dependencies = PrerequisitesOf(i).ToList()
			}).ToList();

			var resolved = source.Where(s => !s.Dependencies.Any()).ToList();
			var unresolved = source.Except(resolved);

			var testResolve = new Func<Type, Type, bool>((a, b) => a == b || a.IsAssignableFrom(b));
			var more = unresolved.Where(u => u.Dependencies.All(d => resolved.Exists(r => testResolve(d, r.Type))));

			// Re-evaluate the vars above until sorted
			while (more.Any())
				resolved.AddRange(more);

			if (unresolved.Any())
			{
				var exceptionString = "ActorInfo(\"" + Name + "\") failed to initialize because of the following:\r\n";
				var missing = unresolved.SelectMany(u => u.Dependencies.Where(d => !source.Any(s => testResolve(d, s.Type)))).Distinct();

				exceptionString += "Missing:\r\n";
				foreach (var m in missing)
					exceptionString += m + " \r\n";

				exceptionString += "Unresolved:\r\n";
				foreach (var u in unresolved)
				{
					var deps = u.Dependencies.Where(d => !resolved.Exists(r => r.Type == d));
					exceptionString += u.Type + ": { " + string.Join(", ", deps) + " }\r\n";
				}

				throw new Exception(exceptionString);
			}

			constructOrderCache = resolved.Select(r => r.Trait).ToList();
			return constructOrderCache;
		}

		static IEnumerable<Type> PrerequisitesOf(ITraitInfo info)
		{
			return info
				.GetType()
				.GetInterfaces()
				.Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Requires<>))
				.Select(t => t.GetGenericArguments()[0]);
		}

		public IEnumerable<Pair<string, Type>> GetInitKeys()
		{
			var inits = Traits.WithInterface<ITraitInfo>().SelectMany(
				t => t.GetType().GetInterfaces()
					.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(UsesInit<>))
					.Select(i => i.GetGenericArguments()[0])).ToList();

			inits.Add(typeof(OwnerInit));		/* not exposed by a trait; this is used by the Actor itself */

			return inits.Select(
				i => Pair.New(
					i.Name.Replace("Init", ""), i));
		}
	}
}
