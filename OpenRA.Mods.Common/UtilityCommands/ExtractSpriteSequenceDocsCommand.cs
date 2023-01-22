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
using System.Reflection;
using Newtonsoft.Json;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class ExtractSpriteSequenceDocsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--sprite-sequence-docs";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return true;
		}

		[Desc("[VERSION]", "Generate sprite sequence documentation in JSON format.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = utility.ModData;

			var version = utility.ModData.Manifest.Metadata.Version;
			if (args.Length > 1)
				version = args[1];

			var objectCreator = utility.ModData.ObjectCreator;
			var spriteSequenceTypes = objectCreator.GetTypesImplementing<ISpriteSequence>().OrderBy(t => t.Namespace).ThenBy(t => t.Name);

			var json = GenerateJson(version, spriteSequenceTypes);
			Console.WriteLine(json);
		}

		static string GenerateJson(string version, IEnumerable<Type> sequenceTypes)
		{
			var relatedEnumTypes = new HashSet<Type>();

			var sequenceTypesInfo = sequenceTypes
				.Where(x => !x.ContainsGenericParameters && !x.IsAbstract
					&& x.Name != nameof(FileNotFoundSequence)) // NOTE: This is the simplest way to exclude FileNotFoundSequence, which shouldn't be added.
				.Select(type => new
				{
					type.Namespace,
					type.Name,
					Description = string.Join(" ", type.GetCustomAttributes<DescAttribute>(false).SelectMany(d => d.Lines)),
					InheritedTypes = type.BaseTypes()
						.Select(y => y.Name)
						.Where(y => y != type.Name && y != "Object"),
					Properties = type.GetFields(BindingFlags.NonPublic | BindingFlags.Static)
						.Where(fi => fi.FieldType.IsGenericType && fi.FieldType.GetGenericTypeDefinition() == typeof(SpriteSequenceField<>))
						.Select(fi =>
						{
							var description = string.Join(" ", fi.GetCustomAttributes<DescAttribute>(false)
								.SelectMany(d => d.Lines));

							var valueType = fi.FieldType.GetGenericArguments()[0];

							var key = (string)fi.FieldType
								.GetField(nameof(SpriteSequenceField<bool>.Key))?
								.GetValue(fi.GetValue(null));

							var defaultValueField = fi.FieldType.GetField(nameof(SpriteSequenceField<bool>.DefaultValue));
							var defaultValue = defaultValueField?.GetValue(fi.GetValue(null));

							if (defaultValueField != null && defaultValueField.FieldType.IsEnum)
								relatedEnumTypes.Add(defaultValueField.FieldType);

							return new
							{
								PropertyName = key,
								DefaultValue = defaultValue?.ToString(),
								InternalType = Util.InternalTypeName(valueType),
								UserFriendlyType = Util.FriendlyTypeName(valueType),
								Description = description
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
				SpriteSequenceTypes = sequenceTypesInfo,
				RelatedEnums = relatedEnums
			};

			return JsonConvert.SerializeObject(result);
		}
	}
}
