#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
using System.Reflection;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Widgets;
using FS = OpenRA.FileSystem.FileSystem;

namespace OpenRA
{
	public sealed class ModData : IDisposable
	{
		public readonly Manifest Manifest;
		public readonly ObjectCreator ObjectCreator;
		public readonly WidgetLoader WidgetLoader;
		public readonly MapCache MapCache;
		public readonly ISoundLoader[] SoundLoaders;
		public readonly ISpriteLoader[] SpriteLoaders;
		public readonly ISpriteSequenceLoader SpriteSequenceLoader;
		public ILoadScreen LoadScreen { get; private set; }
		public VoxelLoader VoxelLoader { get; private set; }
		public CursorProvider CursorProvider { get; private set; }
		public FS ModFiles;
		public IReadOnlyFileSystem DefaultFileSystem { get { return ModFiles; } }

		readonly Lazy<Ruleset> defaultRules;
		public Ruleset DefaultRules { get { return defaultRules.Value; } }

		readonly Lazy<IReadOnlyDictionary<string, TileSet>> defaultTileSets;
		public IReadOnlyDictionary<string, TileSet> DefaultTileSets { get { return defaultTileSets.Value; } }

		readonly Lazy<IReadOnlyDictionary<string, SequenceProvider>> defaultSequences;
		public IReadOnlyDictionary<string, SequenceProvider> DefaultSequences { get { return defaultSequences.Value; } }

		public ModData(Manifest mod, InstalledMods mods, bool useLoadScreen = false)
		{
			Languages = new string[0];

			ModFiles = new FS(mods);

			// Take a local copy of the manifest
			Manifest = new Manifest(mod.Id, mod.Package);
			ModFiles.LoadFromManifest(Manifest);

			ObjectCreator = new ObjectCreator(Manifest, ModFiles);
			Manifest.LoadCustomData(ObjectCreator);

			if (useLoadScreen)
			{
				LoadScreen = ObjectCreator.CreateObject<ILoadScreen>(Manifest.LoadScreen.Value);
				LoadScreen.Init(this, Manifest.LoadScreen.ToDictionary(my => my.Value));
				LoadScreen.Display();
			}

			WidgetLoader = new WidgetLoader(this);
			MapCache = new MapCache(this);

			SoundLoaders = GetLoaders<ISoundLoader>(Manifest.SoundFormats, "sound");
			SpriteLoaders = GetLoaders<ISpriteLoader>(Manifest.SpriteFormats, "sprite");

			var sequenceFormat = Manifest.Get<SpriteSequenceFormat>();
			var sequenceLoader = ObjectCreator.FindType(sequenceFormat.Type + "Loader");
			var ctor = sequenceLoader != null ? sequenceLoader.GetConstructor(new[] { typeof(ModData) }) : null;
			if (sequenceLoader == null || !sequenceLoader.GetInterfaces().Contains(typeof(ISpriteSequenceLoader)) || ctor == null)
				throw new InvalidOperationException("Unable to find a sequence loader for type '{0}'.".F(sequenceFormat.Type));

			SpriteSequenceLoader = (ISpriteSequenceLoader)ctor.Invoke(new[] { this });
			SpriteSequenceLoader.OnMissingSpriteError = s => Log.Write("debug", s);

			defaultRules = Exts.Lazy(() => Ruleset.LoadDefaults(this));
			defaultTileSets = Exts.Lazy(() =>
			{
				var items = new Dictionary<string, TileSet>();

				foreach (var file in Manifest.TileSets)
				{
					var t = new TileSet(DefaultFileSystem, file);
					items.Add(t.Id, t);
				}

				return (IReadOnlyDictionary<string, TileSet>)(new ReadOnlyDictionary<string, TileSet>(items));
			});

			defaultSequences = Exts.Lazy(() =>
			{
				var items = DefaultTileSets.ToDictionary(t => t.Key, t => new SequenceProvider(DefaultFileSystem, this, t.Value, null));
				return (IReadOnlyDictionary<string, SequenceProvider>)(new ReadOnlyDictionary<string, SequenceProvider>(items));
			});

			initialThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
		}

		// HACK: Only update the loading screen if we're in the main thread.
		int initialThreadId;
		internal void HandleLoadingProgress()
		{
			if (LoadScreen != null && IsOnMainThread)
				LoadScreen.Display();
		}

		internal bool IsOnMainThread { get { return System.Threading.Thread.CurrentThread.ManagedThreadId == initialThreadId; } }

		public void InitializeLoaders(IReadOnlyFileSystem fileSystem)
		{
			// all this manipulation of static crap here is nasty and breaks
			// horribly when you use ModData in unexpected ways.
			ChromeMetrics.Initialize(this);
			ChromeProvider.Initialize(this);

			Game.Sound.Initialize(SoundLoaders, fileSystem);

			if (VoxelLoader != null)
				VoxelLoader.Dispose();
			VoxelLoader = new VoxelLoader(fileSystem);

			CursorProvider = new CursorProvider(this);
		}

		TLoader[] GetLoaders<TLoader>(IEnumerable<string> formats, string name)
		{
			var loaders = new List<TLoader>();
			foreach (var format in formats)
			{
				var loader = ObjectCreator.FindType(format + "Loader");
				if (loader == null || !loader.GetInterfaces().Contains(typeof(TLoader)))
					throw new InvalidOperationException("Unable to find a {0} loader for type '{1}'.".F(name, format));

				loaders.Add((TLoader)ObjectCreator.CreateBasic(loader));
			}

			return loaders.ToArray();
		}

		public IEnumerable<string> Languages { get; private set; }

		void LoadTranslations(Map map)
		{
			var selectedTranslations = new Dictionary<string, string>();
			var defaultTranslations = new Dictionary<string, string>();

			if (!Manifest.Translations.Any())
			{
				Languages = new string[0];
				return;
			}

			var yaml = MiniYaml.Load(map, Manifest.Translations, map.TranslationDefinitions);
			Languages = yaml.Select(t => t.Key).ToArray();

			foreach (var y in yaml)
			{
				if (y.Key == Game.Settings.Graphics.Language)
					selectedTranslations = y.Value.ToDictionary(my => my.Value ?? "");
				else if (y.Key == Game.Settings.Graphics.DefaultLanguage)
					defaultTranslations = y.Value.ToDictionary(my => my.Value ?? "");
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

			FieldLoader.SetTranslations(translations);
		}

		public Map PrepareMap(string uid)
		{
			if (LoadScreen != null)
				LoadScreen.Display();

			if (MapCache[uid].Status != MapStatus.Available)
				throw new InvalidDataException("Invalid map uid: {0}".F(uid));

			Map map;
			using (new Support.PerfTimer("Map"))
				map = new Map(this, MapCache[uid].Package);

			LoadTranslations(map);

			// Reinitialize all our assets
			InitializeLoaders(map);

			// Load music with map assets mounted
			using (new Support.PerfTimer("Map.Music"))
				foreach (var entry in map.Rules.Music)
					entry.Value.Load(map);

			VoxelProvider.Initialize(VoxelLoader, map, MiniYaml.Load(map, Manifest.VoxelSequences, map.VoxelSequenceDefinitions));
			VoxelLoader.Finish();

			return map;
		}

		public void Dispose()
		{
			if (LoadScreen != null)
				LoadScreen.Dispose();
			MapCache.Dispose();
			if (VoxelLoader != null)
				VoxelLoader.Dispose();
		}
	}

	public interface ILoadScreen : IDisposable
	{
		void Init(ModData m, Dictionary<string, string> info);
		void Display();
		bool RequiredContentIsInstalled();
		void StartGame(Arguments args);
	}
}
