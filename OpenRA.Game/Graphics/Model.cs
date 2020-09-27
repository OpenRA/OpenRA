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
using OpenRA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public interface IModel
	{
		uint Frames { get; }
		uint Sections { get; }

		float[] TransformationMatrix(uint section, uint frame);
		float[] Size { get; }
		float[] Bounds(uint frame);
		ModelRenderData RenderData(uint section);

		/// <summary>Returns the smallest rectangle that covers all rotations of all frames in a model</summary>
		Rectangle AggregateBounds { get; }
	}

	public struct ModelRenderData
	{
		public readonly int Start;
		public readonly int Count;
		public readonly Sheet Sheet;

		public ModelRenderData(int start, int count, Sheet sheet)
		{
			Start = start;
			Count = count;
			Sheet = sheet;
		}
	}

	public interface IModelCache : IDisposable
	{
		IModel GetModel(string model);
		IModel GetModelSequence(string model, string sequence);
		bool HasModelSequence(string model, string sequence);
		IVertexBuffer<Vertex> VertexBuffer { get; }
	}

	public interface IModelSequenceLoader
	{
		Action<string> OnMissingModelError { get; set; }
		IModelCache CacheModels(IReadOnlyFileSystem fileSystem, ModData modData, IReadOnlyDictionary<string, MiniYamlNode> modelDefinitions);
	}

	public class PlaceholderModelSequenceLoader : IModelSequenceLoader
	{
		public Action<string> OnMissingModelError { get; set; }

		class PlaceholderModelCache : IModelCache
		{
			public IVertexBuffer<Vertex> VertexBuffer { get { throw new NotImplementedException(); } }

			public void Dispose() { }

			public IModel GetModel(string model)
			{
				throw new NotImplementedException();
			}

			public IModel GetModelSequence(string model, string sequence)
			{
				throw new NotImplementedException();
			}

			public bool HasModelSequence(string model, string sequence)
			{
				throw new NotImplementedException();
			}
		}

		public PlaceholderModelSequenceLoader(ModData modData) { }

		public IModelCache CacheModels(IReadOnlyFileSystem fileSystem, ModData modData, IReadOnlyDictionary<string, MiniYamlNode> modelDefinitions)
		{
			return new PlaceholderModelCache();
		}
	}
}
