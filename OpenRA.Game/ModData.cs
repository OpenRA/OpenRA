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
using System.IO;
using System.Linq;
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
		public readonly IPackageLoader[] PackageLoaders;
		public readonly ISoundLoader[] SoundLoaders;
		public readonly ISpriteLoader[] SpriteLoaders;
		public readonly ISpriteSequenceLoader SpriteSequenceLoader;
		public readonly IModelSequenceLoader ModelSequenceLoader;
		public readonly HotkeyManager Hotkeys;
		public ILoadScreen LoadScreen { get; private set; }
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

			// Take a local copy of the manifest
			Manifest = new Manifest(mod.Id, mod.Package);
			ObjectCreator = new ObjectCreator(Manifest, mods);
			PackageLoaders = ObjectCreator.GetLoaders<IPackageLoader>(Manifest.PackageFormats, "package");

			ModFiles = new FS(mod.Id, mods, PackageLoaders);
			ModFiles.LoadFromManifest(Manifest);
			Manifest.LoadCustomData(ObjectCreator);

			if (useLoadScreen)
			{
				LoadScreen = ObjectCreator.CreateObject<ILoadScreen>(Manifest.LoadScreen.Value);
				LoadScreen.Init(this, Manifest.LoadScreen.ToDictionary(my => my.Value));
				LoadScreen.Display();
			}

			WidgetLoader = new WidgetLoader(this);
			MapCache = new MapCache(this);

			SoundLoaders = ObjectCreator.GetLoaders<ISoundLoader>(Manifest.SoundFormats, "sound");
			SpriteLoaders = ObjectCreator.GetLoaders<ISpriteLoader>(Manifest.SpriteFormats, "sprite");

			var sequenceFormat = Manifest.Get<SpriteSequenceFormat>();
			var sequenceLoader = ObjectCreator.FindType(sequenceFormat.Type + "Loader");
			var sequenceCtor = sequenceLoader != null ? sequenceLoader.GetConstructor(new[] { typeof(ModData) }) : null;
			if (sequenceLoader == null || !sequenceLoader.GetInterfaces().Contains(typeof(ISpriteSequenceLoader)) || sequenceCtor == null)
				throw new InvalidOperationException("Unable to find a sequence loader for type '{0}'.".F(sequenceFormat.Type));

			SpriteSequenceLoader = (ISpriteSequenceLoader)sequenceCtor.Invoke(new[] { this });

			var modelFormat = Manifest.Get<ModelSequenceFormat>();
			var modelLoader = ObjectCreator.FindType(modelFormat.Type + "Loader");
			var modelCtor = modelLoader != null ? modelLoader.GetConstructor(new[] { typeof(ModData) }) : null;
			if (modelLoader == null || !modelLoader.GetInterfaces().Contains(typeof(IModelSequenceLoader)) || modelCtor == null)
				throw new InvalidOperationException("Unable to find a model loader for type '{0}'.".F(modelFormat.Type));

			ModelSequenceLoader = (IModelSequenceLoader)modelCtor.Invoke(new[] { this });
			ModelSequenceLoader.OnMissingModelError = s => Log.Write("debug", s);

			Hotkeys = new HotkeyManager(ModFiles, Game.Settings.Keys, Manifest);

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
				var items = DefaultTileSets.ToDictionary(t => t.Key, t => new SequenceProvider(DefaultFileSystem, this, t.Key, null));
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

			CursorProvider = new CursorProvider(this);
		}

		public IEnumerable<string> Languages { get; private set; }

		public Map PrepareMap(string uid)
		{
			LoadScreen?.Display();

			if (MapCache[uid].Status != MapStatus.Available)
				throw new InvalidDataException("Invalid map uid: {0}".F(uid));

			Map map;
			using (new Support.PerfTimer("Map"))
				map = new Map(this, MapCache[uid].Package);

			// Reinitialize all our assets
			InitializeLoaders(map);

			// Load music with map assets mounted
			using (new Support.PerfTimer("Map.Music"))
				foreach (var entry in map.Rules.Music)
					entry.Value.Load(map);

			return map;
		}

		public void Dispose()
		{
			LoadScreen?.Dispose();
			MapCache.Dispose();

			ObjectCreator?.Dispose();

			Manifest.Dispose();
		}
	}

	public interface ILoadScreen : IDisposable
	{
		/// <summary>Initializes the loadscreen with yaml data from the LoadScreen block in mod.yaml.</summary>
		void Init(ModData m, Dictionary<string, string> info);

		/// <summary>Called at arbitrary times during mod load to rerender the loadscreen.</summary>
		void Display();

		/// <summary>
		/// Called before loading the mod assets.
		/// Returns false if mod loading should be aborted (e.g. switching to another mod instead).
		/// </summary>
		bool BeforeLoad();

		/// <summary>Called when the engine expects to connect to a server/replay or load the shellmap.</summary>
		void StartGame(Arguments args);
	}
}
