#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class ExtractTraitDocsCommand : IUtilityCommand
	{
		public string Name { get { return "--docs"; } }

		[Desc("Generate trait documentation in MarkDown format.")]
		public void Run(ModData modData, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.modData = modData;

			Console.WriteLine(
				"This documentation is aimed at modders. It displays all traits with default values and developer commentary. " +
				"Please do not edit it directly, but add new `[Desc(\"String\")]` tags to the source code. This file has been " +
				"automatically generated for version {0} of OpenRA.", Game.modData.Manifest.Mod.Version);
			Console.WriteLine();

			var toc = new StringBuilder();
			var doc = new StringBuilder();

			foreach (var t in Game.modData.ObjectCreator.GetTypesImplementing<ITraitInfo>().OrderBy(t => t.Namespace))
			{
				if (t.ContainsGenericParameters || t.IsAbstract)
					continue; // skip helpers like TraitInfo<T>

				var traitName = t.Name.EndsWith("Info") ? t.Name.Substring(0, t.Name.Length - 4) : t.Name;
				toc.AppendLine("* [{0}](#{1})".F(traitName, traitName.ToLowerInvariant()));
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
						doc.AppendLine("\n");

					doc.Append("Requires trait{0}: ".F(reqCount > 1 ? "s" : ""));

					var i = 0;
					foreach (var require in requires)
					{
						var n = require.Name;
						var name = n.EndsWith("Info") ? n.Remove(n.Length - 4, 4) : n;
						doc.Append("`{0}`{1}".F(name, i + 1 == reqCount ? ".\n" : ", "));
						i++;
					}
				}

				var infos = FieldLoader.GetTypeLoadInfo(t);
				if (!infos.Any())
					continue;
				doc.AppendLine("<table>");
				doc.AppendLine("<tr><th>Property</th><th>Default Value</th><th>Type</th><th>Description</th></tr>");
				var liveTraitInfo = Game.modData.ObjectCreator.CreateBasic(t);
				foreach (var info in infos)
				{
					var fieldDescLines = info.Field.GetCustomAttributes<DescAttribute>(true).SelectMany(d => d.Lines);
					var fieldType = FriendlyTypeName(info.Field.FieldType);
					var defaultValue = FieldSaver.SaveField(liveTraitInfo, info.Field.Name).Value.Value;
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
			if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
				return "Dictionary<{0},{1}>".F(t.GetGenericArguments().Select(FriendlyTypeName).ToArray());

			if (t.IsSubclassOf(typeof(Array)))
				return "Multiple {0}".F(FriendlyTypeName(t.GetElementType()));

			if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(OpenRA.Primitives.Cache<,>))
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

			if (t == typeof(WRange))
				return "1D World Range";

			if (t == typeof(WVec))
				return "3D World Vector";

			return t.Name;
		}
	}
}
