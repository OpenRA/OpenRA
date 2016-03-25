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
		/// You can remove inherited traits by adding a - in front of them as in -TraitName: to inherit everything, but this trait.
		/// </summary>
		public readonly string Name;
		readonly TypeDictionary traits = new TypeDictionary();
		List<ITraitInfo> constructOrderCache = null;

		public ActorInfo(ObjectCreator creator, string name, MiniYaml node)
		{
			try
			{
				Name = name;

				var abstractActorType = name.StartsWith("^");
				foreach (var t in node.Nodes)
				{
					try
					{
						traits.Add(LoadTraitInfo(creator, t.Key.Split('@')[0], t.Value));
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

		public ActorInfo(string name, params ITraitInfo[] traitInfos)
		{
			Name = name;
			foreach (var t in traitInfos)
				traits.Add(t);
		}

		static ITraitInfo LoadTraitInfo(ObjectCreator creator, string traitName, MiniYaml my)
		{
			if (!string.IsNullOrEmpty(my.Value))
				throw new YamlException("Junk value `{0}` on trait node {1}"
				.F(my.Value, traitName));
			var info = creator.CreateObject<ITraitInfo>(traitName + "Info");
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

			var source = traits.WithInterface<ITraitInfo>().Select(i => new
			{
				Trait = i,
				Type = i.GetType(),
				Dependencies = PrerequisitesOf(i).ToList()
			}).ToList();

			var resolved = source.Where(s => !s.Dependencies.Any()).ToList();
			var unresolved = source.Except(resolved);

			var testResolve = new Func<Type, Type, bool>((a, b) => a == b || a.IsAssignableFrom(b));

			// This query detects which unresolved traits can be immediately resolved as all their direct dependencies are met.
			var more = unresolved.Where(u =>
				u.Dependencies.All(d => // To be resolvable, all dependencies must be satisfied according to the following conditions:
					resolved.Exists(r => testResolve(d, r.Type)) && // There must exist a resolved trait that meets the dependency.
					!unresolved.Any(u1 => testResolve(d, u1.Type)))); // All matching traits that meet this dependency must be resolved first.

			// Continue resolving traits as long as possible.
			// Each time we resolve some traits, this means dependencies for other traits may then be possible to satisfy in the next pass.
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

				throw new YamlException(exceptionString);
			}

			constructOrderCache = resolved.Select(r => r.Trait).ToList();
			return constructOrderCache;
		}

		public static IEnumerable<Type> PrerequisitesOf(ITraitInfo info)
		{
			return info
				.GetType()
				.GetInterfaces()
				.Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Requires<>))
				.Select(t => t.GetGenericArguments()[0]);
		}

		public IEnumerable<Pair<string, Type>> GetInitKeys()
		{
			var inits = traits.WithInterface<ITraitInfo>().SelectMany(
				t => t.GetType().GetInterfaces()
					.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(UsesInit<>))
					.Select(i => i.GetGenericArguments()[0])).ToList();

			inits.Add(typeof(OwnerInit));		/* not exposed by a trait; this is used by the Actor itself */

			return inits.Select(
				i => Pair.New(
					i.Name.Replace("Init", ""), i));
		}

		public bool HasTraitInfo<T>() where T : ITraitInfoInterface { return traits.Contains<T>(); }
		public T TraitInfo<T>() where T : ITraitInfoInterface { return traits.Get<T>(); }
		public T TraitInfoOrDefault<T>() where T : ITraitInfoInterface { return traits.GetOrDefault<T>(); }
		public IEnumerable<T> TraitInfos<T>() where T : ITraitInfoInterface { return traits.WithInterface<T>(); }
	}
}
