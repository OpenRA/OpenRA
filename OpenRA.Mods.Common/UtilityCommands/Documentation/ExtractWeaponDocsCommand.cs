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
using OpenRA.GameRules;
using OpenRA.Mods.Common.UtilityCommands.Documentation.Objects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.UtilityCommands.Documentation
{
	sealed class ExtractWeaponDocsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--weapon-docs";

		bool IUtilityCommand.ValidateArguments(string[] args) => true;

		[Desc("[VERSION]", "Generate weaponry documentation in JSON format.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = utility.ModData;

			var version = utility.ModData.Manifest.Metadata.Version;
			if (args.Length > 1)
				version = args[1];

			var objectCreator = utility.ModData.ObjectCreator;
			var weaponInfo = new[] { typeof(WeaponInfo) };
			var warheads = objectCreator.GetTypesImplementing<IWarhead>().OrderBy(t => t.Namespace).ThenBy(t => t.Name);
			var projectiles = objectCreator.GetTypesImplementing<IProjectileInfo>().OrderBy(t => t.Namespace).ThenBy(t => t.Name);

			var weaponTypes = weaponInfo.Concat(projectiles).Concat(warheads);

			var json = GenerateJson(version, weaponTypes, objectCreator);
			Console.WriteLine(json);
		}

		static string GenerateJson(string version, IEnumerable<Type> weaponTypes, ObjectCreator objectCreator)
		{
			var relatedEnumTypes = new HashSet<Type>();
			var pdbReaderCache = Utilities.CreatePdbReaderCache();

			var weaponTypesInfo = weaponTypes
				.Where(x => !x.ContainsGenericParameters && !x.IsAbstract)
				.Select(type =>
				{
					var fields = FieldLoader.GetTypeLoadInfo(type)
						.Where(fi => fi.Field.IsPublic && fi.Field.IsInitOnly && !fi.Field.IsStatic);

					return new ExtractedClassInfo
					{
						Namespace = type.Namespace,
						Name = type.Name.EndsWith("Info", StringComparison.Ordinal) ? type.Name[..^4] : type.Name,
						Filename = Utilities.GetSourceFilenameFromPdb(type, pdbReaderCache),
						Description = string.Join(" ", type.GetCustomAttributes<DescAttribute>(false).SelectMany(d => d.Lines)),
						InheritedTypes = type.BaseTypes()
							.Select(y => y.Name)
							.Where(y => y != type.Name && y != $"{type.Name}Info" && y != "Object"),
						Properties = DocumentationHelpers.GetClassFieldInfos(type, fields, relatedEnumTypes, objectCreator)
					};
				});

			var result = new
			{
				Version = version,
				WeaponTypes = weaponTypesInfo,
				RelatedEnums = DocumentationHelpers.GetRelatedEnumInfos(relatedEnumTypes)
			};

			return JsonConvert.SerializeObject(result);
		}
	}
}
