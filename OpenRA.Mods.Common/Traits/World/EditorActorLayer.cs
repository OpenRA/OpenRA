#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Required for the map editor to work. Attach this to the world actor.")]
	public class EditorActorLayerInfo : ITraitInfo
	{
		[Desc("Size of partition bins (world pixels)")]
		public readonly int BinSize = 250;

		public object Create(ActorInitializer init) { return new EditorActorLayer(init.Self, this); }
	}

	public class EditorActorLayer : IWorldLoaded, ITickRender, IRender, IRadarSignature, ICreatePlayers
	{
		readonly EditorActorLayerInfo info;
		readonly Dictionary<string, EditorActorPreview> previews = new Dictionary<string, EditorActorPreview>();
		readonly Dictionary<CPos, List<EditorActorPreview>> cellMap = new Dictionary<CPos, List<EditorActorPreview>>();

		SpatiallyPartitioned<EditorActorPreview> screenMap;
		WorldRenderer worldRenderer;

		public MapPlayers Players { get; private set; }

		public EditorActorLayer(Actor self, EditorActorLayerInfo info)
		{
			this.info = info;
		}

		public void CreatePlayers(World w)
		{
			if (w.Type != WorldType.Editor)
				return;

			Players = new MapPlayers(w.Map.PlayerDefinitions);

			var worldOwner = Players.Players.Select(kvp => kvp.Value).First(p => !p.Playable && p.OwnsWorld);
			w.WorldActor.Owner = new Player(w, null, worldOwner);
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
			foreach (var p in previews.Values)
				UpdateNeighbours(p.Footprint);
		}

		public void TickRender(WorldRenderer wr, Actor self)
		{
			if (wr.World.Type != WorldType.Editor)
				return;

			foreach (var kv in previews.Values)
				kv.Tick();
		}

		static readonly IEnumerable<IRenderable> NoRenderables = Enumerable.Empty<IRenderable>();
		public virtual IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			if (wr.World.Type != WorldType.Editor)
				return NoRenderables;

			return PreviewsInBox(wr.Viewport.TopLeft, wr.Viewport.BottomRight)
				.SelectMany(p => p.Render());
		}

		public EditorActorPreview Add(ActorReference reference) { return Add(NextActorName(), reference); }

		EditorActorPreview Add(string id, ActorReference reference, bool initialSetup = false)
		{
			var owner = Players.Players[reference.InitDict.Get<OwnerInit>().PlayerName];

			var preview = new EditorActorPreview(worldRenderer, id, reference, owner);
			previews.Add(id, preview);
			screenMap.Add(preview, preview.Bounds);

			foreach (var kv in preview.Footprint)
			{
				List<EditorActorPreview> list;
				if (!cellMap.TryGetValue(kv.Key, out list))
				{
					list = new List<EditorActorPreview>();
					cellMap.Add(kv.Key, list);
				}

				list.Add(preview);
			}

			if (!initialSetup)
			{
				UpdateNeighbours(preview.Footprint);

				if (reference.Type == "mpspawn")
					SyncMultiplayerCount();
			}

			return preview;
		}

		public void Remove(EditorActorPreview preview)
		{
			previews.Remove(preview.ID);
			screenMap.Remove(preview);

			foreach (var kv in preview.Footprint)
			{
				List<EditorActorPreview> list;
				if (!cellMap.TryGetValue(kv.Key, out list))
					continue;

				list.Remove(preview);

				if (!list.Any())
					cellMap.Remove(kv.Key);
			}

			UpdateNeighbours(preview.Footprint);

			if (preview.Info.Name == "mpspawn")
				SyncMultiplayerCount();
		}

		void SyncMultiplayerCount()
		{
			var newCount = previews.Count(p => p.Value.Info.Name == "mpspawn");
			var mp = Players.Players.Where(p => p.Key.StartsWith("Multi")).ToList();
			foreach (var kv in mp)
			{
				var name = kv.Key;
				var index = int.Parse(name.Substring(5));

				if (index >= newCount)
				{
					Players.Players.Remove(name);
					worldRenderer.World.Players.RemoveAll(pp => pp.InternalName == name);
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
		}

		void UpdateNeighbours(IReadOnlyDictionary<CPos, SubCell> footprint)
		{
			// Include actors inside the footprint too
			var cells = OpenRA.Traits.Util.ExpandFootprint(footprint.Keys, true);
			foreach (var p in cells.SelectMany(c => PreviewsAt(c)))
				p.ReplaceInit(new RuntimeNeighbourInit(NeighbouringPreviews(p.Footprint)));
		}

		Dictionary<CPos, string[]> NeighbouringPreviews(IReadOnlyDictionary<CPos, SubCell> footprint)
		{
			var cells = OpenRA.Traits.Util.ExpandFootprint(footprint.Keys, true).Except(footprint.Keys);
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
			List<EditorActorPreview> list;
			if (cellMap.TryGetValue(cell, out list))
				return list;

			return Enumerable.Empty<EditorActorPreview>();
		}

		public SubCell FreeSubCellAt(CPos cell)
		{
			var map = worldRenderer.World.Map;
			var previews = PreviewsAt(cell).ToList();
			if (!previews.Any())
				return map.DefaultSubCell;

			for (var i = (int)SubCell.First; i < map.SubCellOffsets.Length; i++)
				if (!previews.Any(p => p.Footprint[cell] == (SubCell)i))
					return (SubCell)i;

			return SubCell.Invalid;
		}

		public IEnumerable<EditorActorPreview> PreviewsAt(int2 worldPx)
		{
			return screenMap.At(worldPx);
		}

		string NextActorName()
		{
			var id = previews.Count();
			var possibleName = "Actor" + id.ToString();

			while (previews.ContainsKey(possibleName))
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
				nodes.Add(new MiniYamlNode(a.Key, a.Value.Save()));

			return nodes;
		}

		public IEnumerable<Pair<CPos, Color>> RadarSignatureCells(Actor self)
		{
			return cellMap.SelectMany(c => c.Value.Select(p => Pair.New(c.Key, p.Owner.Color.RGB)));
		}
	}
}
