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
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Graphics;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets
{
	public class ModelWidget : Widget, IModelWidget
	{
		public string Palette = "terrain";
		public string PlayerPalette = "player";
		public string NormalsPalette = "normals";
		public string ShadowPalette = "shadow";
		public float Scale = 10f;
		public int LightPitch = 142;
		public int LightYaw = 682;
		public float[] LightAmbientColor = [0.6f, 0.6f, 0.6f];
		public float[] LightDiffuseColor = [0.4f, 0.4f, 0.4f];
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

		string IModelWidget.Palette => GetPalette();
		float IModelWidget.Scale => GetScale();

		void IModelWidget.Setup(Func<bool> isVisible, Func<string> getPalette, Func<string> getPlayerPalette,
			Func<float> getScale, Func<IModel> getVoxel, Func<WRot> getRotation)
		{
			IsVisible = isVisible;
			GetPalette = getPalette;
			GetPlayerPalette = getPlayerPalette;
			GetScale = getScale;
			GetVoxel = getVoxel;
			GetRotation = getRotation;
		}

		public override ModelWidget Clone()
		{
			return new ModelWidget(this);
		}

		string cachedPalette;
		string cachedPlayerPalette;
		string cachedNormalsPalette;
		string cachedShadowPalette;
		int cachedLightPitch;
		int cachedLightYaw;
		WRot cachedLightSource;
		WAngle cachedCameraAngle;
		PaletteReference paletteReference;
		WRot cachedCameraRotation;
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
			if (voxel == null)
				return;

			var palette = GetPalette();
			var playerPalette = GetPlayerPalette();
			if (string.IsNullOrEmpty(palette) && string.IsNullOrEmpty(playerPalette))
				return;

			var normalsPalette = GetNormalsPalette();
			var shadowPalette = GetShadowPalette();
			var scale = GetScale();
			var rotation = GetRotation();
			var lightAmbientColor = GetLightAmbientColor();
			var lightDiffuseColor = GetLightDiffuseColor();
			var lightPitch = GetLightPitch();
			var lightYaw = GetLightYaw();
			var cameraAngle = GetCameraAngle();

			if (palette != cachedPalette)
			{
				paletteReference = WorldRenderer.Palette(playerPalette);
				cachedPalette = palette;
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

			if (lightPitch != cachedLightPitch || lightYaw != cachedLightYaw)
			{
				cachedLightPitch = lightPitch;
				cachedLightYaw = lightYaw;
				cachedLightSource = new WRot(WAngle.Zero, new WAngle(256 - lightPitch), new WAngle(lightYaw));
			}

			if (cameraAngle != cachedCameraAngle)
			{
				cachedCameraAngle = cameraAngle;
				cachedCameraRotation = new WRot(WAngle.Zero, cameraAngle - new WAngle(256), new WAngle(256));
			}

			var animation = new ModelAnimation(
				voxel,
				() => WVec.Zero,
				() => rotation,
				() => false,
				() => 0,
				true);

			var animations = new ModelAnimation[] { animation };

			var screenBounds = animation.ScreenBounds(WPos.Zero, WorldRenderer, scale);
			IdealPreviewSize = new int2(screenBounds.Width, screenBounds.Height);
			var origin = RenderOrigin + new int2(RenderBounds.Size.Width / 2, RenderBounds.Size.Height / 2);

			var renderer = WorldRenderer.World.WorldActor.Trait<ModelRenderer>();
			var modelRenderable = new UIModelRenderable(
				renderer,
				animations, WPos.Zero, origin, 0, cachedCameraRotation, scale,
				cachedLightSource, lightAmbientColor, lightDiffuseColor,
				paletteReferencePlayer ?? paletteReference, paletteReferenceNormals, paletteReferenceShadow);

			renderable = modelRenderable.PrepareRender(WorldRenderer);
		}
	}
}
