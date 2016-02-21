#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
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

		IActorPreview[] preview = new IActorPreview[0];
		public int2 PreviewOffset { get; private set; }
		public int2 IdealPreviewSize { get; private set; }

		[ObjectCreator.UseCtor]
		public ActorPreviewWidget(WorldRenderer worldRenderer)
		{
			this.worldRenderer = worldRenderer;
		}

		protected ActorPreviewWidget(ActorPreviewWidget other)
			: base(other)
		{
			preview = other.preview;
			worldRenderer = other.worldRenderer;
		}

		public override Widget Clone() { return new ActorPreviewWidget(this); }

		public void SetPreview(ActorInfo actor, TypeDictionary td)
		{
			var init = new ActorPreviewInitializer(actor, worldRenderer, td);
			preview = actor.TraitInfos<IRenderActorPreviewInfo>()
				.SelectMany(rpi => rpi.RenderPreview(init))
				.ToArray();

			// Calculate the preview bounds
			PreviewOffset = int2.Zero;
			IdealPreviewSize = int2.Zero;

			var r = preview
				.SelectMany(p => p.Render(worldRenderer, WPos.Zero))
				.OrderBy(WorldRenderer.RenderableScreenZPositionComparisonKey)
				.Select(rr => rr.PrepareRender(worldRenderer));

			if (r.Any())
			{
				var b = r.First().ScreenBounds(worldRenderer);
				foreach (var rr in r.Skip(1))
					b = Rectangle.Union(b, rr.ScreenBounds(worldRenderer));

				IdealPreviewSize = new int2(b.Width, b.Height);
				PreviewOffset = -new int2(b.Left, b.Top) - IdealPreviewSize / 2;
			}
		}

		IFinalizedRenderable[] renderables;
		public override void PrepareRenderables()
		{
			renderables = preview
				.SelectMany(p => p.Render(worldRenderer, WPos.Zero))
				.OrderBy(WorldRenderer.RenderableScreenZPositionComparisonKey)
				.Select(r => r.PrepareRender(worldRenderer))
				.ToArray();
		}

		public override void Draw()
		{
			// HACK: The split between world and UI shaders is a giant PITA because it isn't
			// feasible to maintain two parallel sets of renderables for the two cases.
			// Instead, we temporarily hijack the world rendering context and set the position
			// and zoom values to give the desired screen position and size.
			var scale = GetScale();
			var origin = RenderOrigin + new int2(RenderBounds.Size.Width / 2, RenderBounds.Size.Height / 2);

			// The scale affects world -> screen transform, which we don't want when drawing the (fixed) UI.
			if (scale != 1f)
				origin = (1f / scale * origin.ToFloat2()).ToInt2();

			Game.Renderer.Flush();
			Game.Renderer.SetViewportParams(-origin - PreviewOffset, scale);

			foreach (var r in renderables)
				r.Render(worldRenderer);

			Game.Renderer.Flush();
			Game.Renderer.SetViewportParams(worldRenderer.Viewport.TopLeft, worldRenderer.Viewport.Zoom);
		}

		public override void Tick()
		{
			if (Animate)
				foreach (var p in preview)
					p.Tick();
		}
	}
}
