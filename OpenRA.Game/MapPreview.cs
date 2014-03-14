#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA
{
	public enum MapStatus { Available, Unavailable }
	public class MapPreview
	{
		static readonly List<CPos> NoSpawns = new List<CPos>();

		public readonly string Uid;
		public string Title { get; private set; }
		public string Type { get; private set; }
		public string Author { get; private set; }
		public int PlayerCount { get; private set; }
		public List<CPos> SpawnPoints { get; private set; }
		public Rectangle Bounds { get; private set; }
		public Map Map { get; private set; }
		public MapStatus Status { get; private set; }

		Sprite minimap;
		bool generatingMinimap;
		public Sprite Minimap
		{
			get
			{
				if (minimap != null)
					return minimap;

				if (!generatingMinimap && Status == MapStatus.Available)
				{
					generatingMinimap = true;
					cache.CacheMinimap(this);
				}

				return null;
			}

			set
			{
				minimap = value;
				generatingMinimap = false;
			}
		}

		MapCache cache;
		public MapPreview(string uid, MapCache cache)
		{
			this.cache = cache;
			Uid = uid;
			Title = "Unknown Map";
			Type = "Unknown";
			Author = "Unknown Author";
			PlayerCount = 0;
			Bounds = Rectangle.Empty;
			SpawnPoints = NoSpawns;
			Status = MapStatus.Unavailable;
		}

		public void UpdateFromMap(Map m)
		{
			Map = m;
			Title = m.Title;
			Type = m.Type;
			Type = m.Type;
			Author = m.Author;
			PlayerCount = m.Players.Count(x => x.Value.Playable);
			Bounds = m.Bounds;
			SpawnPoints = m.GetSpawnPoints().ToList();
			Status = MapStatus.Available;
		}
	}
}
