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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using OpenRA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.UtilityCommands
{
	public static class Utilities
	{
		public static MiniYamlNode GetTopLevelNodeByKey(ModData modData, string key,
			Func<Manifest, string[]> manifestPropertySelector,
			Func<Map, MiniYaml> mapPropertySelector = null,
			string mapPath = null)
		{
			if (manifestPropertySelector == null)
				throw new ArgumentNullException(nameof(manifestPropertySelector), "Must pass a non-null manifestPropertySelector");

			Map map = null;
			if (mapPath != null)
			{
				try
				{
					map = new Map(modData, new Folder(Platform.EngineDir).OpenPackage(mapPath, modData.ModFiles));
				}
				catch (InvalidDataException ex)
				{
					Console.WriteLine("Could not load map '{0}' so this data does not include the map's overrides.", mapPath);
					Console.WriteLine(ex);
					map = null;
				}
			}

			var manifestNodes = manifestPropertySelector.Invoke(modData.Manifest);
			var mapProperty = map == null || mapPropertySelector == null ? null
				: mapPropertySelector.Invoke(map);

			var fs = map ?? modData.DefaultFileSystem;
			var topLevelNodes = MiniYaml.Load(fs, manifestNodes, mapProperty);
			return topLevelNodes.FirstOrDefault(n => n.Key == key);
		}

		public static Cache<string, MetadataReader> CreatePdbReaderCache()
		{
			return new Cache<string, MetadataReader>(assemblyPath =>
			{
				var pdbPath = Path.ChangeExtension(assemblyPath, "pdb");
				using var fs = new FileStream(pdbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				var provider = MetadataReaderProvider.FromPortablePdbStream(fs);
				return provider.GetMetadataReader();
			});
		}

		public static string GetSourceFilenameFromPdb(Type type, Cache<string, MetadataReader> pdbReaderCache)
		{
			var filename = "(unknown)";
			try
			{
				var pdb = pdbReaderCache[type.Assembly.Location];

				// Enumerate over ctors before other methods (in case this type is defined across multiple files)
				var methodInfos = type.GetConstructors().Cast<MemberInfo>().Concat(type.GetMethods());
				foreach (var mi in methodInfos)
				{
					var definitionHandle = (MethodDefinitionHandle)MetadataTokens.Handle(mi.MetadataToken);
					var sequencePoints = pdb.GetMethodDebugInformation(definitionHandle.ToDebugInformationHandle())
						.GetSequencePoints()
						.ToList();

					if (sequencePoints.Count == 0)
						continue;

					filename = pdb.GetString(pdb.GetDocument(sequencePoints[0].Document).Name);

					// Remove the common path prefix to give a path relative to the repository root
					for (var i = 0; i < filename.Length; i++)
						if (filename[i] != type.Assembly.Location[i])
							return filename[i..];
				}
			}
			catch
			{
				// Ignored
			}

			return filename;
		}
	}
}
