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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Visualizes the state of the `ResourceLayer`.", " Attach this to the world actor.")]
	public class ResourceRendererInfo : TraitInfo, Requires<ResourceLayerInfo>
	{
		[FieldLoader.Require]
		[Desc("Only render these ResourceType names.")]
		public readonly string[] RenderTypes = null;

		public override object Create(ActorInitializer init) { return new ResourceRenderer(init.Self, this); }
	}

	public class ResourceRenderer : IWorldLoaded, IRenderOverlay, ITickRender, INotifyActorDisposing
	{
		protected readonly ResourceLayer ResourceLayer;
		protected readonly CellLayer<RendererCellContents> RenderContent;
		protected readonly ResourceRendererInfo Info;

		readonly HashSet<CPos> dirty = new HashSet<CPos>();
		readonly Queue<CPos> cleanDirty = new Queue<CPos>();
		readonly Dictionary<PaletteReference, TerrainSpriteLayer> spriteLayers = new Dictionary<PaletteReference, TerrainSpriteLayer>();

		public ResourceRenderer(Actor self, ResourceRendererInfo info)
		{
			Info = info;

			ResourceLayer = self.Trait<ResourceLayer>();
			ResourceLayer.CellChanged += AddDirtyCell;

			RenderContent = new CellLayer<RendererCellContents>(self.World.Map);
		}

		void AddDirtyCell(CPos cell, ResourceType resType)
		{
			if (resType == null || Info.RenderTypes.Contains(resType.Info.Type))
				dirty.Add(cell);
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			var resources = w.WorldActor.TraitsImplementing<ResourceType>()
				.ToDictionary(r => r.Info.ResourceType, r => r);

			// Build the sprite layer dictionary for rendering resources
			// All resources that have the same palette must also share a sheet and blend mode
			foreach (var r in resources)
			{
				var layer = spriteLayers.GetOrAdd(r.Value.Palette, pal =>
				{
					var first = r.Value.Variants.First().Value.First();
					return new TerrainSpriteLayer(w, wr, first.Sheet, first.BlendMode, pal, wr.World.Type != WorldType.Editor);
				});

				// Validate that sprites are compatible with this layer
				var sheet = layer.Sheet;
				if (r.Value.Variants.Any(kv => kv.Value.Any(s => s.Sheet != sheet)))
					throw new InvalidDataException("Resource sprites span multiple sheets. Try loading their sequences earlier.");

				var blendMode = layer.BlendMode;
				if (r.Value.Variants.Any(kv => kv.Value.Any(s => s.BlendMode != blendMode)))
					throw new InvalidDataException("Resource sprites specify different blend modes. "
						+ "Try using different palettes for resource types that use different blend modes.");
			}

			// Initialize the RenderContent with the initial map state
			// because the shroud may not be enabled.
			foreach (var cell in w.Map.AllCells)
			{
				var type = ResourceLayer.GetResourceType(cell);
				if (type != null && Info.RenderTypes.Contains(type.Info.Type))
				{
					var resourceContent = ResourceLayer.GetResource(cell);
					var rendererCellContents = new RendererCellContents(ChooseRandomVariant(resourceContent.Type), resourceContent.Type, resourceContent.Density);
					RenderContent[cell] = rendererCellContents;
					UpdateRenderedSprite(cell, rendererCellContents);
				}
			}
		}

		protected void UpdateSpriteLayers(CPos cell, Sprite sprite, PaletteReference palette)
		{
			foreach (var kv in spriteLayers)
			{
				// resource.Type is meaningless (and may be null) if resource.Sprite is null
				if (sprite != null && palette == kv.Key)
					kv.Value.Update(cell, sprite);
				else
					kv.Value.Update(cell, null);
			}
		}

		void IRenderOverlay.Render(WorldRenderer wr)
		{
			foreach (var kv in spriteLayers.Values)
				kv.Draw(wr.Viewport);
		}

		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			foreach (var cell in dirty)
			{
				if (self.World.FogObscures(cell))
					continue;

				var resourceContent = ResourceLayer.GetResource(cell);
				if (resourceContent.Density > 0)
				{
					var cellContents = RenderContent[cell];
					var variant = cellContents.Variant;
					if (cellContents.Variant == null || cellContents.Type != resourceContent.Type)
						variant = ChooseRandomVariant(resourceContent.Type);

					var rendererCellContents = new RendererCellContents(variant, resourceContent.Type, resourceContent.Density);
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

				UpdateSpriteLayers(cell, sprites[frame], type.Palette);
			}
			else
				UpdateSpriteLayers(cell, null, null);
		}

		bool disposed;
		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (disposed)
				return;

			foreach (var kv in spriteLayers.Values)
				kv.Dispose();

			ResourceLayer.CellChanged -= AddDirtyCell;

			disposed = true;
		}

		protected virtual string ChooseRandomVariant(ResourceType t)
		{
			return t.Variants.Keys.Random(Game.CosmeticRandom);
		}

		public ResourceType GetRenderedResourceType(CPos cell) { return RenderContent[cell].Type; }

		public struct RendererCellContents
		{
			public readonly string Variant;
			public readonly ResourceType Type;
			public readonly int Density;

			public static readonly RendererCellContents Empty = default(RendererCellContents);

			public RendererCellContents(string variant, ResourceType type, int density)
			{
				Variant = variant;
				Type = type;
				Density = density;
			}
		}
	}
}
