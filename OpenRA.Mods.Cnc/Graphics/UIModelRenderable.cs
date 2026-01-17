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
using System.Numerics;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cnc.Graphics
{
	public class UIModelRenderable : IRenderable, IPalettedRenderable
	{
		readonly ModelRenderer renderer;
		readonly IEnumerable<ModelAnimation> models;
		readonly int2 screenPos;
		readonly WRot camera;
		readonly WRot lightSource;
		readonly Vector3 lightAmbientColor;
		readonly Vector3 lightDiffuseColor;
		readonly PaletteReference normalsPalette;
		readonly PaletteReference shadowPalette;
		readonly float scale;

		public UIModelRenderable(
			ModelRenderer renderer, IEnumerable<ModelAnimation> models, WPos effectiveWorldPos, int2 screenPos, int zOffset,
			in WRot camera, float scale, in WRot lightSource, Vector3 lightAmbientColor, Vector3 lightDiffuseColor,
			PaletteReference color, PaletteReference normals, PaletteReference shadow)
		{
			this.renderer = renderer;
			this.models = models;
			Pos = effectiveWorldPos;
			this.screenPos = screenPos;
			ZOffset = zOffset;
			this.scale = scale;
			this.camera = camera;
			this.lightSource = lightSource;
			this.lightAmbientColor = lightAmbientColor;
			this.lightDiffuseColor = lightDiffuseColor;
			Palette = color;
			normalsPalette = normals;
			shadowPalette = shadow;
		}

		public WPos Pos { get; }
		public PaletteReference Palette { get; }
		public int ZOffset { get; }
		public bool IsDecoration => false;

		public IPalettedRenderable WithPalette(PaletteReference newPalette)
		{
			return new UIModelRenderable(
				renderer, models, Pos, screenPos, ZOffset, camera, scale,
				lightSource, lightAmbientColor, lightDiffuseColor,
				newPalette, normalsPalette, shadowPalette);
		}

		public IRenderable WithZOffset(int newOffset) { return this; }
		public IRenderable OffsetBy(in WVec vec) { return this; }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr)
		{
			return new FinalizedUIModelRenderable(wr, this);
		}

		sealed class FinalizedUIModelRenderable : IFinalizedRenderable
		{
			readonly UIModelRenderable model;
			readonly ModelRenderProxy renderProxy;

			public FinalizedUIModelRenderable(WorldRenderer wr, UIModelRenderable model)
			{
				this.model = model;
				var draw = model.models.Where(v => v.IsVisible);

				renderProxy = model.renderer.RenderAsync(
					wr, draw, model.camera, model.scale, WRot.None, model.lightSource,
					model.lightAmbientColor, model.lightDiffuseColor,
					model.Palette, model.normalsPalette, model.shadowPalette);
			}

			public void Render(WorldRenderer wr)
			{
				var pxOrigin = model.screenPos;
				var psb = renderProxy.ProjectedShadowBounds;
				var sa = pxOrigin + psb[0];
				var sb = pxOrigin + psb[2];
				var sc = pxOrigin + psb[1];
				var sd = pxOrigin + psb[3];
				Game.Renderer.RgbaSpriteRenderer.DrawSprite(renderProxy.ShadowSprite, sa, sb, sc, sd, float3.Ones, 1f);
				Game.Renderer.RgbaSpriteRenderer.DrawSprite(renderProxy.Sprite, pxOrigin - 0.5f * renderProxy.Sprite.Size);
			}

			public void RenderDebugGeometry(WorldRenderer wr) { }

			public Rectangle ScreenBounds(WorldRenderer wr)
			{
				return Screen3DBounds(wr).Bounds;
			}

			(Rectangle Bounds, float2 Z) Screen3DBounds(WorldRenderer wr)
			{
				var pxOrigin = model.screenPos;
				var draw = model.models.Where(v => v.IsVisible);
				var scaleTransform = Matrix4x4.CreateScale(model.scale);
				var cameraTransform = model.camera.ToFloatMatrix();

				var minX = float.MaxValue;
				var minY = float.MaxValue;
				var minZ = float.MaxValue;
				var maxX = float.MinValue;
				var maxY = float.MinValue;
				var maxZ = float.MinValue;

				foreach (var v in draw)
				{
					var bounds = v.Model.Bounds(v.FrameFunc());
					var rotation = v.RotationFunc().ToFloatMatrix();
					var worldTransform = rotation * scaleTransform;

					var pxPos = pxOrigin + wr.ScreenVectorComponents(v.OffsetFunc());
					var screenTransform = worldTransform * cameraTransform;

					for (var i = 0; i < 8; i++)
					{
						var vec = bounds.Corner(i);
						var screen = Vector4.Transform(vec, screenTransform);
						minX = Math.Min(minX, pxPos.X + screen[0]);
						minY = Math.Min(minY, pxPos.Y + screen[1]);
						minZ = Math.Min(minZ, pxPos.Z + screen[2]);
						maxX = Math.Max(maxX, pxPos.X + screen[0]);
						maxY = Math.Max(maxY, pxPos.Y + screen[1]);
						maxZ = Math.Max(minZ, pxPos.Z + screen[2]);
					}
				}

				return (Rectangle.FromLTRB((int)minX, (int)minY, (int)maxX, (int)maxY), new float2(minZ, maxZ));
			}
		}
	}
}
