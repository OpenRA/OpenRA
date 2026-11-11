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
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.Common.UtilityCommands.Documentation.Objects;
using OpenRA.Video;

namespace OpenRA.Mods.Common.UtilityCommands.Documentation
{
	sealed class ExtractAssetLoaderDocsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--asset-loaders-docs";

		bool IUtilityCommand.ValidateArguments(string[] args) => true;

		[Desc("[VERSION]", "Generate asset loader documentation in JSON format.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = utility.ModData;

			var version = utility.ModData.Manifest.Metadata.Version;
			if (args.Length > 1)
				version = args[1];

			var objectCreator = utility.ModData.ObjectCreator;
			var packageLoaderTypes = objectCreator.GetTypesImplementing<IPackageLoader>().OrderBy(t => t.Namespace).ThenBy(t => t.Name);
			var soundLoaderTypes = objectCreator.GetTypesImplementing<ISoundLoader>().OrderBy(t => t.Namespace).ThenBy(t => t.Name);
			var spriteLoaderTypes = objectCreator.GetTypesImplementing<ISpriteLoader>().OrderBy(t => t.Namespace).ThenBy(t => t.Name);
			var videoLoaderTypes = objectCreator.GetTypesImplementing<IVideoLoader>().OrderBy(t => t.Namespace).ThenBy(t => t.Name);
			var assetLoaderTypes = packageLoaderTypes
				.Union(soundLoaderTypes)
				.Union(spriteLoaderTypes)
				.Union(videoLoaderTypes);

			var json = GenerateJson(version, assetLoaderTypes);
			Console.WriteLine(json);
		}

		static string GenerateJson(string version, IEnumerable<Type> assetLoaderTypes)
		{
			var relatedEnumTypes = new HashSet<Type>();
			var pdbReaderCache = Utilities.CreatePdbReaderCache();

			var assetLoaderTypesInfo = assetLoaderTypes
				.Where(x => !x.ContainsGenericParameters && !x.IsAbstract)
				.Select(type => new ExtractedClassInfo
				{
					Namespace = type.Namespace,
					Name = type.Name,
					Filename = Utilities.GetSourceFilenameFromPdb(type, pdbReaderCache),
					Description = string.Join(" ", type.GetCustomAttributes<DescAttribute>(false).SelectMany(d => d.Lines)),
					InheritedTypes = type.GetInterfaces().Select(y => y.Name)
				});

			var result = new
			{
				Version = version,
				AssetLoaderTypes = assetLoaderTypesInfo
			};

			return JsonConvert.SerializeObject(result);
		}
	}
}
