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
using System.Globalization;
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

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return true;
		}

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
				.Select(type => new ExtractedClassInfo
				{
					Namespace = type.Namespace,
					Name = type.Name.EndsWith("Info", StringComparison.Ordinal) ? type.Name[..^4] : type.Name,
					Description = string.Join(" ", Utility.GetCustomAttributes<DescAttribute>(type, false).SelectMany(d => d.Lines)),
					Filename = Utilities.GetSourceFilenameFromPdb(type, pdbReaderCache),
					InheritedTypes = type.BaseTypes()
						.Select(y => y.Name)
						.Where(y => y != type.Name && y != $"{type.Name}Info" && y != "Object"),
					Properties = FieldLoader.GetTypeLoadInfo(type)
						.Where(fi => fi.Field.IsPublic && fi.Field.IsInitOnly && !fi.Field.IsStatic)
						.Select(fi =>
						{
							if (fi.Field.FieldType.IsEnum)
								relatedEnumTypes.Add(fi.Field.FieldType);

							return new ExtractedClassFieldInfo
							{
								PropertyName = fi.YamlName,
								DefaultValue = FieldSaver.SaveField(objectCreator.CreateBasic(type), fi.Field.Name).Value.Value,
								InternalType = Util.InternalTypeName(fi.Field.FieldType),
								UserFriendlyType = Util.FriendlyTypeName(fi.Field.FieldType),
								Description = string.Join(" ", Utility.GetCustomAttributes<DescAttribute>(fi.Field, true).SelectMany(d => d.Lines)),
								OtherAttributes = fi.Field.CustomAttributes
									.Where(a => a.AttributeType.Name != nameof(DescAttribute) && a.AttributeType.Name != nameof(FieldLoader.LoadUsingAttribute))
									.Select(a =>
									{
										var name = a.AttributeType.Name;
										name = name.EndsWith("Attribute", StringComparison.Ordinal) ? name[..^9] : name;

										return new ExtractedClassFieldAttributeInfo
										{
											Name = name,
											Parameters = a.Constructor.GetParameters()
												.Select(pi => new ExtractedClassFieldAttributeInfo.Parameter
												{
													Name = pi.Name,
													Value = Util.GetAttributeParameterValue(a.ConstructorArguments[pi.Position])
												})
										};
									})
							};
						})
				});

			var relatedEnums = relatedEnumTypes.OrderBy(t => t.Name).Select(type => new ExtractedEnumInfo
			{
				Namespace = type.Namespace,
				Name = type.Name,
				Values = Enum.GetNames(type).ToDictionary(x => Convert.ToInt32(Enum.Parse(type, x), NumberFormatInfo.InvariantInfo), y => y)
			});

			var result = new
			{
				Version = version,
				WeaponTypes = weaponTypesInfo,
				RelatedEnums = relatedEnums
			};

			return JsonConvert.SerializeObject(result);
		}
	}
}
