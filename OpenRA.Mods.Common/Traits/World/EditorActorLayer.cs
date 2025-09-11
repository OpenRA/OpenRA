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
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.EditorWorld)]
	[Desc("Required for the map editor to work. Attach this to the world actor.")]
	public class EditorActorLayerInfo : TraitInfo, ICreatePlayersInfo
	{
		[Desc("Size of partition bins (world pixels).")]
		public readonly int BinSize = 250;

		[Desc("Facing of new actors.")]
		public readonly WAngle DefaultActorFacing = new(384);

		void ICreatePlayersInfo.CreateServerPlayers(MapPreview map, Session lobbyInfo, List<GameInformation.Player> players, MersenneTwister playerRandom)
		{
			throw new NotImplementedException("EditorActorLayer must not be defined on the world actor.");
		}

		public override object Create(ActorInitializer init) { return new EditorActorLayer(this); }
	}

	public class EditorActorLayer : IWorldLoaded, ITickRender, IRender, IRadarSignature, ICreatePlayers, IRenderAnnotations
	{
		const string ActorPrefix = "Actor";
		const string PlayerSpawnName = "mpspawn";

		public readonly EditorActorLayerInfo Info;
		readonly List<EditorActorPreview> previews = [];
		readonly HashSet<uint> previewIds = [];

		int2 cellOffset;
		SpatiallyPartitioned<EditorActorPreview> cellMap;
		SpatiallyPartitioned<EditorActorPreview> screenMap;
		WorldRenderer worldRenderer;

		public MapPlayers Players { get; private set; }
		PlayerReference worldOwner;

		public EditorActorLayer(EditorActorLayerInfo info)
		{
			Info = info;
		}

		void ICreatePlayers.CreatePlayers(World w, MersenneTwister playerRandom)
		{
			Players = new MapPlayers(w.Map.PlayerDefinitions);

			worldOwner = Players.Players.Select(kvp => kvp.Value).First(p => !p.Playable && p.OwnsWorld);
			w.SetWorldOwner(new Player(w, null, worldOwner, playerRandom));
		}

		public void WorldLoaded(World world, WorldRenderer wr)
		{
			worldRenderer = wr;

			foreach (var pr in Players.Players.Values)
				wr.UpdatePalettesForPlayer(pr.Name, pr.Color, false);

			cellOffset = new int2(world.Map.AllCells.Min(c => c.X), world.Map.AllCells.Min((c) => c.Y));
			var cellOffsetMax = new int2(world.Map.AllCells.Max(c => c.X), world.Map.AllCells.Max((c) => c.Y));
			var mapCellSize = cellOffsetMax - cellOffset;
			var ts = world.Map.Rules.TerrainInfo.TileSize;
			cellMap = new SpatiallyPartitioned<EditorActorPreview>(
				mapCellSize.X, mapCellSize.Y, Exts.IntegerDivisionRoundingAwayFromZero(Info.BinSize, ts.Width));

			var width = world.Map.MapSize.Width * ts.Width;
			var height = world.Map.MapSize.Height * ts.Height;
			screenMap = new SpatiallyPartitioned<EditorActorPreview>(width, height, Info.BinSize);

			var names = new string[world.Map.ActorDefinitions.Count];
			var references = new List<ActorReference>(world.Map.ActorDefinitions.Count);

			for (var i = 0; i < world.Map.ActorDefinitions.Count; i++)
			{
				var kv = world.Map.ActorDefinitions.ElementAt(i);
				names[i] = kv.Key;
				references.Add(new ActorReference(kv.Value.Value, kv.Value.ToDictionary()));
			}

			AddRange(CollectionsMarshal.AsSpan(references), names);
		}

		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			foreach (var p in previews)
				p.Tick();
		}

		public virtual IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			foreach (var p in PreviewsInScreenBox(wr.Viewport.TopLeft, wr.Viewport.BottomRight))
				foreach (var r in p.Render())
					yield return r;
		}

		IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr)
		{
			// World-actor render traits don't require screen bounds
			yield break;
		}

		public IEnumerable<IRenderable> RenderAnnotations(Actor self, WorldRenderer wr)
		{
			return PreviewsInScreenBox(wr.Viewport.TopLeft, wr.Viewport.BottomRight)
				.SelectMany(p => p.RenderAnnotations());
		}

		bool IRenderAnnotations.SpatiallyPartitionable => false;

		IEnumerable<CPos> OccupiedCells(EditorActorPreview preview)
		{
			// Fallback to the actor's CenterPosition for the ActorMap if it has no Footprint
			if (preview.Footprint.Count == 0)
				return [worldRenderer.World.Map.CellContaining(preview.CenterPosition)];
			return preview.Footprint.Keys;
		}

		PlayerReference GetOrAddOwner(ActorReference reference)
		{
			// If an actor's doesn't have a valid owner transfer ownership to neutral
			var ownerInit = reference.Get<OwnerInit>();
			if (!Players.Players.TryGetValue(ownerInit.InternalName, out var owner))
			{
				owner = worldOwner;
				reference.Remove(ownerInit);
				reference.Add(new OwnerInit(worldOwner.Name));
			}

			return owner;
		}

		public EditorActorPreview Add(ActorReference reference)
		{
			var owner = GetOrAddOwner(reference);
			var preview = new EditorActorPreview(worldRenderer, NextActorName(), reference, owner);
			Add(preview);
			return preview;
		}

		public void AddRange(ReadOnlySpan<ActorReference> references, ReadOnlySpan<string> names)
		{
			if (names.Length != references.Length)
				throw new ArgumentException("Member name count must match reference count.");

			var newPreviews = new EditorActorPreview[names.Length];
			using (new PerfTimer("CreatePreviews"))
			{
				for (var i = 0; i < names.Length; i++)
				{
					var id = names[i];
					var reference = references[i];
					var owner = GetOrAddOwner(reference);

					newPreviews[i] = new EditorActorPreview(worldRenderer, id, reference, owner);
				}
			}

			AddRange(newPreviews);
		}

		public void AddRange(ReadOnlySpan<ActorReference> references)
		{
			AddRange(references, NextActorNames(references.Length));
		}

		public void Add(EditorActorPreview preview)
		{
			previews.Add(preview);
			if (TryGetActorId(preview.ID, out var id))
				previewIds.Add(id);

			if (!preview.Bounds.IsEmpty)
				screenMap.Add(preview, preview.Bounds);

			var cellFootprintBounds = OccupiedCells(preview).Select(
				cell => new Rectangle(cell.X - cellOffset.X, cell.Y - cellOffset.Y, 1, 1)).Union();

			cellMap.Add(preview, cellFootprintBounds);

			preview.AddedToEditor();
			UpdateNeighbours(preview.Footprint);

			if (preview.Type == PlayerSpawnName)
				SyncMultiplayerCount();
		}

		public void AddRange(ReadOnlySpan<EditorActorPreview> newPreviews)
		{
			previews.AddRange(newPreviews);
			previewIds.EnsureCapacity(previews.Count * 2);

			foreach (var preview in newPreviews)
			{
				if (TryGetActorId(preview.ID, out var id))
					previewIds.Add(id);

				if (!preview.Bounds.IsEmpty)
					screenMap.Add(preview, preview.Bounds);

				var cellFootprintBounds = OccupiedCells(preview)
					.Select(cell => new Rectangle(cell.X - cellOffset.X, cell.Y - cellOffset.Y, 1, 1)).Union();

				cellMap.Add(preview, cellFootprintBounds);

				preview.AddedToEditor();
			}

			using (new PerfTimer("UpdateNeighbours"))
				UpdateNeighbours(newPreviews);

			SyncMultiplayerCount();
		}

		public void Remove(EditorActorPreview preview)
		{
			previews.Remove(preview);
			if (TryGetActorId(preview.ID, out var id))
				previewIds.Remove(id);

			screenMap.Remove(preview);

			cellMap.Remove(preview);

			preview.RemovedFromEditor();
			UpdateNeighbours(preview.Footprint);

			if (preview.Info.Name == PlayerSpawnName)
				SyncMultiplayerCount();
		}

		public void RemoveRange(ReadOnlySpan<EditorActorPreview> removePreviews)
		{
			foreach (var preview in removePreviews)
			{
				previews.Remove(preview);
				if (TryGetActorId(preview.ID, out var id))
					previewIds.Remove(id);

				screenMap.Remove(preview);
				cellMap.Remove(preview);
			}

			using (new PerfTimer("RemovedFromEditor", 1))
				foreach (var preview in removePreviews)
					preview.RemovedFromEditor();

			using (new PerfTimer("UpdateNeighbours", 1))
				UpdateNeighbours(removePreviews);

			SyncMultiplayerCount();
		}

		public void RemoveRegion(CellCoordsRegion region)
		{
			RemoveRange(PreviewsInCellRegion(region).ToArray().AsSpan());
		}

		public void RemoveRegion(CellCoordsRegion region, HashSet<CPos> mask)
		{
			RemoveRange(PreviewsInCellRegion(region).Where(p => mask.Overlaps(p.Footprint.Keys)).ToArray().AsSpan());
		}

		public void MoveActor(EditorActorPreview preview, CPos location)
		{
			Remove(preview);
			preview.ReplaceInit(new LocationInit(location));
			var ios = preview.Info.TraitInfoOrDefault<IOccupySpaceInfo>();
			if (ios != null && ios.SharesCell)
			{
				var actorSubCell = FreeSubCellAt(location);
				if (actorSubCell == SubCell.Invalid)
					preview.RemoveInit<SubCellInit>();
				else
					preview.ReplaceInit(new SubCellInit(actorSubCell));
			}

			preview.UpdateFromMove();
			Add(preview);
		}

		void SyncMultiplayerCount()
		{
			var newCount = previews.Count(p => p.Info.Name == PlayerSpawnName);
			var playersChanged = false;
			foreach (var kv in Players.Players)
			{
				if (!kv.Key.StartsWith("Multi", StringComparison.Ordinal))
					continue;

				var name = kv.Key;
				var index = Exts.ParseInt32Invariant(name[5..]);

				if (index >= newCount)
				{
					Players.Players.Remove(name);
					OnPlayerRemoved();
					playersChanged = true;
				}
			}

			for (var index = 0; index < newCount; index++)
			{
				if (Players.Players.ContainsKey($"Multi{index}"))
					continue;

				var pr = new PlayerReference
				{
					Name = $"Multi{index}",
					Faction = "Random",
					Playable = true,
					Enemies = ["Creeps"]
				};

				Players.Players.Add(pr.Name, pr);
				worldRenderer.UpdatePalettesForPlayer(pr.Name, pr.Color, true);
				playersChanged = true;
			}

			if (!playersChanged)
				return;

			var creeps = Players.Players.Keys.FirstOrDefault(p => p == "Creeps");
			if (!string.IsNullOrEmpty(creeps))
				Players.Players[creeps].Enemies = Players.Players.Keys.Where(p => !Players.Players[p].NonCombatant).ToArray();
		}

		void UpdateNeighbours(ReadOnlySpan<EditorActorPreview> previews)
		{
			var cells = new HashSet<CPos>(previews.Length * 6);
			foreach (var preview in previews)
				cells.UnionWith(Util.ExpandFootprint(preview.Footprint.Keys, true));

			if (cells.Count == 0)
				return;

			var bounds = CellCoordsRegion.BoundingRegion(cells);
			var touchedPreviews = PreviewsInCellRegion(bounds)
				.Where(p => cells.Overlaps(p.Footprint.Keys));

			foreach (var p in touchedPreviews)
				p.ReplaceInit(new RuntimeNeighbourInit(NeighbouringPreviews(p.Footprint)));
		}

		void UpdateNeighbours(IReadOnlyDictionary<CPos, SubCell> footprint)
		{
			// Include actors inside the footprint too
			var cells = Util.ExpandFootprint(footprint.Keys, true);
			foreach (var p in cells.SelectMany(PreviewsAtCell))
				p.ReplaceInit(new RuntimeNeighbourInit(NeighbouringPreviews(p.Footprint)));
		}

		Dictionary<CPos, string[]> NeighbouringPreviews(IReadOnlyDictionary<CPos, SubCell> footprint)
		{
			var cells = Util.ExpandFootprint(footprint.Keys, true).Except(footprint.Keys);
			return cells.ToDictionary(c => c, c => PreviewsAtCell(c).Select(p => p.Info.Name).ToArray());
		}

		public IEnumerable<EditorActorPreview> PreviewsInScreenBox(int2 a, int2 b)
		{
			return PreviewsInScreenBox(Rectangle.FromLTRB(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
		}

		public IEnumerable<EditorActorPreview> PreviewsInScreenBox(Rectangle r)
		{
			return screenMap.InBox(r);
		}

		public IEnumerable<EditorActorPreview> PreviewsInCellRegion(CellCoordsRegion region)
		{
			return cellMap.InBox(Rectangle.FromLTRB(
				region.TopLeft.X - cellOffset.X,
				region.TopLeft.Y - cellOffset.Y,
				region.BottomRight.X - cellOffset.X + 1,
				region.BottomRight.Y - cellOffset.Y + 1))
				.Where(p => OccupiedCells(p).Any(region.Contains));
		}

		public IEnumerable<EditorActorPreview> PreviewsAtCell(CPos cell)
		{
			return cellMap.At(new int2(cell.X - cellOffset.X, cell.Y - cellOffset.Y))
				.Where(p => OccupiedCells(p).Any(fp => fp == cell));
		}

		public SubCell FreeSubCellAt(CPos cell)
		{
			var map = worldRenderer.World.Map;
			var previews = PreviewsAtCell(cell).ToArray();
			if (previews.Length == 0)
				return map.Grid.DefaultSubCell;

			for (var i = (byte)SubCell.First; i < map.Grid.SubCellOffsets.Length; i++)
			{
				var blocked = previews.Any(p => p.Footprint.TryGetValue(cell, out var s) && s == (SubCell)i);

				if (!blocked)
					return (SubCell)i;
			}

			return SubCell.Invalid;
		}

		public IEnumerable<EditorActorPreview> PreviewsAtWorldPixel(int2 worldPx)
		{
			return screenMap.At(worldPx);
		}

		public Action OnPlayerRemoved = () => { };

		static bool TryGetActorId(string name, out uint id)
		{
			id = 0;
			return name.StartsWith(ActorPrefix, StringComparison.Ordinal)
				&& uint.TryParse(name.AsSpan(5), NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out id);
		}

		string NextActorName()
		{
			var currentId = 0u;
			while (previewIds.Contains(currentId))
				currentId++;

			return ActorPrefix + currentId.ToStringInvariant();
		}

		ReadOnlySpan<string> NextActorNames(int count)
		{
			var newNamesCount = 0u;
			var newNames = new string[count];

			for (var currentId = 0u; newNamesCount < count; currentId++)
				if (!previewIds.Contains(currentId))
					newNames[newNamesCount++] = ActorPrefix + currentId.ToStringInvariant();

			return newNames;
		}

		public List<MiniYamlNode> Save()
		{
			var nodes = new List<MiniYamlNode>();
			foreach (var a in previews)
				nodes.Add(new MiniYamlNode(a.ID, a.Save()));

			return nodes;
		}

		public void PopulateRadarSignatureCells(Actor self, List<(CPos Cell, Color Color)> destinationBuffer)
		{
			foreach (var preview in cellMap.Keys)
				foreach (var cell in OccupiedCells(preview))
					destinationBuffer.Add((cell, preview.RadarColor));
		}

		public EditorActorPreview this[string id]
		{
			get { return previews.FirstOrDefault(p => p.ID.Equals(id, StringComparison.OrdinalIgnoreCase)); }
		}
	}
}
