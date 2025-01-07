using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OpenRA.Mods.Common.UtilityCommands.Documentation.Objects;

namespace OpenRA.Mods.Common.UtilityCommands.Documentation
{
	public static class DocumentationHelpers
	{
		public static IEnumerable<ExtractedClassFieldInfo> GetClassFieldInfos(Type type, IEnumerable<FieldLoader.FieldLoadInfo> fields,
			HashSet<Type> relatedEnumTypes, ObjectCreator objectCreator)
		{
			return fields
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
				});
		}

		public static IEnumerable<ExtractedEnumInfo> GetRelatedEnumInfos(HashSet<Type> relatedEnumTypes)
		{
			return relatedEnumTypes.OrderBy(t => t.Name).Select(type => new ExtractedEnumInfo
			{
				Namespace = type.Namespace,
				Name = type.Name,
				Values = Enum.GetNames(type).ToDictionary(x => Convert.ToInt32(Enum.Parse(type, x), NumberFormatInfo.InvariantInfo), y => y)
			});
		}
	}
}
