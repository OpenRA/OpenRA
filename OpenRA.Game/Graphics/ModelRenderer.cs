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
		static readonly float4 ZeroVector = new float4(0, 0, 0, 1);
		static readonly float4 ZVector = new float4(0, 0, 1, 1);
		static readonly FloatMatrix4x4 FlipMtx = FloatMatrix4x4.CreateScale(new float3(1, -1, 1));
		static readonly FloatMatrix4x4 ShadowScaleFlipMtx = FloatMatrix4x4.CreateScale(new float3(2, -2, 2));

		readonly Renderer renderer;
		readonly IShader shader;

		readonly Dictionary<Sheet, IFrameBuffer> mappedBuffers = new Dictionary<Sheet, IFrameBuffer>();
		readonly Stack<KeyValuePair<Sheet, IFrameBuffer>> unmappedBuffers = new Stack<KeyValuePair<Sheet, IFrameBuffer>>();
		readonly List<Pair<Sheet, Action>> doRender = new List<Pair<Sheet, Action>>();

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
			var view = new FloatMatrix4x4(
				a, 0, 0, 0,
				0, -a, 0, 0,
				0, 0, -2 * a, 0,
				-1, 1, 0, 1);

			shader.SetMatrix("View", view);
		}

		public ModelRenderProxy RenderAsync(
			WorldRenderer wr, IEnumerable<ModelAnimation> models, WRot camera, float scale,
			float4 groundNormal, WRot lightSource, float[] lightAmbientColor, float[] lightDiffuseColor,
			PaletteReference color, PaletteReference normals, PaletteReference shadowPalette)
		{
			if (!isInFrame)
				throw new InvalidOperationException("BeginFrame has not been called. You cannot render until a frame has been started.");

			// Correct for inverted y-axis
			var scaleTransform = FloatMatrix4x4.CreateScale(new float3(scale, scale, scale));

			// Correct for bogus light source definition
			var lightYaw = (FloatMatrix4x4)new WRot(WAngle.Zero, WAngle.Zero, -lightSource.Yaw).AsMatrix();
			var lightPitch = (FloatMatrix4x4)new WRot(WAngle.Zero, -lightSource.Pitch, WAngle.Zero).AsMatrix();
			var shadowTransform = lightPitch * lightYaw;

			var invShadowTransform = shadowTransform.Invert();
			var cameraTransform = (FloatMatrix4x4)camera.AsMatrix();
			var invCameraTransform = cameraTransform.Invert();

			// Sprite rectangle
			var tl = new float2(float.MaxValue, float.MaxValue);
			var br = new float2(float.MinValue, float.MinValue);

			// Shadow sprite rectangle
			var stl = new float2(float.MaxValue, float.MaxValue);
			var sbr = new float2(float.MinValue, float.MinValue);

			foreach (var m in models)
			{
				// Convert screen offset back to world coords
				var offsetVec = invCameraTransform * wr.ScreenVector(m.OffsetFunc());
				var offsetTransform = FloatMatrix4x4.CreateTranslation(new float3(offsetVec.X, offsetVec.Y, offsetVec.Z));

				var worldTransform = (FloatMatrix4x4)m.RotationFunc().AsMatrix();
				worldTransform = scaleTransform * worldTransform;
				worldTransform = offsetTransform * worldTransform;

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
			var corners = new float4[]
			{
				new float4(stl.X, stl.Y, 0, 1),
				new float4(sbr.X, sbr.Y, 0, 1),
				new float4(sbr.X, stl.Y, 0, 1),
				new float4(stl.X, sbr.Y, 0, 1)
			};

			var shadowScreenTransform = cameraTransform * invShadowTransform;
			var shadowGroundNormal = shadowTransform * groundNormal;
			var screenCorners = new float3[4];
			for (var j = 0; j < 4; j++)
			{
				// Project to ground plane
				var z = -(corners[j].Y * shadowGroundNormal.Y / shadowGroundNormal.Z +
					corners[j].X * shadowGroundNormal.X / shadowGroundNormal.Z);

				// Rotate to camera-space
				corners[j] = shadowScreenTransform * new float4(corners[j].X, corners[j].Y, z, corners[j].W);
				screenCorners[j] = new float3(corners[j].X, corners[j].Y, 0);
			}

			// Shadows are rendered at twice the resolution to reduce artifacts
			Size spriteSize, shadowSpriteSize;
			int2 spriteOffset, shadowSpriteOffset;
			CalculateSpriteGeometry(tl, br, 1, out spriteSize, out spriteOffset);
			CalculateSpriteGeometry(stl, sbr, 2, out shadowSpriteSize, out shadowSpriteOffset);

			if (sheetBuilderForFrame == null)
				sheetBuilderForFrame = new SheetBuilder(SheetType.BGRA, AllocateSheet);

			var sprite = sheetBuilderForFrame.Allocate(spriteSize, 0, spriteOffset);
			var shadowSprite = sheetBuilderForFrame.Allocate(shadowSpriteSize, 0, shadowSpriteOffset);
			var sb = sprite.Bounds;
			var ssb = shadowSprite.Bounds;
			var spriteCenter = new float2(sb.Left + sb.Width / 2, sb.Top + sb.Height / 2);
			var shadowCenter = new float2(ssb.Left + ssb.Width / 2, ssb.Top + ssb.Height / 2);

			var translateMtx = FloatMatrix4x4.CreateTranslation(new float3(spriteCenter.X - spriteOffset.X, renderer.SheetSize - (spriteCenter.Y - spriteOffset.Y), 0));
			var shadowTranslateMtx = FloatMatrix4x4.CreateTranslation(new float3(shadowCenter.X - shadowSpriteOffset.X, renderer.SheetSize - (shadowCenter.Y - shadowSpriteOffset.Y), 0));
			var correctionTransform = translateMtx * FlipMtx;
			var shadowCorrectionTransform = shadowTranslateMtx * ShadowScaleFlipMtx;

			doRender.Add(Pair.New<Sheet, Action>(sprite.Sheet, () =>
			{
				foreach (var m in models)
				{
					// Convert screen offset to world offset
					var offsetVec = invCameraTransform * wr.ScreenVector(m.OffsetFunc());
					var offsetTransform = FloatMatrix4x4.CreateTranslation(new float3(offsetVec.X, offsetVec.Y, offsetVec.Z));

					var rotations = (FloatMatrix4x4)m.RotationFunc().AsMatrix();
					var worldTransform = scaleTransform * rotations;
					worldTransform = offsetTransform * worldTransform;

					var transform = cameraTransform * worldTransform;
					transform = correctionTransform * transform;

					var shadow = shadowTransform * worldTransform;
					shadow = shadowCorrectionTransform * shadow;

					var lightTransform = rotations.Invert() * invShadowTransform;

					var frame = m.FrameFunc();
					for (uint i = 0; i < m.Model.Sections; i++)
					{
						var rd = m.Model.RenderData(i);
						var t = m.Model.TransformationMatrix(i, frame);
						var it = t.Invert();

						// Transform light vector from shadow -> world -> limb coords
						var lightDirection = ExtractRotationVector(it * lightTransform);

						Render(rd, wr.World.ModelCache, transform * t, lightDirection,
							lightAmbientColor, lightDiffuseColor, color.TextureMidIndex, normals.TextureMidIndex);

						// Disable shadow normals by forcing zero diffuse and identity ambient light
						if (m.ShowShadow)
							Render(rd, wr.World.ModelCache, shadow * t, lightDirection,
								ShadowAmbient, ShadowDiffuse, shadowPalette.TextureMidIndex, normals.TextureMidIndex);
					}
				}
			}));

			var screenLightVector = invShadowTransform * ZVector;
			screenLightVector = cameraTransform * screenLightVector;
			return new ModelRenderProxy(sprite, shadowSprite, screenCorners, -screenLightVector.Z / screenLightVector.Y);
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

		static float4 ExtractRotationVector(FloatMatrix4x4 mtx)
		{
			var tVec = mtx * ZVector;
			var tOrigin = mtx * ZeroVector;
			tVec = new float4(
				tVec.X - tOrigin.X * tVec.W / tOrigin.W,
				tVec.Y - tOrigin.Y * tVec.W / tOrigin.W,
				tVec.Z - tOrigin.Z * tVec.W / tOrigin.W,
				tVec.W);

			// Renormalize
			var w = (float)Math.Sqrt(tVec.X * tVec.X + tVec.Y * tVec.Y + tVec.Z * tVec.Z);
			return new float4(tVec.X / w, tVec.Y / w, tVec.Z / w, 1f);
		}

		void Render(
			ModelRenderData renderData,
			IModelCache cache,
			FloatMatrix4x4 t, float4 lightDirection,
			float[] ambientLight, float[] diffuseLight,
			float colorPaletteTextureMidIndex, float normalsPaletteTextureMidIndex)
		{
			shader.SetTexture("DiffuseTexture", renderData.Sheet.GetTexture());
			shader.SetVec("PaletteRows", colorPaletteTextureMidIndex, normalsPaletteTextureMidIndex);
			shader.SetMatrix("TransformMatrix", t);
			shader.SetVec("LightDirection", new[] { lightDirection.X, lightDirection.Y, lightDirection.Z, lightDirection.W }, 4);
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
				if (v.First != currentSheet)
				{
					if (fbo != null)
						DisableFrameBuffer(fbo);

					currentSheet = v.First;
					fbo = EnableFrameBuffer(currentSheet);
				}

				v.Second();
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
