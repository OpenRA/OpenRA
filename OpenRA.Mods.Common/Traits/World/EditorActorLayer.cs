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
using System.Linq;
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
		public readonly EditorActorLayerInfo Info;
		readonly List<EditorActorPreview> previews = new();

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
			if (w.Type != WorldType.Editor)
				return;

			Players = new MapPlayers(w.Map.PlayerDefinitions);

			worldOwner = Players.Players.Select(kvp => kvp.Value).First(p => !p.Playable && p.OwnsWorld);
			w.SetWorldOwner(new Player(w, null, worldOwner, playerRandom));
		}

		public void WorldLoaded(World world, WorldRenderer wr)
		{
			if (world.Type != WorldType.Editor)
				return;

			worldRenderer = wr;

			foreach (var pr in Players.Players.Values)
				wr.UpdatePalettesForPlayer(pr.Name, pr.Color, false);

			cellOffset = new int2(world.Map.AllCells.Min(c => c.X), world.Map.AllCells.Min((c) => c.Y));
			var cellOffsetMax = new int2(world.Map.AllCells.Max(c => c.X), world.Map.AllCells.Max((c) => c.Y));
			var mapCellSize = cellOffsetMax - cellOffset;
			cellMap = new SpatiallyPartitioned<EditorActorPreview>(
				mapCellSize.X, mapCellSize.Y, Exts.IntegerDivisionRoundingAwayFromZero(Info.BinSize, world.Map.Grid.TileSize.Width));

			var ts = world.Map.Grid.TileSize;
			var width = world.Map.MapSize.X * ts.Width;
			var height = world.Map.MapSize.Y * ts.Height;
			screenMap = new SpatiallyPartitioned<EditorActorPreview>(width, height, Info.BinSize);

			foreach (var kv in world.Map.ActorDefinitions)
				Add(kv.Key, new ActorReference(kv.Value.Value, kv.Value.ToDictionary()), true);

			// Update neighbours in one pass
			foreach (var p in previews)
				UpdateNeighbours(p.Footprint);
		}

		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			if (wr.World.Type != WorldType.Editor)
				return;

			foreach (var p in previews)
				p.Tick();
		}

		static readonly IEnumerable<IRenderable> NoRenderables = Enumerable.Empty<IRenderable>();
		public virtual IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			if (wr.World.Type != WorldType.Editor)
				return NoRenderables;

			return PreviewsInScreenBox(wr.Viewport.TopLeft, wr.Viewport.BottomRight)
				.SelectMany(p => p.Render());
		}

		IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr)
		{
			// World-actor render traits don't require screen bounds
			yield break;
		}

		public IEnumerable<IRenderable> RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (wr.World.Type != WorldType.Editor)
				return NoRenderables;

			return PreviewsInScreenBox(wr.Viewport.TopLeft, wr.Viewport.BottomRight)
				.SelectMany(p => p.RenderAnnotations());
		}

		bool IRenderAnnotations.SpatiallyPartitionable => false;

		public EditorActorPreview Add(ActorReference reference) { return Add(NextActorName(), reference); }

		public EditorActorPreview Add(string id, ActorReference reference, bool initialSetup = false)
		{
			// If an actor's doesn't have a valid owner transfer ownership to neutral
			var ownerInit = reference.Get<OwnerInit>();
			if (!Players.Players.TryGetValue(ownerInit.InternalName, out var owner))
			{
				owner = worldOwner;
				reference.Remove(ownerInit);
				reference.Add(new OwnerInit(worldOwner.Name));
			}

			var preview = new EditorActorPreview(worldRenderer, id, reference, owner);
			Add(preview, initialSetup);
			return preview;
		}

		public void Add(EditorActorPreview preview, bool initialSetup = false)
		{
			previews.Add(preview);
			if (!preview.Bounds.IsEmpty)
				screenMap.Add(preview, preview.Bounds);

			var cellFootprintBounds = Footprint(preview).Select(
				cell => new Rectangle(cell.X - cellOffset.X, cell.Y - cellOffset.Y, 1, 1)).Union();
			cellMap.Add(preview, cellFootprintBounds);

			preview.AddedToEditor();

			if (!initialSetup)
			{
				UpdateNeighbours(preview.Footprint);

				if (preview.Type == "mpspawn")
					SyncMultiplayerCount();
			}
		}

		IEnumerable<CPos> Footprint(EditorActorPreview preview)
		{
			// Fallback to the actor's CenterPosition for the ActorMap if it has no Footprint
			if (preview.Footprint.Count == 0)
				return new[] { worldRenderer.World.Map.CellContaining(preview.CenterPosition) };
			return preview.Footprint.Keys;
		}

		public void Remove(EditorActorPreview preview)
		{
			previews.Remove(preview);
			screenMap.Remove(preview);

			cellMap.Remove(preview);

			preview.RemovedFromEditor();
			UpdateNeighbours(preview.Footprint);

			if (preview.Info.Name == "mpspawn")
				SyncMultiplayerCount();
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
			var newCount = previews.Count(p => p.Info.Name == "mpspawn");
			var mp = Players.Players.Where(p => p.Key.StartsWith("Multi", StringComparison.Ordinal)).ToList();
			foreach (var kv in mp)
			{
				var name = kv.Key;
				var index = Exts.ParseInt32Invariant(name[5..]);

				if (index >= newCount)
				{
					Players.Players.Remove(name);
					OnPlayerRemoved();
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
					Enemies = new[] { "Creeps" }
				};

				Players.Players.Add(pr.Name, pr);
				worldRenderer.UpdatePalettesForPlayer(pr.Name, pr.Color, true);
			}

			var creeps = Players.Players.Keys.FirstOrDefault(p => p == "Creeps");
			if (!string.IsNullOrEmpty(creeps))
				Players.Players[creeps].Enemies = Players.Players.Keys.Where(p => !Players.Players[p].NonCombatant).ToArray();
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
				.Where(p => Footprint(p).Any(region.Contains));
		}

		public IEnumerable<EditorActorPreview> PreviewsAtCell(CPos cell)
		{
			return cellMap.At(new int2(cell.X - cellOffset.X, cell.Y - cellOffset.Y))
				.Where(p => Footprint(p).Any(fp => fp == cell));
		}

		public SubCell FreeSubCellAt(CPos cell)
		{
			var map = worldRenderer.World.Map;
			var previews = PreviewsAtCell(cell).ToList();
			if (previews.Count == 0)
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

		string NextActorName()
		{
			var id = previews.Count;
			var possibleName = "Actor" + id.ToStringInvariant();

			while (previews.Any(p => p.ID == possibleName))
			{
				id++;
				possibleName = "Actor" + id.ToStringInvariant();
			}

			return possibleName;
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
				foreach (var cell in Footprint(preview))
					destinationBuffer.Add((cell, preview.RadarColor));
		}

		public EditorActorPreview this[string id]
		{
			get { return previews.FirstOrDefault(p => p.ID.Equals(id, StringComparison.OrdinalIgnoreCase)); }
		}
	}
}
