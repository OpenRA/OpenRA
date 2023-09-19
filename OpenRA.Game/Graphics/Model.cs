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
using OpenRA.Primitives;
using OpenRA.Traits;

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

		/// <summary>Returns the smallest rectangle that covers all rotations of all frames in a model.</summary>
		Rectangle AggregateBounds { get; }
	}

	public interface IModelWidget
	{
		public string Palette { get; }
		public float Scale { get; }
		public void Setup(Func<bool> isVisible, Func<string> getPalette, Func<string> getPlayerPalette,
			Func<float> getScale, Func<IModel> getVoxel, Func<WRot> getRotation);
	}

	public readonly struct ModelRenderData
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

	public interface IModelCacheInfo : ITraitInfoInterface { }

	public interface IModelCache
	{
		IModel GetModel(string model);
		IModel GetModelSequence(string model, string sequence);
		bool HasModelSequence(string model, string sequence);
		IVertexBuffer<ModelVertex> VertexBuffer { get; }
	}
}
