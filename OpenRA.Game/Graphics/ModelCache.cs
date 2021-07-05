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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileSystem;

namespace OpenRA.Graphics
{
	using System.IO;

	public class ModelCache
	{
		readonly IModelLoader[] loaders;
		readonly IReadOnlyFileSystem fileSystem;
		readonly Dictionary<string, Dictionary<string, IModel>> models = new Dictionary<string, Dictionary<string, IModel>>();

		public ModelCache(IModelLoader[] loaders, IReadOnlyFileSystem fileSystem)
		{
			this.loaders = loaders;
			this.fileSystem = fileSystem;
		}

		public void CacheModel(string model, string sequence, MiniYaml definition)
		{
			if (!models.ContainsKey(model))
				models.Add(model, new Dictionary<string, IModel>());

			models[model].Add(sequence, LoadModel(model, sequence, definition));
		}

		IModel LoadModel(string unit, string sequence, MiniYaml yaml)
		{
			foreach (var loader in loaders)
				if (loader.TryLoadModel(fileSystem, unit, yaml, out var model))
					return model;

			throw new InvalidDataException(unit + "." + sequence + " is not a valid model file!");
		}

		public IModel GetModel(string filename)
		{
			foreach (var loader in loaders)
				if (loader.TryLoadModel(fileSystem, filename, out var model))
					return model;

			throw new InvalidDataException(filename + " is not a valid model file!");
		}

		public IModel GetModelSequence(string model, string sequence)
		{
			if (!HasModelSequence(model, sequence))
				throw new InvalidOperationException(
					$"Model `{model}` does not have a sequence `{sequence}`");

			return models[model][sequence];
		}

		public bool HasModelSequence(string model, string sequence)
		{
			if (!models.ContainsKey(model))
				throw new InvalidOperationException(
					$"Model `{model}` does not have any sequences defined.");

			return models[model].ContainsKey(sequence);
		}

		public void Dispose()
		{
			foreach (var model in models.SelectMany(model => model.Value.Values))
				model.Dispose();
		}
	}
}
