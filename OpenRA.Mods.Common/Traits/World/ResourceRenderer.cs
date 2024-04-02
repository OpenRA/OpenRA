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
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Visualizes the state of the `ResourceLayer`.", " Attach this to the world actor.")]
	public class ResourceRendererInfo : TraitInfo, Requires<IResourceLayerInfo>, IMapPreviewSignatureInfo
	{
		public class ResourceTypeInfo
		{
			[Desc("Sequence image that holds the different variants.")]
			public readonly string Image = "resources";

			[FieldLoader.Require]
			[SequenceReference(nameof(Image))]
			[Desc("Randomly chosen image sequences.")]
			public readonly string[] Sequences = Array.Empty<string>();

			[PaletteReference]
			[Desc("Palette used for rendering the resource sprites.")]
			public readonly string Palette = TileSet.TerrainPaletteInternalName;

			[FieldLoader.Require]
			[Desc("Resource name used by tooltips.")]
			[TranslationReference]
			public readonly string Name = null;

			public ResourceTypeInfo(MiniYaml yaml)
			{
				FieldLoader.Load(this, yaml);
			}
		}

		[FieldLoader.LoadUsing(nameof(LoadResourceTypes))]
		public readonly Dictionary<string, ResourceTypeInfo> ResourceTypes = null;

		// Copied from ResourceLayerInfo
		protected static object LoadResourceTypes(MiniYaml yaml)
		{
			var ret = new Dictionary<string, ResourceTypeInfo>();
			var resources = yaml.NodeWithKeyOrDefault("ResourceTypes");
			if (resources != null)
				foreach (var r in resources.Value.Nodes)
					ret[r.Key] = new ResourceTypeInfo(r.Value);

			return ret;
		}

		void IMapPreviewSignatureInfo.PopulateMapPreviewSignatureCells(Map map, ActorInfo ai, ActorReference s, List<(MPos Uv, Color Color)> destinationBuffer)
		{
			var resourceLayer = ai.TraitInfoOrDefault<IResourceLayerInfo>();
			if (resourceLayer == null)
				return;

			var terrainInfo = map.Rules.TerrainInfo;
			var colors = new Dictionary<byte, Color>();
			foreach (var r in ResourceTypes.Keys)
			{
				if (!resourceLayer.TryGetResourceIndex(r, out var resourceIndex) || !resourceLayer.TryGetTerrainType(r, out var terrainType))
					continue;

				var info = terrainInfo.TerrainTypes[terrainInfo.GetTerrainIndex(terrainType)];
				colors.Add(resourceIndex, info.Color);
			}

			for (var i = 0; i < map.MapSize.X; i++)
			{
				for (var j = 0; j < map.MapSize.Y; j++)
				{
					var cell = new MPos(i, j);
					if (colors.TryGetValue(map.Resources[cell].Type, out var color))
						destinationBuffer.Add((cell, color));
				}
			}
		}

		public override object Create(ActorInitializer init) { return new ResourceRenderer(init.Self, this); }
	}

	public class ResourceRenderer : IResourceRenderer, IWorldLoaded, IRenderOverlay, ITickRender, INotifyActorDisposing, IRadarTerrainLayer
	{
		protected readonly ResourceRendererInfo Info;
		protected readonly IResourceLayer ResourceLayer;
		protected readonly CellLayer<RendererCellContents> RenderContents;
		protected readonly Dictionary<string, Dictionary<string, ISpriteSequence>> Variants = new();
		protected readonly World World;

		readonly HashSet<CPos> dirty = new();
		readonly Queue<CPos> cleanDirty = new();
		TerrainSpriteLayer shadowLayer;
		TerrainSpriteLayer spriteLayer;
		bool disposed;

		public ResourceRenderer(Actor self, ResourceRendererInfo info)
		{
			Info = info;
			World = self.World;
			ResourceLayer = self.Trait<IResourceLayer>();
			ResourceLayer.CellChanged += AddDirtyCell;
			RenderContents = new CellLayer<RendererCellContents>(self.World.Map);
		}

		void AddDirtyCell(CPos cell, string resourceType)
		{
			if (resourceType == null || Info.ResourceTypes.ContainsKey(resourceType))
				dirty.Add(cell);
		}

		protected virtual void WorldLoaded(World w, WorldRenderer wr)
		{
			var sequences = w.Map.Sequences;
			foreach (var kv in Info.ResourceTypes)
			{
				var resourceInfo = kv.Value;
				var resourceVariants = resourceInfo.Sequences
					.ToDictionary(v => v, v => sequences.GetSequence(resourceInfo.Image, v));
				Variants.Add(kv.Key, resourceVariants);

				if (spriteLayer == null)
				{
					var first = resourceVariants.First().Value.GetSprite(0);
					var emptySprite = new Sprite(first.Sheet, Rectangle.Empty, TextureChannel.Alpha);
					spriteLayer = new TerrainSpriteLayer(w, wr, emptySprite, first.BlendMode, wr.World.Type != WorldType.Editor);
				}

				if (shadowLayer == null)
				{
					var firstShadow = resourceVariants.Values
						.Select(v => v.GetShadow(0, WAngle.Zero))
						.FirstOrDefault(s => s != null);
					if (firstShadow != null)
					{
						var emptySprite = new Sprite(firstShadow.Sheet, Rectangle.Empty, TextureChannel.Alpha);
						shadowLayer = new TerrainSpriteLayer(w, wr, emptySprite, firstShadow.BlendMode, wr.World.Type != WorldType.Editor);
					}
				}

				// All resources must share a blend mode
				var sprites = resourceVariants.Values.SelectMany(v => Exts.MakeArray(v.Length, x => v.GetSprite(x)));
				if (sprites.Any(s => s.BlendMode != spriteLayer.BlendMode))
					throw new InvalidDataException("Resource sprites specify different blend modes. "
						+ "Try using different ResourceRenderer traits for resource types that use different blend modes.");
			}

			// Initialize the RenderContent with the initial map state so it is visible
			// through the fog with the Explored Map option enabled
			foreach (var cell in w.Map.AllCells)
			{
				var resource = ResourceLayer.GetResource(cell);
				var rendererCellContents = CreateRenderCellContents(wr, resource, cell);
				if (rendererCellContents.Type != null)
				{
					RenderContents[cell] = rendererCellContents;
					UpdateRenderedSprite(cell, rendererCellContents);
				}
			}
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr) { WorldLoaded(w, wr); }

		protected RendererCellContents CreateRenderCellContents(WorldRenderer wr, ResourceLayerContents contents, CPos cell)
		{
			if (contents.Type != null && contents.Density > 0 && Info.ResourceTypes.TryGetValue(contents.Type, out var resourceInfo))
				return new RendererCellContents(contents.Type, contents.Density, resourceInfo, ChooseVariant(contents.Type, cell), wr.Palette(resourceInfo.Palette));

			return RendererCellContents.Empty;
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

				var rendererCellContents = RendererCellContents.Empty;
				var contents = ResourceLayer.GetResource(cell);
				if (contents.Density > 0)
				{
					rendererCellContents = RenderContents[cell];

					// Contents are the same, so just update the density
					if (rendererCellContents.Type == contents.Type)
						rendererCellContents = new RendererCellContents(rendererCellContents, contents.Density);
					else
						rendererCellContents = CreateRenderCellContents(wr, contents, cell);
				}

				RenderContents[cell] = rendererCellContents;
				UpdateRenderedSprite(cell, rendererCellContents);
				cleanDirty.Enqueue(cell);
			}

			while (cleanDirty.Count > 0)
				dirty.Remove(cleanDirty.Dequeue());
		}

		protected virtual void UpdateRenderedSprite(CPos cell, RendererCellContents content)
		{
			if (content.Density > 0)
			{
				var maxDensity = ResourceLayer.GetMaxDensity(content.Type);
				var frame = int2.Lerp(0, content.Sequence.Length - 1, content.Density, maxDensity);
				UpdateSpriteLayers(cell, content.Sequence, frame, content.Palette);
			}
			else
				UpdateSpriteLayers(cell, null, 0, null);
		}

		protected virtual void Disposing(Actor self)
		{
			if (disposed)
				return;

			shadowLayer?.Dispose();
			spriteLayer.Dispose();

			ResourceLayer.CellChanged -= AddDirtyCell;

			disposed = true;
		}

		void INotifyActorDisposing.Disposing(Actor self) { Disposing(self); }

		protected virtual ISpriteSequence ChooseVariant(string resourceType, CPos cell)
		{
			return Variants[resourceType].Values.Random(World.LocalRandom);
		}

		protected virtual string GetRenderedResourceType(CPos cell) { return RenderContents[cell].Type; }

		protected virtual string GetRenderedResourceTooltip(CPos cell)
		{
			var info = RenderContents[cell].Info;
			if (info == null)
				return null;

			return TranslationProvider.GetString(info.Name);
		}

		IEnumerable<string> IResourceRenderer.ResourceTypes => Info.ResourceTypes.Keys;

		string IResourceRenderer.GetRenderedResourceType(CPos cell) { return GetRenderedResourceType(cell); }

		string IResourceRenderer.GetRenderedResourceTooltip(CPos cell) { return GetRenderedResourceTooltip(cell); }

		IEnumerable<IRenderable> IResourceRenderer.RenderUIPreview(WorldRenderer wr, string resourceType, int2 origin, float scale)
		{
			if (!Variants.TryGetValue(resourceType, out var variant))
				yield break;

			if (!Info.ResourceTypes.TryGetValue(resourceType, out var resourceInfo))
				yield break;

			var sequence = variant.First().Value;
			var sprite = sequence.GetSprite(sequence.Length - 1);
			var shadow = sequence.GetShadow(sequence.Length - 1, WAngle.Zero);
			var palette = wr.Palette(resourceInfo.Palette);

			if (shadow != null)
				yield return new UISpriteRenderable(shadow, WPos.Zero, origin, 0, palette, scale);

			yield return new UISpriteRenderable(sprite, WPos.Zero, origin, 0, palette, scale);
		}

		IEnumerable<IRenderable> IResourceRenderer.RenderPreview(WorldRenderer wr, string resourceType, WPos origin)
		{
			if (!Variants.TryGetValue(resourceType, out var variant))
				yield break;

			if (!Info.ResourceTypes.TryGetValue(resourceType, out var resourceInfo))
				yield break;

			var sequence = variant.First().Value;
			var sprite = sequence.GetSprite(sequence.Length - 1);
			var shadow = sequence.GetShadow(sequence.Length - 1, WAngle.Zero);
			var alpha = sequence.GetAlpha(sequence.Length - 1);
			var palette = wr.Palette(resourceInfo.Palette);
			var tintModifiers = sequence.IgnoreWorldTint ? TintModifiers.IgnoreWorldTint : TintModifiers.None;

			if (shadow != null)
				yield return new SpriteRenderable(shadow, origin, WVec.Zero, 0, palette, sequence.Scale, alpha, float3.Ones, tintModifiers, false);

			yield return new SpriteRenderable(sprite, origin, WVec.Zero, 0, palette, sequence.Scale, alpha, float3.Ones, tintModifiers, false);
		}

		event Action<CPos> IRadarTerrainLayer.CellEntryChanged
		{
			add => RenderContents.CellEntryChanged += value;
			remove => RenderContents.CellEntryChanged -= value;
		}

		bool IRadarTerrainLayer.TryGetTerrainColorPair(MPos uv, out (Color Left, Color Right) value)
		{
			value = default;

			var type = RenderContents[uv].Type;
			if (type == null)
				return false;

			if (!ResourceLayer.Info.TryGetTerrainType(type, out var terrainType))
				return false;

			var terrainInfo = World.Map.Rules.TerrainInfo;
			var info = terrainInfo.TerrainTypes[terrainInfo.GetTerrainIndex(terrainType)];

			value = (info.Color, info.Color);
			return true;
		}

		public readonly struct RendererCellContents
		{
			public readonly string Type;
			public readonly ResourceRendererInfo.ResourceTypeInfo Info;
			public readonly ISpriteSequence Sequence;
			public readonly PaletteReference Palette;
			public readonly int Density;

			public static readonly RendererCellContents Empty = default;

			public RendererCellContents(string resourceType, int density, ResourceRendererInfo.ResourceTypeInfo info, ISpriteSequence sequence, PaletteReference palette)
			{
				Type = resourceType;
				Density = density;
				Info = info;
				Sequence = sequence;
				Palette = palette;
			}

			public RendererCellContents(RendererCellContents contents, int density)
			{
				Type = contents.Type;
				Density = density;
				Info = contents.Info;
				Sequence = contents.Sequence;
				Palette = contents.Palette;
			}
		}
	}
}
