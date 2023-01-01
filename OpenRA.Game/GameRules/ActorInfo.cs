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
		public const string AbstractActorPrefix = "^";
		public const char TraitInstanceSeparator = '@';

		/// <summary>
		/// The actor name can be anything, but the sprites used in the Render*: traits default to this one.
		/// If you add an ^ in front of the name, the engine will recognize this as a collection of traits
		/// that can be inherited by others (using Inherits:) and not a real unit.
		/// You can remove inherited traits by adding a - in front of them as in -TraitName: to inherit everything, but this trait.
		/// </summary>
		public readonly string Name;
		readonly TypeDictionary traits = new TypeDictionary();
		List<TraitInfo> constructOrderCache = null;

		public ActorInfo(ObjectCreator creator, string name, MiniYaml node)
		{
			try
			{
				Name = name;

				foreach (var t in node.Nodes)
				{
					try
					{
						// HACK: The linter does not want to crash when a trait doesn't exist but only print an error instead
						// LoadTraitInfo will only return null to signal us to abort here if the linter is running
						var trait = LoadTraitInfo(creator, t.Key, t.Value);
						if (trait != null)
							traits.Add(trait);
					}
					catch (FieldLoader.MissingFieldsException e)
					{
						throw new YamlException(e.Message);
					}
				}

				traits.TrimExcess();
			}
			catch (YamlException e)
			{
				throw new YamlException($"Actor type {name}: {e.Message}");
			}
		}

		public ActorInfo(string name, params TraitInfo[] traitInfos)
		{
			Name = name;
			foreach (var t in traitInfos)
				traits.Add(t);
			traits.TrimExcess();
		}

		static TraitInfo LoadTraitInfo(ObjectCreator creator, string traitName, MiniYaml my)
		{
			if (!string.IsNullOrEmpty(my.Value))
				throw new YamlException($"Junk value `{my.Value}` on trait node {traitName}");

			// HACK: The linter does not want to crash when a trait doesn't exist but only print an error instead
			// ObjectCreator will only return null to signal us to abort here if the linter is running
			var traitInstance = traitName.Split(TraitInstanceSeparator);
			var info = creator.CreateObject<TraitInfo>(traitInstance[0] + "Info");
			if (info == null)
				return null;

			try
			{
				if (traitInstance.Length > 1)
					info.GetType().GetField(nameof(info.InstanceName)).SetValue(info, traitInstance[1]);

				FieldLoader.Load(info, my);
			}
			catch (FieldLoader.MissingFieldsException e)
			{
				var header = "Trait name " + traitName + ": " + (e.Missing.Length > 1 ? "Required properties missing" : "Required property missing");
				throw new FieldLoader.MissingFieldsException(e.Missing, header);
			}

			return info;
		}

		public IEnumerable<TraitInfo> TraitsInConstructOrder()
		{
			if (constructOrderCache != null)
				return constructOrderCache;

			var source = traits.WithInterface<TraitInfo>().Select(i => new
			{
				Trait = i,
				Type = i.GetType(),
				Dependencies = PrerequisitesOf(i).ToList(),
				OptionalDependencies = OptionalPrerequisitesOf(i).ToList()
			}).ToList();

			var resolved = source.Where(s => s.Dependencies.Count == 0 && s.OptionalDependencies.Count == 0).ToList();
			var unresolved = source.Except(resolved);

			var testResolve = new Func<Type, Type, bool>((a, b) => a == b || a.IsAssignableFrom(b));

			// This query detects which unresolved traits can be immediately resolved as all their direct dependencies are met.
			var more = unresolved.Where(u =>
				u.Dependencies.All(d => // To be resolvable, all dependencies must be satisfied according to the following conditions:
					resolved.Exists(r => testResolve(d, r.Type)) && // There must exist a resolved trait that meets the dependency.
					!unresolved.Any(u1 => testResolve(d, u1.Type))) && // All matching traits that meet this dependency must be resolved first.
				u.OptionalDependencies.All(d => // To be resolvable, all optional dependencies must be satisfied according to the following condition:
					!unresolved.Any(u1 => testResolve(d, u1.Type)))); // All matching traits that meet this optional dependencies must be resolved first.

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
					var optDeps = u.OptionalDependencies.Where(d => !resolved.Exists(r => r.Type == d));
					var allDeps = string.Join(", ", deps.Select(o => o.ToString()).Concat(optDeps.Select(o => $"[{o}]")));
					exceptionString += $"{u.Type}: {{ {allDeps} }}\r\n";
				}

				throw new YamlException(exceptionString);
			}

			constructOrderCache = resolved.Select(r => r.Trait).ToList();
			return constructOrderCache;
		}

		public static IEnumerable<Type> PrerequisitesOf(TraitInfo info)
		{
			return info
				.GetType()
				.GetInterfaces()
				.Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Requires<>))
				.Select(t => t.GetGenericArguments()[0]);
		}

		public static IEnumerable<Type> OptionalPrerequisitesOf(TraitInfo info)
		{
			return info
				.GetType()
				.GetInterfaces()
				.Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(NotBefore<>))
				.Select(t => t.GetGenericArguments()[0]);
		}

		public bool HasTraitInfo<T>() where T : ITraitInfoInterface { return traits.Contains<T>(); }
		public T TraitInfo<T>() where T : ITraitInfoInterface { return traits.Get<T>(); }
		public T TraitInfoOrDefault<T>() where T : ITraitInfoInterface { return traits.GetOrDefault<T>(); }
		public IEnumerable<T> TraitInfos<T>() where T : ITraitInfoInterface { return traits.WithInterface<T>(); }

		public BitSet<TargetableType> GetAllTargetTypes()
		{
			// PERF: Avoid LINQ.
			var targetTypes = default(BitSet<TargetableType>);
			foreach (var targetable in TraitInfos<ITargetableInfo>())
				targetTypes = targetTypes.Union(targetable.GetTargetTypes());
			return targetTypes;
		}
	}
}
