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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class ExtractTraitDocsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--docs";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return true;
		}

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

			var traitTypesInfo = traitTypes
				.Where(x => !x.ContainsGenericParameters && !x.IsAbstract)
				.Select(type => new
				{
					type.Namespace,
					Name = type.Name.EndsWith("Info") ? type.Name.Substring(0, type.Name.Length - 4) : type.Name,
					Description = string.Join(" ", type.GetCustomAttributes<DescAttribute>(false).SelectMany(d => d.Lines)),
					RequiresTraits = RequiredTraitTypes(type)
						.Select(y => y.Name),
					InheritedTypes = type.BaseTypes()
						.Select(y => y.Name)
						.Where(y => y != type.Name && y != $"{type.Name}Info" && y != "Object" && y != "TraitInfo`1"), // HACK: This is the simplest way to exclude TraitInfo<T>, which doesn't serialize well.
					Properties = FieldLoader.GetTypeLoadInfo(type)
						.Where(fi => fi.Field.IsPublic && fi.Field.IsInitOnly && !fi.Field.IsStatic)
						.Select(fi =>
						{
							if (fi.Field.FieldType.IsEnum)
								relatedEnumTypes.Add(fi.Field.FieldType);

							return new
							{
								PropertyName = fi.YamlName,
								DefaultValue = FieldSaver.SaveField(objectCreator.CreateBasic(type), fi.Field.Name).Value.Value,
								InternalType = Util.InternalTypeName(fi.Field.FieldType),
								UserFriendlyType = Util.FriendlyTypeName(fi.Field.FieldType),
								Description = string.Join(" ", fi.Field.GetCustomAttributes<DescAttribute>(true).SelectMany(d => d.Lines)),
								OtherAttributes = fi.Field.CustomAttributes
									.Where(a => a.AttributeType.Name != nameof(DescAttribute) && a.AttributeType.Name != nameof(FieldLoader.LoadUsingAttribute))
									.Select(a =>
									{
										var name = a.AttributeType.Name;
										name = name.EndsWith("Attribute") ? name.Substring(0, name.Length - 9) : name;

										return new
										{
											Name = name,
											Parameters = a.Constructor.GetParameters()
												.Select(pi => new
												{
													pi.Name,
													Value = Util.GetAttributeParameterValue(a.ConstructorArguments[pi.Position])
												})
										};
									})
							};
						})
				});

			var relatedEnums = relatedEnumTypes.OrderBy(t => t.Name).Select(type => new
			{
				type.Namespace,
				type.Name,
				Values = Enum.GetNames(type).Select(x => new
				{
					Key = Convert.ToInt32(Enum.Parse(type, x)),
					Value = x
				})
			});

			var result = new
			{
				Version = version,
				TraitInfos = traitTypesInfo,
				RelatedEnums = relatedEnums
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
