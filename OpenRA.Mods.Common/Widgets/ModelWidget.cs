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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ModelWidget : Widget
	{
		public string Palette = "terrain";
		public string PlayerPalette = "player";
		public string NormalsPalette = "normals";
		public string ShadowPalette = "shadow";
		public float Scale = 12f;
		public int LightPitch = 142;
		public int LightYaw = 682;
		public float[] LightAmbientColor = new float[] { 0.6f, 0.6f, 0.6f };
		public float[] LightDiffuseColor = new float[] { 0.4f, 0.4f, 0.4f };
		public WRot Rotation = WRot.None;
		public WAngle CameraAngle = WAngle.FromDegrees(40);

		public Func<string> GetPalette;
		public Func<string> GetPlayerPalette;
		public Func<string> GetNormalsPalette;
		public Func<string> GetShadowPalette;
		public Func<float[]> GetLightAmbientColor;
		public Func<float[]> GetLightDiffuseColor;
		public Func<float> GetScale;
		public Func<int> GetLightPitch;
		public Func<int> GetLightYaw;
		public Func<IModel> GetVoxel;
		public Func<WRot> GetRotation;
		public Func<WAngle> GetCameraAngle;
		public int2 IdealPreviewSize { get; private set; }

		protected readonly WorldRenderer WorldRenderer;

		IFinalizedRenderable renderable;

		[ObjectCreator.UseCtor]
		public ModelWidget(WorldRenderer worldRenderer)
		{
			GetPalette = () => Palette;
			GetPlayerPalette = () => PlayerPalette;
			GetNormalsPalette = () => NormalsPalette;
			GetShadowPalette = () => ShadowPalette;
			GetLightAmbientColor = () => LightAmbientColor;
			GetLightDiffuseColor = () => LightDiffuseColor;
			GetScale = () => Scale;
			GetRotation = () => Rotation;
			GetLightPitch = () => LightPitch;
			GetLightYaw = () => LightYaw;
			GetCameraAngle = () => CameraAngle;
			WorldRenderer = worldRenderer;
		}

		protected ModelWidget(ModelWidget other)
			: base(other)
		{
			Palette = other.Palette;
			GetPalette = other.GetPalette;
			GetVoxel = other.GetVoxel;

			WorldRenderer = other.WorldRenderer;
		}

		public override Widget Clone()
		{
			return new ModelWidget(this);
		}

		IModel cachedVoxel;
		string cachedPalette;
		string cachedPlayerPalette;
		string cachedNormalsPalette;
		string cachedShadowPalette;
		float cachedScale;
		WRot cachedRotation;
		float[] cachedLightAmbientColor = new float[] { 0, 0, 0 };
		float[] cachedLightDiffuseColor = new float[] { 0, 0, 0 };
		int cachedLightPitch;
		int cachedLightYaw;
		WAngle cachedCameraAngle;
		PaletteReference paletteReference;
		PaletteReference paletteReferencePlayer;
		PaletteReference paletteReferenceNormals;
		PaletteReference paletteReferenceShadow;

		public override void Draw()
		{
			if (renderable == null)
				return;

			renderable.Render(WorldRenderer);
		}

		public override void PrepareRenderables()
		{
			var voxel = GetVoxel();
			var palette = GetPalette();
			var playerPalette = GetPlayerPalette();
			var normalsPalette = GetNormalsPalette();
			var shadowPalette = GetShadowPalette();
			var scale = GetScale();
			var rotation = GetRotation();
			var lightAmbientColor = GetLightAmbientColor();
			var lightDiffuseColor = GetLightDiffuseColor();
			var lightPitch = GetLightPitch();
			var lightYaw = GetLightYaw();
			var cameraAngle = GetCameraAngle();

			if (voxel == null || palette == null)
				return;

			if (voxel != cachedVoxel)
				cachedVoxel = voxel;

			if (palette != cachedPalette)
			{
				if (string.IsNullOrEmpty(palette) && string.IsNullOrEmpty(playerPalette))
					return;

				var paletteName = string.IsNullOrEmpty(palette) ? playerPalette : palette;
				paletteReference = WorldRenderer.Palette(paletteName);
				cachedPalette = paletteName;
			}

			if (playerPalette != cachedPlayerPalette)
			{
				paletteReferencePlayer = WorldRenderer.Palette(playerPalette);
				cachedPlayerPalette = playerPalette;
			}

			if (normalsPalette != cachedNormalsPalette)
			{
				paletteReferenceNormals = WorldRenderer.Palette(normalsPalette);
				cachedNormalsPalette = normalsPalette;
			}

			if (shadowPalette != cachedShadowPalette)
			{
				paletteReferenceShadow = WorldRenderer.Palette(shadowPalette);
				cachedShadowPalette = shadowPalette;
			}

			if (scale != cachedScale)
				cachedScale = scale;

			if (rotation != cachedRotation)
				cachedRotation = rotation;

			if (lightPitch != cachedLightPitch)
				cachedLightPitch = lightPitch;

			if (lightYaw != cachedLightYaw)
				cachedLightYaw = lightYaw;

			if (cachedLightAmbientColor[0] != lightAmbientColor[0] || cachedLightAmbientColor[1] != lightAmbientColor[1] || cachedLightAmbientColor[2] != lightAmbientColor[2])
				cachedLightAmbientColor = lightAmbientColor;

			if (cachedLightDiffuseColor[0] != lightDiffuseColor[0] || cachedLightDiffuseColor[1] != lightDiffuseColor[1] || cachedLightDiffuseColor[2] != lightDiffuseColor[2])
				cachedLightDiffuseColor = lightDiffuseColor;

			if (cameraAngle != cachedCameraAngle)
				cachedCameraAngle = cameraAngle;

			if (cachedVoxel == null)
				return;

			var animation = new ModelAnimation(
				cachedVoxel,
				() => WVec.Zero,
				() => cachedRotation,
				() => false,
				() => 0,
				true);

			var animations = new ModelAnimation[] { animation };

			ModelPreview preview = new ModelPreview(
				new ModelAnimation[] { animation }, WVec.Zero, 0,
				cachedScale,
				new WAngle(cachedLightPitch),
				new WAngle(cachedLightYaw),
				cachedLightAmbientColor,
				cachedLightDiffuseColor,
				cachedCameraAngle,
				paletteReference,
				paletteReferenceNormals,
				paletteReferenceShadow);

			var screenBounds = animation.ScreenBounds(WPos.Zero, WorldRenderer, scale);
			IdealPreviewSize = new int2(screenBounds.Width, screenBounds.Height);
			var origin = RenderOrigin + new int2(RenderBounds.Size.Width / 2, RenderBounds.Size.Height / 2);

			var camera = new WRot(WAngle.Zero, cachedCameraAngle - new WAngle(256), new WAngle(256));
			var modelRenderable = new UIModelRenderable(
				animations, WPos.Zero, origin, 0, camera, scale,
				WRot.None, cachedLightAmbientColor, cachedLightDiffuseColor,
				paletteReferencePlayer, paletteReferenceNormals, paletteReferenceShadow);

			renderable = modelRenderable.PrepareRender(WorldRenderer);
		}
	}
}
