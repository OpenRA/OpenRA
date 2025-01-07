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
using Newtonsoft.Json;
using OpenRA.Mods.Common.UtilityCommands.Documentation.Objects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.UtilityCommands.Documentation
{
	sealed class ExtractTraitDocsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--docs";

		bool IUtilityCommand.ValidateArguments(string[] args) => true;

		[Desc("[VERSION]", "Generate trait documentation in JSON format.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = utility.ModData;

			var version = utility.ModData.Manifest.Metadata.Version;
			if (args.Length > 1)
				version = args[1];

			var objectCreator = utility.ModData.ObjectCreator;
			var traitInfos = objectCreator.GetTypesImplementing<TraitInfo>().OrderBy(t => t.Namespace).ThenBy(t => t.Name);

			var json = GenerateJson(version, traitInfos, objectCreator);
			Console.WriteLine(json);
		}

		static string GenerateJson(string version, IEnumerable<Type> traitTypes, ObjectCreator objectCreator)
		{
			var relatedEnumTypes = new HashSet<Type>();
			var pdbReaderCache = Utilities.CreatePdbReaderCache();

			var traitTypesInfo = traitTypes
				.Where(x => !x.ContainsGenericParameters && !x.IsAbstract)
				.Select(type =>
				{
					var fields = FieldLoader.GetTypeLoadInfo(type)
						.Where(fi => fi.Field.IsPublic && fi.Field.IsInitOnly && !fi.Field.IsStatic);

					return new ExtractedTraitInfo
					{
						Namespace = type.Namespace,
						Name = type.Name.EndsWith("Info", StringComparison.Ordinal) ? type.Name[..^4] : type.Name,
						Filename = Utilities.GetSourceFilenameFromPdb(type, pdbReaderCache),
						Description = string.Join(" ", type.GetCustomAttributes<DescAttribute>(false).SelectMany(d => d.Lines)),
						RequiresTraits = RequiredTraitTypes(type)
							.Select(y => y.Name),
						InheritedTypes = type.BaseTypes()
							.Select(y => y.Name)
							.Where(y => y != type.Name && y != $"{type.Name}Info" && y != "Object" && y != "TraitInfo`1"), // HACK: This is the simplest way to exclude TraitInfo<T>, which doesn't serialize well.
						Properties = DocumentationHelpers.GetClassFieldInfos(type, fields, relatedEnumTypes, objectCreator)
					};
				});

			var result = new
			{
				Version = version,
				TraitInfos = traitTypesInfo,
				RelatedEnums = DocumentationHelpers.GetRelatedEnumInfos(relatedEnumTypes)
			};

			return JsonConvert.SerializeObject(result);
		}

		static IEnumerable<Type> RequiredTraitTypes(Type t)
		{
			return t.GetInterfaces()
				.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(Requires<>))
				.SelectMany(i => i.GetGenericArguments())
				.Where(i => !i.IsInterface && !t.IsSubclassOf(i))
				.OrderBy(i => i.Name);
		}
	}
}
