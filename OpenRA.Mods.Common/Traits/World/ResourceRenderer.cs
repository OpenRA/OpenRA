﻿#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Visualizes the state of the `ResourceLayer`.", " Attach this to the world actor.")]
	public class ResourceRendererInfo : TraitInfo, Requires<IResourceLayerInfo>
	{
		[FieldLoader.Require]
		[Desc("Only render these ResourceType names.")]
		public readonly string[] RenderTypes = null;

		public override object Create(ActorInitializer init) { return new ResourceRenderer(init.Self, this); }
	}

	public class ResourceRenderer : IResourceRenderer, IWorldLoaded, IRenderOverlay, ITickRender, INotifyActorDisposing
	{
		protected readonly IResourceLayer ResourceLayer;
		protected readonly CellLayer<RendererCellContents> RenderContent;
		protected readonly ResourceRendererInfo Info;
		protected readonly Dictionary<string, ResourceType> ResourceInfo;

		readonly HashSet<CPos> dirty = new HashSet<CPos>();
		readonly Queue<CPos> cleanDirty = new Queue<CPos>();
		TerrainSpriteLayer shadowLayer;
		TerrainSpriteLayer spriteLayer;

		public ResourceRenderer(Actor self, ResourceRendererInfo info)
		{
			Info = info;
			ResourceLayer = self.Trait<IResourceLayer>();
			ResourceLayer.CellChanged += AddDirtyCell;
			ResourceInfo = self.TraitsImplementing<ResourceType>()
				.ToDictionary(r => r.Info.Type, r => r);

			RenderContent = new CellLayer<RendererCellContents>(self.World.Map);
		}

		void AddDirtyCell(CPos cell, string resourceType)
		{
			if (resourceType == null || Info.RenderTypes.Contains(resourceType))
				dirty.Add(cell);
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			foreach (var resourceType in ResourceInfo.Values)
			{
				if (spriteLayer == null)
				{
					var first = resourceType.Variants.First().Value.GetSprite(0);
					var emptySprite = new Sprite(first.Sheet, Rectangle.Empty, TextureChannel.Alpha);
					spriteLayer = new TerrainSpriteLayer(w, wr, emptySprite, first.BlendMode, wr.World.Type != WorldType.Editor);
				}

				if (shadowLayer == null)
				{
					var firstWithShadow = resourceType.Variants.Values.FirstOrDefault(v => v.ShadowStart > 0);
					if (firstWithShadow != null)
					{
						var first = firstWithShadow.GetShadow(0, WAngle.Zero);
						var emptySprite = new Sprite(first.Sheet, Rectangle.Empty, TextureChannel.Alpha);
						shadowLayer = new TerrainSpriteLayer(w, wr, emptySprite, first.BlendMode, wr.World.Type != WorldType.Editor);
					}
				}

				// All resources must share a blend mode
				var sprites = resourceType.Variants.Values.SelectMany(v => Exts.MakeArray(v.Length, x => v.GetSprite(x)));
				if (sprites.Any(s => s.BlendMode != spriteLayer.BlendMode))
					throw new InvalidDataException("Resource sprites specify different blend modes. "
						+ "Try using different ResourceRenderer traits for resource types that use different blend modes.");
			}

			// Initialize the RenderContent with the initial map state
			// because the shroud may not be enabled.
			foreach (var cell in w.Map.AllCells)
			{
				var type = ResourceLayer.GetResource(cell).Type;
				if (type != null && Info.RenderTypes.Contains(type))
				{
					var resourceContent = ResourceLayer.GetResource(cell);
					if (!ResourceInfo.TryGetValue(resourceContent.Type, out var resourceType))
						continue;

					var rendererCellContents = new RendererCellContents(ChooseRandomVariant(resourceType), resourceType, resourceContent.Density);
					RenderContent[cell] = rendererCellContents;
					UpdateRenderedSprite(cell, rendererCellContents);
				}
			}
		}

		protected void UpdateSpriteLayers(CPos cell, ISpriteSequence sequence, int frame, PaletteReference palette)
		{
			// resource.Type is meaningless (and may be null) if resource.Sequence is null
			if (sequence != null)
			{
				shadowLayer?.Update(cell, sequence.GetShadow(frame, WAngle.Zero), palette, 1f, 1f, sequence.IgnoreWorldTint);
				spriteLayer.Update(cell, sequence, palette, frame);
			}
			else
			{
				shadowLayer?.Clear(cell);
				spriteLayer.Clear(cell);
			}
		}

		void IRenderOverlay.Render(WorldRenderer wr)
		{
			shadowLayer?.Draw(wr.Viewport);
			spriteLayer.Draw(wr.Viewport);
		}

		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			foreach (var cell in dirty)
			{
				if (!ResourceLayer.IsVisible(cell))
					continue;

				var resourceContent = ResourceLayer.GetResource(cell);
				if (resourceContent.Density > 0)
				{
					var cellContents = RenderContent[cell];
					var resourceData = ResourceInfo[resourceContent.Type];
					var variant = cellContents.Variant;
					if (cellContents.Variant == null || cellContents.Type.Info.Type != resourceContent.Type)
						variant = ChooseRandomVariant(resourceData);

					var rendererCellContents = new RendererCellContents(variant, resourceData, resourceContent.Density);
					RenderContent[cell] = rendererCellContents;

					UpdateRenderedSprite(cell, rendererCellContents);
				}
				else
				{
					var rendererCellContents = RendererCellContents.Empty;
					RenderContent[cell] = rendererCellContents;
					UpdateRenderedSprite(cell, rendererCellContents);
				}

				cleanDirty.Enqueue(cell);
			}

			while (cleanDirty.Count > 0)
				dirty.Remove(cleanDirty.Dequeue());
		}

		protected virtual void UpdateRenderedSprite(CPos cell, RendererCellContents content)
		{
			var density = content.Density;
			var type = content.Type;
			if (content.Density > 0)
			{
				// The call chain for this method (that starts with AddDirtyCell()) guarantees
				// that the new content type would still be suitable for this renderer,
				// but that is a bit too fragile to rely on in case the code starts changing.
				if (!Info.RenderTypes.Contains(type.Info.Type))
					return;

				var sprites = type.Variants[content.Variant];
				var maxDensity = type.Info.MaxDensity;
				var frame = int2.Lerp(0, sprites.Length - 1, density, maxDensity);

				UpdateSpriteLayers(cell, sprites, frame, type.Palette);
			}
			else
				UpdateSpriteLayers(cell, null, 0, null);
		}

		bool disposed;
		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (disposed)
				return;

			shadowLayer?.Dispose();
			spriteLayer.Dispose();

			ResourceLayer.CellChanged -= AddDirtyCell;

			disposed = true;
		}

		protected virtual string ChooseRandomVariant(ResourceType t)
		{
			return t.Variants.Keys.Random(Game.CosmeticRandom);
		}

		protected virtual string GetRenderedResourceType(CPos cell) { return RenderContent[cell].Type.Info.Type; }

		protected virtual string GetRenderedResourceTooltip(CPos cell) { return RenderContent[cell].Type?.Info.Name; }

		IEnumerable<string> IResourceRenderer.ResourceTypes => ResourceInfo.Keys;

		string IResourceRenderer.GetRenderedResourceType(CPos cell) { return GetRenderedResourceType(cell); }

		string IResourceRenderer.GetRenderedResourceTooltip(CPos cell) { return GetRenderedResourceTooltip(cell); }

		IEnumerable<IRenderable> IResourceRenderer.RenderUIPreview(WorldRenderer wr, string resourceType, int2 origin, float scale)
		{
			if (!ResourceInfo.TryGetValue(resourceType, out var resourceInfo))
				yield break;

			var sequence = resourceInfo.Variants.First().Value;
			var sprite = sequence.GetSprite(sequence.Length - 1);
			var shadow = sequence.GetShadow(sequence.Length - 1, WAngle.Zero);
			var palette = resourceInfo.Palette;

			if (shadow != null)
				yield return new UISpriteRenderable(shadow, WPos.Zero, origin, 0, palette, scale);

			yield return new UISpriteRenderable(sprite, WPos.Zero, origin, 0, palette, scale);
		}

		IEnumerable<IRenderable> IResourceRenderer.RenderPreview(WorldRenderer wr, string resourceType, WPos origin)
		{
			if (!ResourceInfo.TryGetValue(resourceType, out var resourceInfo))
				yield break;

			var sequence = resourceInfo.Variants.First().Value;
			var sprite = sequence.GetSprite(sequence.Length - 1);
			var shadow = sequence.GetShadow(sequence.Length - 1, WAngle.Zero);
			var alpha = sequence.GetAlpha(sequence.Length - 1);
			var palette = resourceInfo.Palette;
			var tintModifiers = sequence.IgnoreWorldTint ? TintModifiers.IgnoreWorldTint : TintModifiers.None;

			if (shadow != null)
				yield return new SpriteRenderable(shadow, origin, WVec.Zero, 0, palette, sequence.Scale, alpha, float3.Ones, tintModifiers, false);

			yield return new SpriteRenderable(sprite, origin, WVec.Zero, 0, palette, sequence.Scale, alpha, float3.Ones, tintModifiers, false);
		}

		public struct RendererCellContents
		{
			public readonly string Variant;
			public readonly ResourceType Type;
			public readonly int Density;

			public static readonly RendererCellContents Empty = default;

			public RendererCellContents(string variant, ResourceType type, int density)
			{
				Variant = variant;
				Type = type;
				Density = density;
			}
		}
	}
}
