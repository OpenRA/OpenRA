#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;

namespace OpenRA.Graphics
{
	public class VoxelRenderer
	{
		Renderer renderer;
		IShader shader;
		IShader shadowShader;

		public VoxelRenderer(Renderer renderer, IShader shader, IShader shadowShader)
		{
			this.renderer = renderer;
			this.shader = shader;
			this.shadowShader = shadowShader;
		}

		public void Render(VoxelLoader loader, VoxelRenderData renderData,
		                   float[] t, float[] lightDirection,
		                   float[] ambientLight, float[] diffuseLight,
		                   int colorPalette, int normalsPalette)
		{
			shader.SetTexture("DiffuseTexture", renderData.Sheet.Texture);
			shader.SetVec("PaletteRows", (colorPalette + 0.5f) / HardwarePalette.MaxPalettes,
			                             (normalsPalette + 0.5f) / HardwarePalette.MaxPalettes);
			shader.SetMatrix("TransformMatrix", t);
			shader.SetVec("LightDirection", lightDirection, 4);
			shader.SetVec("AmbientLight", ambientLight, 3);
			shader.SetVec("DiffuseLight", diffuseLight, 3);
			shader.Render(() => renderer.DrawBatch(loader.VertexBuffer, renderData.Start, renderData.Count, PrimitiveType.QuadList));
		}

		public void RenderShadow(VoxelLoader loader, VoxelRenderData renderData,
		                   float[] t, float[] lightDirection, float[] groundNormal, float groundZ, int colorPalette)
		{
			shadowShader.SetTexture("DiffuseTexture", renderData.Sheet.Texture);
			shadowShader.SetVec("PaletteRows", (colorPalette + 0.5f) / HardwarePalette.MaxPalettes, 0);
			shadowShader.SetMatrix("TransformMatrix", t);
			shadowShader.SetVec("LightDirection", lightDirection, 4);
			shadowShader.SetVec("GroundNormal", groundNormal, 3);
			shadowShader.SetVec("GroundZ", groundZ);
			shadowShader.Render(() => renderer.DrawBatch(loader.VertexBuffer, renderData.Start, renderData.Count, PrimitiveType.QuadList));
		}

		public void SetPalette(ITexture palette)
		{
			shader.SetTexture("Palette", palette);
			shadowShader.SetTexture("Palette", palette);
		}

		public void SetViewportParams(Size screen, float zoom, float2 scroll)
		{
			// Construct projection matrix
			// Clip planes are set at -height and +2*height

			var tiw = 2*zoom / screen.Width;
			var tih = 2*zoom / screen.Height;
			var view = new float[]
			{
				tiw, 0, 0, 0,
				0, -tih, 0, 0,
				0, 0, -tih/3, 0,
				-1 - tiw*scroll.X,
				1 + tih*scroll.Y,
				1 + tih*scroll.Y/3,
				1
			};

			shader.SetMatrix("View", view);
			shadowShader.SetMatrix("View", view);
		}
	}
}
