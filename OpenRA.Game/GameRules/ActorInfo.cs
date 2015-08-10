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
				var abstractActorType = name.StartsWith("^");

				// Guard against circular inheritance
				allParents.Add(name);
				var mergedNode = MergeWithParents(node, allUnits, allParents).ToDictionary();

				Name = name;

				foreach (var t in mergedNode)
				{
					if (t.Key[0] == '-')
						throw new YamlException("Bogus trait removal: " + t.Key);

					if (t.Key != "Inherits" && !t.Key.StartsWith("Inherits@"))
						try
						{
							Traits.Add(LoadTraitInfo(t.Key.Split('@')[0], t.Value));
						}
						catch (FieldLoader.MissingFieldsException e)
						{
							if (!abstractActorType)
								throw new YamlException(e.Message);
						}
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
			try
			{
				FieldLoader.Load(info, my);
			}
			catch (FieldLoader.MissingFieldsException e)
			{
				var header = "Trait name " + traitName + ": " + (e.Missing.Length > 1 ? "Required properties missing" : "Required property missing");
				throw new FieldLoader.MissingFieldsException(e.Missing, header);
			}

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
				Predecessors = PredecessorsOf(i).ToList(),
				Requisites = RequisitesOf(i).ToList()
			}).ToList();

			var resolved = source.Where(s => !s.Predecessors.Any()).ToList();
			var unresolved = source.Except(resolved).ToList();

			var testResolve = new Func<Type, Type, bool>((a, b) => a == b || a.IsAssignableFrom(b));

			// When referenced, evaluates to those in unresolved with all present predecessors in resolved
			var enumeratedResolvables = unresolved.Where(u => u.Predecessors.All(d => !unresolved.Any(r => testResolve(d, r.Type))));
			var resolvables = enumeratedResolvables.ToList();

			while (resolvables.Any())
			{
				unresolved.RemoveAll(resolvables.Contains);
				resolved.AddRange(resolvables);
				resolvables.Clear();
				resolvables.AddRange(enumeratedResolvables);
			}

			var missing = source.SelectMany(u => u.Requisites.Where(d => !source.Any(s => testResolve(d, s.Type)))).Distinct();
			if (missing.Any() || unresolved.Any())
			{
				var exceptionString = "ActorInfo(\"" + Name + "\") failed to initialize because of the following:\r\n";

				if (missing.Any())
				{
					exceptionString += "Missing:\r\n";
					foreach (var m in missing)
					{
						var users = source.Where(s => s.Requisites.Any(r => testResolve(r, m))).Select(s => s.Type);
						exceptionString += m + ": { " + string.Join(", ", users) + " }\r\n";
					}
				}

				if (unresolved.Any())
				{
					exceptionString += "Unresolved:\r\n";
					foreach (var u in unresolved)
					{
						var deps = u.Predecessors.Where(d => !resolved.Exists(r => r.Type == d));
						exceptionString += u.Type + ": { " + string.Join(", ", deps) + " }\r\n";
					}
				}

				throw new Exception(exceptionString);
			}

			constructOrderCache = resolved.Select(r => r.Trait).ToList();
			return constructOrderCache;
		}

		static IEnumerable<Type> PredecessorsOf(ITraitInfo info)
		{
			return info
				.GetType()
				.GetInterfaces()
				.Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(InitializeAfter<>))
				.Select(t => t.GetGenericArguments()[0]);
		}

		static IEnumerable<Type> RequisitesOf(ITraitInfo info)
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

		public bool TraitInfosAny<T>() where T : ITraitInfo { return Traits.Contains<T>(); }
		public T TraitInfo<T>() where T : ITraitInfo { return Traits.Get<T>(); }
		public T TraitInfoOrDefault<T>() where T : ITraitInfo { return Traits.GetOrDefault<T>(); }
	}
}
