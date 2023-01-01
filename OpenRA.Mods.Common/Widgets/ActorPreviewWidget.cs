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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ActorPreviewWidget : Widget
	{
		public bool Animate = false;
		public Func<float> GetScale = () => 1f;

		readonly WorldRenderer worldRenderer;
		readonly WorldViewportSizes viewportSizes;

		IActorPreview[] preview = Array.Empty<IActorPreview>();
		public int2 PreviewOffset { get; private set; }
		public int2 IdealPreviewSize { get; private set; }

		[ObjectCreator.UseCtor]
		public ActorPreviewWidget(ModData modData, WorldRenderer worldRenderer)
		{
			viewportSizes = modData.Manifest.Get<WorldViewportSizes>();
			this.worldRenderer = worldRenderer;
		}

		protected ActorPreviewWidget(ActorPreviewWidget other)
			: base(other)
		{
			preview = other.preview;
			worldRenderer = other.worldRenderer;
			viewportSizes = other.viewportSizes;
		}

		public override Widget Clone() { return new ActorPreviewWidget(this); }

		public void SetPreview(ActorInfo actor, TypeDictionary td)
		{
			var init = new ActorPreviewInitializer(actor, worldRenderer, td);
			preview = actor.TraitInfos<IRenderActorPreviewInfo>()
				.SelectMany(rpi => rpi.RenderPreview(init))
				.ToArray();

			// Calculate the preview bounds
			var r = preview.SelectMany(p => p.ScreenBounds(worldRenderer, WPos.Zero));
			var b = r.Union();
			IdealPreviewSize = new int2((int)(b.Width * viewportSizes.DefaultScale), (int)(b.Height * viewportSizes.DefaultScale));
			PreviewOffset = -new int2((int)(b.Left * viewportSizes.DefaultScale), (int)(b.Top * viewportSizes.DefaultScale)) - IdealPreviewSize / 2;
		}

		IFinalizedRenderable[] renderables;
		public override void PrepareRenderables()
		{
			var scale = GetScale() * viewportSizes.DefaultScale;
			var origin = RenderOrigin + PreviewOffset + new int2(RenderBounds.Size.Width / 2, RenderBounds.Size.Height / 2);

			renderables = preview
				.SelectMany(p => p.RenderUI(worldRenderer, origin, scale))
				.OrderBy(WorldRenderer.RenderableZPositionComparisonKey)
				.Select(r => r.PrepareRender(worldRenderer))
				.ToArray();
		}

		public override void Draw()
		{
			Game.Renderer.EnableAntialiasingFilter();
			foreach (var r in renderables)
				r.Render(worldRenderer);
			Game.Renderer.DisableAntialiasingFilter();
		}

		public override void Tick()
		{
			if (Animate)
				foreach (var p in preview)
					p.Tick();
		}
	}
}
