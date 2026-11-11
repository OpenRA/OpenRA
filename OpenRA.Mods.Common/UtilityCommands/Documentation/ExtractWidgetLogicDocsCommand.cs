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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.UtilityCommands.Documentation
{
	sealed class ExtractWidgetLogicDocsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--widget-logic-docs";

		bool IUtilityCommand.ValidateArguments(string[] args) => true;

		[Desc("[VERSION]", "Generate widget logic documentation in JSON format.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = utility.ModData;

			var version = utility.ModData.Manifest.Metadata.Version;
			if (args.Length > 1)
				version = args[1];

			var objectCreator = utility.ModData.ObjectCreator;
			var widgetLogicTypes = objectCreator.GetTypesImplementing<ChromeLogic>().OrderBy(t => t.Namespace).ThenBy(t => t.Name);

			var json = GenerateJson(version, widgetLogicTypes);
			Console.WriteLine(json);
		}

		static string GenerateJson(string version, IEnumerable<Type> widgetLogicTypes)
		{
			var pdbReaderCache = Utilities.CreatePdbReaderCache();

			var widgetLogicTypesInfo = widgetLogicTypes
				.Where(x => !x.ContainsGenericParameters && !x.IsAbstract)
				.Select(type => new ExtractedClassInfo
				{
					Namespace = type.Namespace,
					Name = type.Name,
					Filename = Utilities.GetSourceFilenameFromPdb(type, pdbReaderCache),
					Description = string.Join(" ", type.GetCustomAttributes<DescAttribute>(false).SelectMany(d => d.Lines)),
					InheritedTypes = type.BaseTypes()
						.Select(y => y.Name)
						.Where(y => y != type.Name && y != "Object")
				});

			var result = new
			{
				Version = version,
				WidgetLogicTypes = widgetLogicTypesInfo
			};

			return JsonConvert.SerializeObject(result);
		}
	}
}
