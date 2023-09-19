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
using System.IO;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Loads voxel models.")]
	public sealed class VoxelCacheInfo : TraitInfo, IModelCacheInfo
	{
		public override object Create(ActorInitializer init) { return new VoxelCache(this, init.Self); }
	}

	public sealed class VoxelCache : IModelCache, INotifyActorDisposing, IDisposable
	{
		readonly VoxelLoader loader;
		readonly Dictionary<string, Dictionary<string, IModel>> models = new();

		public VoxelCache(VoxelCacheInfo info, Actor self)
		{
			var map = self.World.Map;
			loader = new VoxelLoader(map);
			foreach (var kv in map.Rules.ModelSequences)
			{
				Game.ModData.LoadScreen.Display();
				try
				{
					CacheModel(kv.Key, kv.Value.Value);
				}
				catch (FileNotFoundException ex)
				{
					// Eat the FileNotFound exceptions from missing sprites.
					Console.WriteLine(ex);
					Log.Write("debug", ex.Message);
				}
			}

			loader.RefreshBuffer();
			loader.Finish();
		}

		public void CacheModel(string model, MiniYaml definition)
		{
			models.Add(model, definition.ToDictionary(my => LoadVoxel(model, my)));
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
						$"Model `{model}` does not have a sequence `{sequence}`");
				else
					throw new InvalidOperationException(
						$"Model `{model}` does not have any sequences defined.");
			}
		}

		public bool HasModelSequence(string model, string sequence)
		{
			if (!models.ContainsKey(model))
				throw new InvalidOperationException(
					$"Model `{model}` does not have any sequences defined.");

			return models[model].ContainsKey(sequence);
		}

		public IVertexBuffer<ModelVertex> VertexBuffer => loader.VertexBuffer;

		public void Dispose()
		{
			loader.Dispose();
		}

		void INotifyActorDisposing.Disposing(Actor a)
		{
			Dispose();
		}
	}
}
