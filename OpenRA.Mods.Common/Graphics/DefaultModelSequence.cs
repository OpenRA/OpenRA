#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.FileSystem;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.Graphics
{
	public class DefaultModelSequenceLoader : IModelSequenceLoader
	{
		public DefaultModelSequenceLoader(ModData modData) { }

		public ModelCache CacheModels(IModelLoader[] loaders, IReadOnlyFileSystem fileSystem, ModData modData, IReadOnlyDictionary<string, MiniYamlNode> modelSequences)
		{
			var cache = new ModelCache(loaders, fileSystem);

			foreach (var (unit, unitYaml) in modelSequences)
			{
				modData.LoadScreen.Display();

				var sequences = unitYaml.Value.ToDictionary();

				foreach (var (sequence, sequenceYaml) in sequences)
					cache.CacheModel(unit, sequence, sequenceYaml);
			}

			return cache;
		}
	}
}
