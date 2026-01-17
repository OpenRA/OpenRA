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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	public class ModelRenderProxy
	{
		public readonly Sprite Sprite;
		public readonly Sprite ShadowSprite;
		public readonly float ShadowDirection;
		public readonly float3[] ProjectedShadowBounds;

		public ModelRenderProxy(Sprite sprite, Sprite shadowSprite, float3[] projectedShadowBounds, float shadowDirection)
		{
			Sprite = sprite;
			ShadowSprite = shadowSprite;
			ProjectedShadowBounds = projectedShadowBounds;
			ShadowDirection = shadowDirection;
		}
	}

	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Render voxels")]
	public class ModelRendererInfo : TraitInfo, Requires<IModelCacheInfo>
	{
		public readonly int RenderBufferSize = 2048;
		public override object Create(ActorInitializer init) { return new ModelRenderer(this, init.Self); }
	}

	public sealed class ModelRenderer : IDisposable, IRenderer, INotifyActorDisposing
	{
		// Static constants
		static readonly Vector3 ShadowDiffuse = new(0, 0, 0);
		static readonly Vector3 ShadowAmbient = new(1, 1, 1);
		static readonly float2 SpritePadding = new(2, 2);
		static readonly Vector4 ZeroVector = new(0, 0, 0, 1);
		static readonly Vector4 ZVector = new(0, 0, 1, 1);
		static readonly Matrix4x4 FlipMtx = Matrix4x4.CreateScale(1, -1, 1);
		static readonly Matrix4x4 ShadowScaleFlipMtx = Matrix4x4.CreateScale(2, -2, 2);
		static readonly Vector4 GroundNormal = new(0, 0, 1, 1);

		readonly Renderer renderer;
		readonly IShader shader;
		public readonly IModelCache ModelCache;

		readonly Dictionary<Sheet, IFrameBuffer> mappedBuffers = [];
		readonly Stack<KeyValuePair<Sheet, IFrameBuffer>> unmappedBuffers = [];
		readonly List<(Sheet Sheet, Action Func)> doRender = [];
		readonly int sheetSize;

		SheetBuilder sheetBuilderForFrame;
		bool isInFrame;

		public void SetPalette(HardwarePalette palette)
		{
			shader.SetTexture("Palette", palette.Texture);
			shader.SetVec("PaletteRows", palette.Height);
		}

		public ModelRenderer(ModelRendererInfo info, Actor self)
		{
			renderer = Game.Renderer;
			shader = renderer.CreateShader(new ModelShaderBindings());
			renderer.WorldRenderers = renderer.WorldRenderers.Append(this).ToArray();

			ModelCache = self.Trait<IModelCache>();

			sheetSize = info.RenderBufferSize;
			var a = 2f / sheetSize;
			var view = new Matrix4x4(
				a, 0, 0, -1,
				0, -a, 0, 1,
				0, 0, -2 * a, 0,
				0, 0, 0, 1);

			shader.SetMatrixRowMajor("View", view);
		}

		public ModelRenderProxy RenderAsync(
			WorldRenderer wr, IEnumerable<ModelAnimation> models, in WRot camera, float scale,
			in WRot groundOrientation, in WRot lightSource, Vector3 lightAmbientColor, Vector3 lightDiffuseColor,
			PaletteReference color, PaletteReference normals, PaletteReference shadowPalette)
		{
			if (!isInFrame)
				throw new InvalidOperationException("BeginFrame has not been called. You cannot render until a frame has been started.");

			// Correct for inverted y-axis
			var scaleTransform = Matrix4x4.CreateScale(scale);

			// Correct for bogus light source definition
			var lightYaw = new WRot(WAngle.Zero, WAngle.Zero, -lightSource.Yaw).ToFloatMatrix();
			var lightPitch = new WRot(WAngle.Zero, -lightSource.Pitch, WAngle.Zero).ToFloatMatrix();
			var ground = groundOrientation.ToFloatMatrix();
			Matrix4x4.Invert(ground, out var groundInverse);
			var shadowTransform = lightPitch * lightYaw * groundInverse;

			var groundNormal = Vector4.Transform(GroundNormal, ground);
			Matrix4x4.Invert(shadowTransform, out var invShadowTransform);
			var cameraTransform = camera.ToFloatMatrix();
			if (!Matrix4x4.Invert(cameraTransform, out var invCameraTransform))
				throw new InvalidOperationException("Failed to invert the cameraTransform matrix during RenderAsync.");

			// Sprite rectangle
			var tl = new float2(float.MaxValue, float.MaxValue);
			var br = new float2(float.MinValue, float.MinValue);

			// Shadow sprite rectangle
			var stl = new float2(float.MaxValue, float.MaxValue);
			var sbr = new float2(float.MinValue, float.MinValue);

			foreach (var m in models)
			{
				// Convert screen offset back to world coords
				var offsetVec = Vector4.Transform(wr.ScreenVector(m.OffsetFunc()), invCameraTransform);
				var offsetTransform = Matrix4x4.CreateTranslation(offsetVec.X, offsetVec.Y, offsetVec.Z);

				var worldTransform = m.RotationFunc().ToFloatMatrix();
				worldTransform = scaleTransform * worldTransform;
				worldTransform = offsetTransform * worldTransform;

				var bounds = m.Model.Bounds(m.FrameFunc());
				var worldBounds = AABB.Transform(bounds, worldTransform);
				var screenBounds = AABB.Transform(worldBounds, cameraTransform);
				var shadowBounds = AABB.Transform(worldBounds, shadowTransform);

				// Aggregate bounds rects
				tl = float2.Min(tl, new float2(screenBounds.MinX, screenBounds.MinY));
				br = float2.Max(br, new float2(screenBounds.MaxX, screenBounds.MaxY));
				stl = float2.Min(stl, new float2(shadowBounds.MinX, shadowBounds.MinY));
				sbr = float2.Max(sbr, new float2(shadowBounds.MaxX, shadowBounds.MaxY));
			}

			// Inflate rects to ensure rendering is within bounds
			tl -= SpritePadding;
			br += SpritePadding;
			stl -= SpritePadding;
			sbr += SpritePadding;

			// Corners of the shadow quad, in shadow-space
			var corners = new Vector4[]
			{
				new(stl.X, stl.Y, 0, 1),
				new(sbr.X, sbr.Y, 0, 1),
				new(sbr.X, stl.Y, 0, 1),
				new(stl.X, sbr.Y, 0, 1),
			};

			var shadowScreenTransform = invShadowTransform * cameraTransform;
			var shadowGroundNormal = Vector4.Transform(groundNormal, shadowTransform);
			var screenCorners = new float3[4];
			for (var j = 0; j < 4; j++)
			{
				// Project to ground plane
				corners[j][2] = -(corners[j][1] * shadowGroundNormal[1] / shadowGroundNormal[2] +
								  corners[j][0] * shadowGroundNormal[0] / shadowGroundNormal[2]);

				// Rotate to camera-space
				corners[j] = Vector4.Transform(corners[j], shadowScreenTransform);
				screenCorners[j] = new float3(corners[j][0], corners[j][1], 0);
			}

			// Shadows are rendered at twice the resolution to reduce artifacts
			CalculateSpriteGeometry(tl, br, 1, out var spriteSize, out var spriteOffset);
			CalculateSpriteGeometry(stl, sbr, 2, out var shadowSpriteSize, out var shadowSpriteOffset);

			sheetBuilderForFrame ??= new SheetBuilder(SheetType.BGRA, AllocateSheet);

			var sprite = sheetBuilderForFrame.Allocate(spriteSize, 0, spriteOffset);
			var shadowSprite = sheetBuilderForFrame.Allocate(shadowSpriteSize, 0, shadowSpriteOffset);
			var sb = sprite.Bounds;
			var ssb = shadowSprite.Bounds;
			var spriteCenter = new float2(sb.Left + sb.Width / 2, sb.Top + sb.Height / 2);
			var shadowCenter = new float2(ssb.Left + ssb.Width / 2, ssb.Top + ssb.Height / 2);

			var translateMtx = Matrix4x4.CreateTranslation(spriteCenter.X - spriteOffset.X, sheetSize - (spriteCenter.Y - spriteOffset.Y), 0);
			var shadowTranslateMtx = Matrix4x4.CreateTranslation(shadowCenter.X - shadowSpriteOffset.X, sheetSize - (shadowCenter.Y - shadowSpriteOffset.Y), 0);
			var correctionTransform = FlipMtx * translateMtx;
			var shadowCorrectionTransform = ShadowScaleFlipMtx * shadowTranslateMtx;

			void RenderFunc()
			{
				foreach (var m in models)
				{
					// Convert screen offset to world offset
					var offsetVec = Vector4.Transform(wr.ScreenVector(m.OffsetFunc()), invCameraTransform);
					var offsetTransform = Matrix4x4.CreateTranslation(offsetVec.X, offsetVec.Y, offsetVec.Z);

					var rotations = m.RotationFunc().ToFloatMatrix();
					var worldTransform = scaleTransform * rotations;
					worldTransform *= offsetTransform;

					var transform = worldTransform * cameraTransform;
					transform *= correctionTransform;

					var shadow = worldTransform * shadowTransform;
					shadow *= shadowCorrectionTransform;

					Matrix4x4.Invert(rotations, out var rotationsInverse);
					var lightTransform = invShadowTransform * rotationsInverse;

					var frame = m.FrameFunc();
					for (uint i = 0; i < m.Model.Sections; i++)
					{
						var rd = m.Model.RenderData(i);
						var t = m.Model.TransformationMatrix(i, frame);
						if (!Matrix4x4.Invert(t, out var it))
							throw new InvalidOperationException($"Failed to invert the transformed matrix of frame {i} during RenderAsync.");

						// Transform light vector from shadow -> world -> limb coords
						var lightDirection = ExtractRotationVector(lightTransform * it);

						Render(rd, ModelCache, t * transform, lightDirection,
							lightAmbientColor, lightDiffuseColor, color.TextureIndex, normals.TextureIndex);

						// Disable shadow normals by forcing zero diffuse and identity ambient light
						if (m.ShowShadow)
							Render(rd, ModelCache, t * shadow, lightDirection,
								ShadowAmbient, ShadowDiffuse, shadowPalette.TextureIndex, normals.TextureIndex);
					}
				}
			}

			doRender.Add((sprite.Sheet, RenderFunc));

			var screenLightVector = Vector4.Transform(ZVector, invShadowTransform);
			screenLightVector = Vector4.Transform(screenLightVector, cameraTransform);
			return new ModelRenderProxy(sprite, shadowSprite, screenCorners, -screenLightVector[2] / screenLightVector[1]);
		}

		static void CalculateSpriteGeometry(float2 tl, float2 br, float scale, out Size size, out int2 offset)
		{
			var width = (int)(scale * (br.X - tl.X));
			var height = (int)(scale * (br.Y - tl.Y));
			offset = (0.5f * scale * (br + tl)).ToInt2();

			// Width and height must be even to avoid rendering glitches
			if ((width & 1) == 1)
				width++;
			if ((height & 1) == 1)
				height++;

			size = new Size(width, height);
		}

		static Vector4 ExtractRotationVector(Matrix4x4 mtx)
		{
			var tVec = Vector4.Transform(ZVector, mtx);
			var tOrigin = Vector4.Transform(ZeroVector, mtx);
			tVec[0] -= tOrigin[0] * tVec[3] / tOrigin[3];
			tVec[1] -= tOrigin[1] * tVec[3] / tOrigin[3];
			tVec[2] -= tOrigin[2] * tVec[3] / tOrigin[3];

			// Renormalize
			var w = (float)Math.Sqrt(tVec[0] * tVec[0] + tVec[1] * tVec[1] + tVec[2] * tVec[2]);
			tVec[0] /= w;
			tVec[1] /= w;
			tVec[2] /= w;
			tVec[3] = 1f;

			return tVec;
		}

		void Render(
			ModelRenderData renderData,
			IModelCache cache,
			Matrix4x4 t, Vector4 lightDirection,
			Vector3 ambientLight, Vector3 diffuseLight,
			float colorPaletteTextureIndex, float normalsPaletteTextureIndex)
		{
			shader.SetTexture("DiffuseTexture", renderData.Sheet.GetTexture());
			shader.SetVec("Palettes", colorPaletteTextureIndex, normalsPaletteTextureIndex);
			shader.SetMatrixRowMajor("TransformMatrix", t);
			shader.SetVec("LightDirection", lightDirection);
			shader.SetVec("AmbientLight", ambientLight);
			shader.SetVec("DiffuseLight", diffuseLight);

			shader.PrepareRender();
			renderer.DrawBatch(cache.VertexBuffer, shader, renderData.Start, renderData.Count, PrimitiveType.TriangleList);
		}

		public void BeginFrame()
		{
			if (isInFrame)
				throw new InvalidOperationException("BeginFrame has already been called. A new frame cannot be started until EndFrame has been called.");

			isInFrame = true;

			foreach (var kv in mappedBuffers)
				unmappedBuffers.Push(kv);
			mappedBuffers.Clear();
		}

		IFrameBuffer EnableFrameBuffer(Sheet s)
		{
			var fbo = mappedBuffers[s];
			Game.Renderer.Flush();
			fbo.Bind();

			Game.Renderer.EnableDepthBuffer();
			return fbo;
		}

		static void DisableFrameBuffer(IFrameBuffer fbo)
		{
			Game.Renderer.Flush();
			Game.Renderer.DisableDepthBuffer();
			fbo.Unbind();
		}

		public void EndFrame()
		{
			if (!isInFrame)
				throw new InvalidOperationException("BeginFrame has not been called. There is no frame to end.");

			isInFrame = false;
			sheetBuilderForFrame = null;

			if (doRender.Count == 0)
				return;

			Sheet currentSheet = null;
			IFrameBuffer fbo = null;
			foreach (var v in doRender)
			{
				// Change sheet
				if (v.Sheet != currentSheet)
				{
					if (fbo != null)
						DisableFrameBuffer(fbo);

					currentSheet = v.Sheet;
					fbo = EnableFrameBuffer(currentSheet);
				}

				v.Func();
			}

			if (fbo != null)
				DisableFrameBuffer(fbo);

			doRender.Clear();
		}

		public Sheet AllocateSheet()
		{
			// Reuse cached fbo
			if (unmappedBuffers.Count > 0)
			{
				var kv = unmappedBuffers.Pop();
				mappedBuffers.Add(kv.Key, kv.Value);
				return kv.Key;
			}

			var framebuffer = renderer.CreateFrameBuffer(new Size(sheetSize, sheetSize));
			var sheet = new Sheet(SheetType.BGRA, framebuffer.Texture);
			mappedBuffers.Add(sheet, framebuffer);

			return sheet;
		}

		public void Dispose()
		{
			foreach (var kvp in mappedBuffers.Concat(unmappedBuffers))
			{
				kvp.Key.Dispose();
				kvp.Value.Dispose();
			}

			mappedBuffers.Clear();
			unmappedBuffers.Clear();
			renderer.WorldRenderers = renderer.WorldRenderers.Where(r => r != this).ToArray();
		}

		void INotifyActorDisposing.Disposing(Actor a)
		{
			Dispose();
		}
	}
}
