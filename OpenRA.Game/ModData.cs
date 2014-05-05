#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA
{
	public class ModData
	{
		public readonly Manifest Manifest;
		public readonly ObjectCreator ObjectCreator;
		public readonly WidgetLoader WidgetLoader;
		public readonly MapCache MapCache;
		public ILoadScreen LoadScreen = null;
		public SheetBuilder SheetBuilder;
		public SpriteLoader SpriteLoader;
		public VoxelLoader VoxelLoader;

		public ModData(string mod)
		{
			Languages = new string[0];
			Manifest = new Manifest(mod);
			ObjectCreator = new ObjectCreator(Manifest);
			LoadScreen = ObjectCreator.CreateObject<ILoadScreen>(Manifest.LoadScreen.Value);
			LoadScreen.Init(Manifest, Manifest.LoadScreen.NodesDict.ToDictionary(x => x.Key, x => x.Value.Value));
			LoadScreen.Display();
			WidgetLoader = new WidgetLoader(this);
			MapCache = new MapCache(Manifest);

			// HACK: Mount only local folders so we have a half-working environment for the asset installer
			GlobalFileSystem.UnmountAll();
			foreach (var dir in Manifest.Folders)
				GlobalFileSystem.Mount(dir);
		}

		public void InitializeLoaders()
		{
			// all this manipulation of static crap here is nasty and breaks
			// horribly when you use ModData in unexpected ways.
			ChromeMetrics.Initialize(Manifest.ChromeMetrics);
			ChromeProvider.Initialize(Manifest.Chrome);
			SheetBuilder = new SheetBuilder(SheetType.Indexed);
			SpriteLoader = new SpriteLoader(new string[0], SheetBuilder);
			VoxelLoader = new VoxelLoader();
			CursorProvider.Initialize(Manifest.Cursors);
		}

		public IEnumerable<string> Languages { get; private set; }

		void LoadTranslations(Map map)
		{
			var selectedTranslations = new Dictionary<string, string>();
			var defaultTranslations = new Dictionary<string, string>();

			if (!Manifest.Translations.Any())
			{
				Languages = new string[0];
				FieldLoader.Translations = new Dictionary<string, string>();
				return;
			}
			
			var yaml = Manifest.Translations.Select(MiniYaml.FromFile).Aggregate(MiniYaml.MergeLiberal);
			Languages = yaml.Select(t => t.Key).ToArray();

			yaml = MiniYaml.MergeLiberal(map.Translations, yaml);

			foreach (var y in yaml)
			{
				if (y.Key == Game.Settings.Graphics.Language)
					selectedTranslations = y.Value.NodesDict.ToDictionary(x => x.Key, x => x.Value.Value ?? "");
				if (y.Key == Game.Settings.Graphics.DefaultLanguage)
					defaultTranslations = y.Value.NodesDict.ToDictionary(x => x.Key, x => x.Value.Value ?? "");
			}
			
			var translations = new Dictionary<string, string>();
			foreach (var tkv in defaultTranslations.Concat(selectedTranslations))
			{
				if (translations.ContainsKey(tkv.Key))
					continue;
				if (selectedTranslations.ContainsKey(tkv.Key))
					translations.Add(tkv.Key, selectedTranslations[tkv.Key]);
				else
					translations.Add(tkv.Key, tkv.Value);
			}

			FieldLoader.Translations = translations;
		}

		public Map PrepareMap(string uid)
		{
			LoadScreen.Display();

			if (MapCache[uid].Status != MapStatus.Available)
				throw new InvalidDataException("Invalid map uid: {0}".F(uid));

			// Operate on a copy of the map to avoid gameplay state leaking into the cache
			var map = new Map(MapCache[uid].Map.Path);

			LoadTranslations(map);

			// Reinit all our assets
			InitializeLoaders();
			GlobalFileSystem.LoadFromManifest(Manifest);

			// Mount map package so custom assets can be used. TODO: check priority.
			GlobalFileSystem.Mount(GlobalFileSystem.OpenPackage(map.Path, null, int.MaxValue));

			using (new Support.PerfTimer("LoadRules"))
				Rules.LoadRules(Manifest, map);
			SpriteLoader = new SpriteLoader(Rules.TileSets[map.Tileset].Extensions, SheetBuilder);

			using (new Support.PerfTimer("SequenceProvider.Initialize"))
			{
				// TODO: Don't load the sequences for assets that are not used in this tileset. Maybe use the existing EditorTilesetFilters.
				SequenceProvider.Initialize(Manifest.Sequences, map.Sequences);
			}
			VoxelProvider.Initialize(Manifest.VoxelSequences, map.VoxelSequences);
			return map;
		}
	}

	public interface ILoadScreen
	{
		void Init(Manifest m, Dictionary<string, string> info);
		void Display();
		void StartGame();
	}
}
