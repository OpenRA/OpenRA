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
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ActorPreviewWidget : Widget, IEditActorInits
	{
		public bool Animate = false;
		public bool Center = false;
		public bool RecalculateBounds = true;

		public float Scale { get; private set; } = 1f;
		public void SetScale(float scale)
		{
			if (RecalculateBounds)
			{
				if (Center)
					previewOffset = previewPos - IdealPreviewSize / 2;
				else
				{
					Bounds.Width = (int)(scale * IdealPreviewSize.X);
					Bounds.Height = (int)(scale * IdealPreviewSize.Y);
				}
			}

			Scale = scale;
		}

		readonly WorldRenderer worldRenderer;
		readonly WorldViewportSizes viewportSizes;

		public IActorPreview[] Preview { get; private set; } = Array.Empty<IActorPreview>();
		ActorReference reference;
		ActorInfo info;

		int2 previewOffset;
		public int2 IdealPreviewSize { get; private set; }
		int2 previewPos;
		public SequenceSet Sequences;

		[ObjectCreator.UseCtor]
		public ActorPreviewWidget(ModData modData, WorldRenderer worldRenderer)
		{
			viewportSizes = modData.Manifest.Get<WorldViewportSizes>();
			this.worldRenderer = worldRenderer;
		}

		protected ActorPreviewWidget(ActorPreviewWidget other)
			: base(other)
		{
			Preview = other.Preview;
			worldRenderer = other.worldRenderer;
			viewportSizes = other.viewportSizes;
		}

		public override Widget Clone() { return new ActorPreviewWidget(this); }

		public void SetPreview(ActorInfo actor, TypeDictionary td)
		{
			reference = new ActorReference(actor.Name.ToLowerInvariant(), td);
			info = actor;
			GeneratePreviews();
		}

		public void RemoveInit<T>(TraitInfo info) where T : ActorInit
		{
			var original = GetInitOrDefault<T>(info);
			if (original != null)
				reference.Remove(original);
			GeneratePreviews();
		}

		public void ReplaceInit<T>(T init) where T : ActorInit, ISingleInstanceInit
		{
			var original = reference.GetOrDefault<T>();
			if (original != null)
				reference.Remove(original);

			reference.Add(init);
			GeneratePreviews();
		}

		public void ReplaceInit<T>(T init, TraitInfo info) where T : ActorInit
		{
			var original = GetInitOrDefault<T>(info);
			if (original != null)
				reference.Remove(original);

			reference.Add(init);
			GeneratePreviews();
		}

		public T GetInitOrDefault<T>(TraitInfo info) where T : ActorInit
		{
			return reference.GetOrDefault<T>(info);
		}

		public T GetInitOrDefault<T>() where T : ActorInit, ISingleInstanceInit
		{
			return reference.GetOrDefault<T>();
		}

		void GeneratePreviews()
		{
			var init = new ActorPreviewInitializer(info, reference, worldRenderer, Sequences);
			Preview = info.TraitInfos<IRenderActorPreviewInfo>()
				.SelectMany(rpi => rpi.RenderPreview(init))
				.ToArray();

			// Calculate the preview bounds.
			if (RecalculateBounds)
			{
				var r = Preview.SelectMany(p => p.ScreenBounds(worldRenderer, WPos.Zero));
				var b = r.Union();
				IdealPreviewSize = new int2((int)(b.Width * viewportSizes.DefaultScale), (int)(b.Height * viewportSizes.DefaultScale));
				previewPos = -new int2((int)(b.Left * viewportSizes.DefaultScale), (int)(b.Top * viewportSizes.DefaultScale));
				previewOffset = previewPos - IdealPreviewSize / 2;
			}
			else
				previewOffset = int2.Zero;
		}

		IFinalizedRenderable[] renderables;
		public override void PrepareRenderables()
		{
			var scale = Scale * viewportSizes.DefaultScale;
			var origin = RenderOrigin + previewOffset + new int2(RenderBounds.Size.Width / 2, RenderBounds.Size.Height / 2);

			renderables = Preview
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
				foreach (var p in Preview)
					p.Tick();
		}
	}
}
