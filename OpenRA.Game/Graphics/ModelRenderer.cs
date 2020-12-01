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
using System.Linq;
using OpenRA.Primitives;

namespace OpenRA.Graphics
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

	public sealed class ModelRenderer : IDisposable
	{
		// Static constants
		static readonly float[] ShadowDiffuse = new float[] { 0, 0, 0 };
		static readonly float[] ShadowAmbient = new float[] { 1, 1, 1 };
		static readonly float2 SpritePadding = new float2(2, 2);
		static readonly float[] ZeroVector = new float[] { 0, 0, 0, 1 };
		static readonly float[] ZVector = new float[] { 0, 0, 1, 1 };
		static readonly float[] FlipMtx = Util.ScaleMatrix(1, -1, 1);
		static readonly float[] ShadowScaleFlipMtx = Util.ScaleMatrix(2, -2, 2);

		readonly Renderer renderer;
		readonly IShader shader;

		readonly Dictionary<Sheet, IFrameBuffer> mappedBuffers = new Dictionary<Sheet, IFrameBuffer>();
		readonly Stack<KeyValuePair<Sheet, IFrameBuffer>> unmappedBuffers = new Stack<KeyValuePair<Sheet, IFrameBuffer>>();
		readonly List<(Sheet Sheet, Action Func)> doRender = new List<(Sheet, Action)>();

		SheetBuilder sheetBuilderForFrame;
		bool isInFrame;

		public ModelRenderer(Renderer renderer, IShader shader)
		{
			this.renderer = renderer;
			this.shader = shader;
		}

		public void SetPalette(ITexture palette)
		{
			shader.SetTexture("Palette", palette);
		}

		public void SetViewportParams(Size screen, int2 scroll)
		{
			var a = 2f / renderer.SheetSize;
			var view = new[]
			{
				a, 0, 0, 0,
				0, -a, 0, 0,
				0, 0, -2 * a, 0,
				-1, 1, 0, 1
			};

			shader.SetMatrix("View", view);
		}

		public ModelRenderProxy RenderAsync(
			WorldRenderer wr, IEnumerable<ModelAnimation> models, in WRot camera, float scale,
			float[] groundNormal, in WRot lightSource, float[] lightAmbientColor, float[] lightDiffuseColor,
			PaletteReference color, PaletteReference normals, PaletteReference shadowPalette)
		{
			if (!isInFrame)
				throw new InvalidOperationException("BeginFrame has not been called. You cannot render until a frame has been started.");

			// Correct for inverted y-axis
			var scaleTransform = Util.ScaleMatrix(scale, scale, scale);

			// Correct for bogus light source definition
			var lightYaw = Util.MakeFloatMatrix(new WRot(WAngle.Zero, WAngle.Zero, -lightSource.Yaw).AsMatrix());
			var lightPitch = Util.MakeFloatMatrix(new WRot(WAngle.Zero, -lightSource.Pitch, WAngle.Zero).AsMatrix());
			var shadowTransform = Util.MatrixMultiply(lightPitch, lightYaw);

			var invShadowTransform = Util.MatrixInverse(shadowTransform);
			var cameraTransform = Util.MakeFloatMatrix(camera.AsMatrix());
			var invCameraTransform = Util.MatrixInverse(cameraTransform);
			if (invCameraTransform == null)
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
				var offsetVec = Util.MatrixVectorMultiply(invCameraTransform, wr.ScreenVector(m.OffsetFunc()));
				var offsetTransform = Util.TranslationMatrix(offsetVec[0], offsetVec[1], offsetVec[2]);

				var worldTransform = Util.MakeFloatMatrix(m.RotationFunc().AsMatrix());
				worldTransform = Util.MatrixMultiply(scaleTransform, worldTransform);
				worldTransform = Util.MatrixMultiply(offsetTransform, worldTransform);

				var bounds = m.Model.Bounds(m.FrameFunc());
				var worldBounds = Util.MatrixAABBMultiply(worldTransform, bounds);
				var screenBounds = Util.MatrixAABBMultiply(cameraTransform, worldBounds);
				var shadowBounds = Util.MatrixAABBMultiply(shadowTransform, worldBounds);

				// Aggregate bounds rects
				tl = float2.Min(tl, new float2(screenBounds[0], screenBounds[1]));
				br = float2.Max(br, new float2(screenBounds[3], screenBounds[4]));
				stl = float2.Min(stl, new float2(shadowBounds[0], shadowBounds[1]));
				sbr = float2.Max(sbr, new float2(shadowBounds[3], shadowBounds[4]));
			}

			// Inflate rects to ensure rendering is within bounds
			tl -= SpritePadding;
			br += SpritePadding;
			stl -= SpritePadding;
			sbr += SpritePadding;

			// Corners of the shadow quad, in shadow-space
			var corners = new float[][]
			{
				new[] { stl.X, stl.Y, 0, 1 },
				new[] { sbr.X, sbr.Y, 0, 1 },
				new[] { sbr.X, stl.Y, 0, 1 },
				new[] { stl.X, sbr.Y, 0, 1 }
			};

			var shadowScreenTransform = Util.MatrixMultiply(cameraTransform, invShadowTransform);
			var shadowGroundNormal = Util.MatrixVectorMultiply(shadowTransform, groundNormal);
			var screenCorners = new float3[4];
			for (var j = 0; j < 4; j++)
			{
				// Project to ground plane
				corners[j][2] = -(corners[j][1] * shadowGroundNormal[1] / shadowGroundNormal[2] +
								  corners[j][0] * shadowGroundNormal[0] / shadowGroundNormal[2]);

				// Rotate to camera-space
				corners[j] = Util.MatrixVectorMultiply(shadowScreenTransform, corners[j]);
				screenCorners[j] = new float3(corners[j][0], corners[j][1], 0);
			}

			// Shadows are rendered at twice the resolution to reduce artifacts
			CalculateSpriteGeometry(tl, br, 1, out var spriteSize, out var spriteOffset);
			CalculateSpriteGeometry(stl, sbr, 2, out var shadowSpriteSize, out var shadowSpriteOffset);

			if (sheetBuilderForFrame == null)
				sheetBuilderForFrame = new SheetBuilder(SheetType.BGRA, AllocateSheet);

			var sprite = sheetBuilderForFrame.Allocate(spriteSize, 0, spriteOffset);
			var shadowSprite = sheetBuilderForFrame.Allocate(shadowSpriteSize, 0, shadowSpriteOffset);
			var sb = sprite.Bounds;
			var ssb = shadowSprite.Bounds;
			var spriteCenter = new float2(sb.Left + sb.Width / 2, sb.Top + sb.Height / 2);
			var shadowCenter = new float2(ssb.Left + ssb.Width / 2, ssb.Top + ssb.Height / 2);

			var translateMtx = Util.TranslationMatrix(spriteCenter.X - spriteOffset.X, renderer.SheetSize - (spriteCenter.Y - spriteOffset.Y), 0);
			var shadowTranslateMtx = Util.TranslationMatrix(shadowCenter.X - shadowSpriteOffset.X, renderer.SheetSize - (shadowCenter.Y - shadowSpriteOffset.Y), 0);
			var correctionTransform = Util.MatrixMultiply(translateMtx, FlipMtx);
			var shadowCorrectionTransform = Util.MatrixMultiply(shadowTranslateMtx, ShadowScaleFlipMtx);

			doRender.Add((sprite.Sheet, () =>
			{
				foreach (var m in models)
				{
					// Convert screen offset to world offset
					var offsetVec = Util.MatrixVectorMultiply(invCameraTransform, wr.ScreenVector(m.OffsetFunc()));
					var offsetTransform = Util.TranslationMatrix(offsetVec[0], offsetVec[1], offsetVec[2]);

					var rotations = Util.MakeFloatMatrix(m.RotationFunc().AsMatrix());
					var worldTransform = Util.MatrixMultiply(scaleTransform, rotations);
					worldTransform = Util.MatrixMultiply(offsetTransform, worldTransform);

					var transform = Util.MatrixMultiply(cameraTransform, worldTransform);
					transform = Util.MatrixMultiply(correctionTransform, transform);

					var shadow = Util.MatrixMultiply(shadowTransform, worldTransform);
					shadow = Util.MatrixMultiply(shadowCorrectionTransform, shadow);

					var lightTransform = Util.MatrixMultiply(Util.MatrixInverse(rotations), invShadowTransform);

					var frame = m.FrameFunc();
					for (uint i = 0; i < m.Model.Sections; i++)
					{
						var rd = m.Model.RenderData(i);
						var t = m.Model.TransformationMatrix(i, frame);
						var it = Util.MatrixInverse(t);
						if (it == null)
							throw new InvalidOperationException("Failed to invert the transformed matrix of frame {0} during RenderAsync.".F(i));

						// Transform light vector from shadow -> world -> limb coords
						var lightDirection = ExtractRotationVector(Util.MatrixMultiply(it, lightTransform));

						Render(rd, wr.World.ModelCache, Util.MatrixMultiply(transform, t), lightDirection,
							lightAmbientColor, lightDiffuseColor, color.TextureMidIndex, normals.TextureMidIndex);

						// Disable shadow normals by forcing zero diffuse and identity ambient light
						if (m.ShowShadow)
							Render(rd, wr.World.ModelCache, Util.MatrixMultiply(shadow, t), lightDirection,
								ShadowAmbient, ShadowDiffuse, shadowPalette.TextureMidIndex, normals.TextureMidIndex);
					}
				}
			}));

			var screenLightVector = Util.MatrixVectorMultiply(invShadowTransform, ZVector);
			screenLightVector = Util.MatrixVectorMultiply(cameraTransform, screenLightVector);
			return new ModelRenderProxy(sprite, shadowSprite, screenCorners, -screenLightVector[2] / screenLightVector[1]);
		}

		static void CalculateSpriteGeometry(float2 tl, float2 br, float scale, out Size size, out int2 offset)
		{
			var width = (int)(scale * (br.X - tl.X));
			var height = (int)(scale * (br.Y - tl.Y));
			offset = (0.5f * scale * (br + tl)).ToInt2();

			// Width and height must be even to avoid rendering glitches
			if ((width & 1) == 1)
				width += 1;
			if ((height & 1) == 1)
				height += 1;

			size = new Size(width, height);
		}

		static float[] ExtractRotationVector(float[] mtx)
		{
			var tVec = Util.MatrixVectorMultiply(mtx, ZVector);
			var tOrigin = Util.MatrixVectorMultiply(mtx, ZeroVector);
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
			float[] t, float[] lightDirection,
			float[] ambientLight, float[] diffuseLight,
			float colorPaletteTextureMidIndex, float normalsPaletteTextureMidIndex)
		{
			shader.SetTexture("DiffuseTexture", renderData.Sheet.GetTexture());
			shader.SetVec("PaletteRows", colorPaletteTextureMidIndex, normalsPaletteTextureMidIndex);
			shader.SetMatrix("TransformMatrix", t);
			shader.SetVec("LightDirection", lightDirection, 4);
			shader.SetVec("AmbientLight", ambientLight, 3);
			shader.SetVec("DiffuseLight", diffuseLight, 3);

			shader.PrepareRender();
			renderer.DrawBatch(cache.VertexBuffer, renderData.Start, renderData.Count, PrimitiveType.TriangleList);
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

			Game.Renderer.Context.EnableDepthBuffer();
			return fbo;
		}

		void DisableFrameBuffer(IFrameBuffer fbo)
		{
			Game.Renderer.Flush();
			Game.Renderer.Context.DisableDepthBuffer();
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

			var size = new Size(renderer.SheetSize, renderer.SheetSize);
			var framebuffer = renderer.Context.CreateFrameBuffer(size);
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
		}
	}
}
