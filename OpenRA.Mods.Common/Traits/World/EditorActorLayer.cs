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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Required for the map editor to work. Attach this to the world actor.")]
	public class EditorActorLayerInfo : TraitInfo, ICreatePlayersInfo
	{
		[Desc("Size of partition bins (world pixels)")]
		public readonly int BinSize = 250;

		void ICreatePlayersInfo.CreateServerPlayers(MapPreview map, Session lobbyInfo, List<GameInformation.Player> players, MersenneTwister playerRandom)
		{
			throw new NotImplementedException("EditorActorLayer must not be defined on the world actor");
		}

		public override object Create(ActorInitializer init) { return new EditorActorLayer(init.Self, this); }
	}

	public class EditorActorLayer : IWorldLoaded, ITickRender, IRender, IRadarSignature, ICreatePlayers, IRenderAnnotations
	{
		readonly EditorActorLayerInfo info;
		readonly List<EditorActorPreview> previews = new List<EditorActorPreview>();
		readonly Dictionary<CPos, List<EditorActorPreview>> cellMap = new Dictionary<CPos, List<EditorActorPreview>>();

		SpatiallyPartitioned<EditorActorPreview> screenMap;
		WorldRenderer worldRenderer;

		public MapPlayers Players { get; private set; }

		public EditorActorLayer(Actor self, EditorActorLayerInfo info)
		{
			this.info = info;
		}

		void ICreatePlayers.CreatePlayers(World w, MersenneTwister playerRandom)
		{
			if (w.Type != WorldType.Editor)
				return;

			Players = new MapPlayers(w.Map.PlayerDefinitions);

			var worldOwner = Players.Players.Select(kvp => kvp.Value).First(p => !p.Playable && p.OwnsWorld);
			w.SetWorldOwner(new Player(w, null, worldOwner, playerRandom));
		}

		public void WorldLoaded(World world, WorldRenderer wr)
		{
			if (world.Type != WorldType.Editor)
				return;

			worldRenderer = wr;

			foreach (var pr in Players.Players.Values)
				wr.UpdatePalettesForPlayer(pr.Name, pr.Color, false);

			var ts = world.Map.Grid.TileSize;
			var width = world.Map.MapSize.X * ts.Width;
			var height = world.Map.MapSize.Y * ts.Height;
			screenMap = new SpatiallyPartitioned<EditorActorPreview>(width, height, info.BinSize);

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

			return PreviewsInBox(wr.Viewport.TopLeft, wr.Viewport.BottomRight)
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

			return PreviewsInBox(wr.Viewport.TopLeft, wr.Viewport.BottomRight)
				.SelectMany(p => p.RenderAnnotations());
		}

		bool IRenderAnnotations.SpatiallyPartitionable { get { return false; } }

		public EditorActorPreview Add(ActorReference reference) { return Add(NextActorName(), reference); }

		public EditorActorPreview Add(string id, ActorReference reference, bool initialSetup = false)
		{
			var owner = Players.Players[reference.Get<OwnerInit>().InternalName];
			var preview = new EditorActorPreview(worldRenderer, id, reference, owner);

			Add(preview, initialSetup);

			return preview;
		}

		public void Add(EditorActorPreview preview, bool initialSetup = false)
		{
			previews.Add(preview);
			if (!preview.Bounds.IsEmpty)
				screenMap.Add(preview, preview.Bounds);

			// Fallback to the actor's CenterPosition for the ActorMap if it has no Footprint
			var footprint = preview.Footprint.Select(kv => kv.Key).ToArray();
			if (!footprint.Any())
				footprint = new[] { worldRenderer.World.Map.CellContaining(preview.CenterPosition) };

			foreach (var cell in footprint)
				AddPreviewLocation(preview, cell);

			preview.AddedToEditor();

			if (!initialSetup)
			{
				UpdateNeighbours(preview.Footprint);

				if (preview.Type == "mpspawn")
					SyncMultiplayerCount();
			}
		}

		public void Remove(EditorActorPreview preview)
		{
			previews.Remove(preview);
			screenMap.Remove(preview);

			// Fallback to the actor's CenterPosition for the ActorMap if it has no Footprint
			var footprint = preview.Footprint.Select(kv => kv.Key).ToArray();
			if (!footprint.Any())
				footprint = new[] { worldRenderer.World.Map.CellContaining(preview.CenterPosition) };

			foreach (var cell in footprint)
			{
				if (!cellMap.TryGetValue(cell, out var list))
					continue;

				list.Remove(preview);

				if (!list.Any())
					cellMap.Remove(cell);
			}

			preview.RemovedFromEditor();
			UpdateNeighbours(preview.Footprint);

			if (preview.Info.Name == "mpspawn")
				SyncMultiplayerCount();
		}

		void SyncMultiplayerCount()
		{
			var newCount = previews.Count(p => p.Info.Name == "mpspawn");
			var mp = Players.Players.Where(p => p.Key.StartsWith("Multi")).ToList();
			foreach (var kv in mp)
			{
				var name = kv.Key;
				var index = int.Parse(name.Substring(5));

				if (index >= newCount)
				{
					Players.Players.Remove(name);
					OnPlayerRemoved();
				}
			}

			for (var index = 0; index < newCount; index++)
			{
				if (Players.Players.ContainsKey("Multi{0}".F(index)))
					continue;

				var pr = new PlayerReference
				{
					Name = "Multi{0}".F(index),
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
			foreach (var p in cells.SelectMany(c => PreviewsAt(c)))
				p.ReplaceInit(new RuntimeNeighbourInit(NeighbouringPreviews(p.Footprint)));
		}

		void AddPreviewLocation(EditorActorPreview preview, CPos location)
		{
			if (!cellMap.TryGetValue(location, out var list))
			{
				list = new List<EditorActorPreview>();
				cellMap.Add(location, list);
			}

			list.Add(preview);
		}

		Dictionary<CPos, string[]> NeighbouringPreviews(IReadOnlyDictionary<CPos, SubCell> footprint)
		{
			var cells = Util.ExpandFootprint(footprint.Keys, true).Except(footprint.Keys);
			return cells.ToDictionary(c => c, c => PreviewsAt(c).Select(p => p.Info.Name).ToArray());
		}

		public IEnumerable<EditorActorPreview> PreviewsInBox(int2 a, int2 b)
		{
			return screenMap.InBox(Rectangle.FromLTRB(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
		}

		public IEnumerable<EditorActorPreview> PreviewsInBox(Rectangle r)
		{
			return screenMap.InBox(r);
		}

		public IEnumerable<EditorActorPreview> PreviewsAt(CPos cell)
		{
			if (cellMap.TryGetValue(cell, out var list))
				return list;

			return Enumerable.Empty<EditorActorPreview>();
		}

		public SubCell FreeSubCellAt(CPos cell)
		{
			var map = worldRenderer.World.Map;
			var previews = PreviewsAt(cell).ToList();
			if (!previews.Any())
				return map.Grid.DefaultSubCell;

			for (var i = (byte)SubCell.First; i < map.Grid.SubCellOffsets.Length; i++)
			{
				var blocked = previews.Any(p =>
				{
					return p.Footprint.TryGetValue(cell, out var s) && s == (SubCell)i;
				});

				if (!blocked)
					return (SubCell)i;
			}

			return SubCell.Invalid;
		}

		public IEnumerable<EditorActorPreview> PreviewsAt(int2 worldPx)
		{
			return screenMap.At(worldPx);
		}

		public Action OnPlayerRemoved = () => { };

		string NextActorName()
		{
			var id = previews.Count;
			var possibleName = "Actor" + id.ToString();

			while (previews.Any(p => p.ID == possibleName))
			{
				id++;
				possibleName = "Actor" + id.ToString();
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
			foreach (var previewsForCell in cellMap)
				foreach (var preview in previewsForCell.Value)
					destinationBuffer.Add((previewsForCell.Key, preview.RadarColor));
		}

		public EditorActorPreview this[string id]
		{
			get { return previews.FirstOrDefault(p => p.ID.ToLowerInvariant() == id); }
		}
	}
}
