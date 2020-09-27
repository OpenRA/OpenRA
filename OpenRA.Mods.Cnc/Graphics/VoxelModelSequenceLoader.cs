#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using OpenRA.FileSystem;
using OpenRA.Graphics;

namespace OpenRA.Mods.Cnc.Graphics
{
	public class VoxelModelSequenceLoader : IModelSequenceLoader
	{
		public Action<string> OnMissingModelError { get; set; }

		public VoxelModelSequenceLoader(ModData modData) { }

		public IModelCache CacheModels(IReadOnlyFileSystem fileSystem, ModData modData, IReadOnlyDictionary<string, MiniYamlNode> modelSequences)
		{
			var cache = new VoxelModelCache(fileSystem);
			foreach (var kv in modelSequences)
			{
				modData.LoadScreen.Display();
				try
				{
					cache.CacheModel(kv.Key, kv.Value.Value);
				}
				catch (FileNotFoundException ex)
				{
					Console.WriteLine(ex);

					// Eat the FileNotFound exceptions from missing sprites
					OnMissingModelError(ex.Message);
				}
			}

			cache.LoadComplete();

			return cache;
		}
	}

	public class VoxelModelCache : IModelCache
	{
		readonly VoxelLoader loader;
		readonly Dictionary<string, Dictionary<string, IModel>> models = new Dictionary<string, Dictionary<string, IModel>>();

		public VoxelModelCache(IReadOnlyFileSystem fileSystem)
		{
			loader = new VoxelLoader(fileSystem);
		}

		public void CacheModel(string model, MiniYaml definition)
		{
			models.Add(model, definition.ToDictionary(my => LoadVoxel(model, my)));
		}

		public void LoadComplete()
		{
			loader.RefreshBuffer();
			loader.Finish();
		}

		IModel LoadVoxel(string unit, MiniYaml info)
		{
			var vxl = unit;
			var hva = unit;
			if (info.Value != null)
			{
				var fields = info.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				if (fields.Length >= 1)
					vxl = hva = fields[0].Trim();

				if (fields.Length >= 2)
					hva = fields[1].Trim();
			}

			return loader.Load(vxl, hva);
		}

		public IModel GetModel(string model)
		{
			return loader.Load(model, model);
		}

		public IModel GetModelSequence(string model, string sequence)
		{
			try { return models[model][sequence]; }
			catch (KeyNotFoundException)
			{
				if (models.ContainsKey(model))
					throw new InvalidOperationException(
						"Model `{0}` does not have a sequence `{1}`".F(model, sequence));
				else
					throw new InvalidOperationException(
						"Model `{0}` does not have any sequences defined.".F(model));
			}
		}

		public bool HasModelSequence(string model, string sequence)
		{
			if (!models.ContainsKey(model))
				throw new InvalidOperationException(
					"Model `{0}` does not have any sequences defined.".F(model));

			return models[model].ContainsKey(sequence);
		}

		public IVertexBuffer<Vertex> VertexBuffer { get { return loader.VertexBuffer; } }

		public void Dispose()
		{
			loader.Dispose();
		}
	}
}
