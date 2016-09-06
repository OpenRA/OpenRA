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
using System.Text;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class ExtractTraitDocsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--docs"; } }

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return true;
		}

		[Desc("Generate trait documentation in MarkDown format.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = utility.ModData;

			Console.WriteLine(
				"This documentation is aimed at modders. It displays all traits with default values and developer commentary. " +
				"Please do not edit it directly, but add new `[Desc(\"String\")]` tags to the source code. This file has been " +
				"automatically generated for version {0} of OpenRA.", utility.ModData.Manifest.Metadata.Version);
			Console.WriteLine();

			var toc = new StringBuilder();
			var doc = new StringBuilder();
			var currentNamespace = "";

			foreach (var t in Game.ModData.ObjectCreator.GetTypesImplementing<ITraitInfo>().OrderBy(t => t.Namespace))
			{
				if (t.ContainsGenericParameters || t.IsAbstract)
					continue; // skip helpers like TraitInfo<T>

				if (currentNamespace != t.Namespace)
				{
					currentNamespace = t.Namespace;
					doc.AppendLine();
					doc.AppendLine("## {0}".F(currentNamespace));
					toc.AppendLine("* [{0}](#{1})".F(currentNamespace, currentNamespace.Replace(".", "").ToLowerInvariant()));
				}

				var traitName = t.Name.EndsWith("Info") ? t.Name.Substring(0, t.Name.Length - 4) : t.Name;
				toc.AppendLine(" * [{0}](#{1})".F(traitName, traitName.ToLowerInvariant()));
				var traitDescLines = t.GetCustomAttributes<DescAttribute>(false).SelectMany(d => d.Lines);
				doc.AppendLine();
				doc.AppendLine("### {0}".F(traitName));
				foreach (var line in traitDescLines)
					doc.AppendLine(line);

				var requires = RequiredTraitTypes(t);
				var reqCount = requires.Length;
				if (reqCount > 0)
				{
					if (t.HasAttribute<DescAttribute>())
						doc.AppendLine();

					doc.Append("Requires trait{0}: ".F(reqCount > 1 ? "s" : ""));

					var i = 0;
					foreach (var require in requires)
					{
						var n = require.Name;
						var name = n.EndsWith("Info") ? n.Remove(n.Length - 4, 4) : n;
						doc.Append("[`{0}`](#{1}){2}".F(name, name.ToLowerInvariant(), i + 1 == reqCount ? ".\n" : ", "));
						i++;
					}
				}

				var infos = FieldLoader.GetTypeLoadInfo(t);
				if (!infos.Any())
					continue;
				doc.AppendLine("<table>");
				doc.AppendLine("<tr><th>Property</th><th>Default Value</th><th>Type</th><th>Description</th></tr>");
				var liveTraitInfo = Game.ModData.ObjectCreator.CreateBasic(t);
				foreach (var info in infos)
				{
					var fieldDescLines = info.Field.GetCustomAttributes<DescAttribute>(true).SelectMany(d => d.Lines);
					var fieldType = FriendlyTypeName(info.Field.FieldType);
					var loadInfo = info.Field.GetCustomAttributes<FieldLoader.SerializeAttribute>(true).FirstOrDefault();
					var defaultValue = loadInfo != null && loadInfo.Required ? "<em>(required)</em>" : FieldSaver.SaveField(liveTraitInfo, info.Field.Name).Value.Value;
					doc.Append("<tr><td>{0}</td><td>{1}</td><td>{2}</td>".F(info.YamlName, defaultValue, fieldType));
					doc.Append("<td>");
					foreach (var line in fieldDescLines)
						doc.Append(line + " ");
					doc.AppendLine("</td></tr>");
				}

				doc.AppendLine("</table>");
			}

			Console.Write(toc.ToString());
			Console.Write(doc.ToString());
		}

		static Type[] RequiredTraitTypes(Type t)
		{
			return t.GetInterfaces()
				.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(Requires<>))
				.SelectMany(i => i.GetGenericArguments())
				.Where(i => !i.IsInterface && !t.IsSubclassOf(i))
				.OrderBy(i => i.Name)
				.ToArray();
		}

		static string FriendlyTypeName(Type t)
		{
			if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(HashSet<>))
				return "Set of {0}".F(t.GetGenericArguments().Select(FriendlyTypeName).ToArray());

			if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
				return "Dictionary<{0},{1}>".F(t.GetGenericArguments().Select(FriendlyTypeName).ToArray());

			if (t.IsSubclassOf(typeof(Array)))
				return "Multiple {0}".F(FriendlyTypeName(t.GetElementType()));

			if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Primitives.Cache<,>))
				return "Cached<{0},{1}>".F(t.GetGenericArguments().Select(FriendlyTypeName).ToArray());

			if (t == typeof(int) || t == typeof(uint))
				return "Integer";

			if (t == typeof(int2))
				return "2D Integer";

			if (t == typeof(float) || t == typeof(decimal))
				return "Real Number";

			if (t == typeof(float2))
				return "2D Real Number";

			if (t == typeof(CPos))
				return "2D Cell Position";

			if (t == typeof(CVec))
				return "2D Cell Vector";

			if (t == typeof(WAngle))
				return "1D World Angle";

			if (t == typeof(WRot))
				return "3D World Rotation";

			if (t == typeof(WPos))
				return "3D World Position";

			if (t == typeof(WDist))
				return "1D World Distance";

			if (t == typeof(WVec))
				return "3D World Vector";

			if (t == typeof(HSLColor))
				return "Color";

			return t.Name;
		}
	}
}
