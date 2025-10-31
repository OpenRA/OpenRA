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
using System.Collections.ObjectModel;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Video;
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
		public readonly ITerrainLoader TerrainLoader;
		public readonly ISpriteSequenceLoader SpriteSequenceLoader;
		public readonly IVideoLoader[] VideoLoaders;
		public readonly HotkeyManager Hotkeys;
		public readonly IFileSystemLoader FileSystemLoader;

		public ILoadScreen LoadScreen { get; }
		public CursorProvider CursorProvider { get; private set; }
		public FS ModFiles;
		public IReadOnlyFileSystem DefaultFileSystem => ModFiles;

		readonly Lazy<Ruleset> defaultRules;
		public Ruleset DefaultRules => defaultRules.Value;

		readonly Lazy<IReadOnlyDictionary<string, ITerrainInfo>> defaultTerrainInfo;
		public IReadOnlyDictionary<string, ITerrainInfo> DefaultTerrainInfo => defaultTerrainInfo.Value;

		public ModData(Manifest mod, InstalledMods mods, bool useLoadScreen = false)
		{
			Languages = [];

			// Take a local copy of the manifest
			Manifest = new Manifest(mod.Id, mod.Package);
			ObjectCreator = new ObjectCreator(Manifest, mods);
			PackageLoaders = ObjectCreator.GetLoaders<IPackageLoader>(Manifest.PackageFormats, "package");
			ModFiles = new FS(mod.Id, mods, PackageLoaders);

			FileSystemLoader = ObjectCreator.GetLoader<IFileSystemLoader>(Manifest.FileSystem.Value, "filesystem");
			FieldLoader.Load(FileSystemLoader, Manifest.FileSystem);
			FileSystemLoader.Mount(ModFiles, ObjectCreator);
			ModFiles.TrimExcess();

			Manifest.LoadCustomData(ObjectCreator);

			FluentProvider.Initialize(this, DefaultFileSystem);

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
			VideoLoaders = ObjectCreator.GetLoaders<IVideoLoader>(Manifest.VideoFormats, "video");

			var terrainFormat = Manifest.Get<TerrainFormat>();
			var terrainLoader = ObjectCreator.FindType(terrainFormat.Type + "Loader");
			var terrainCtor = terrainLoader?.GetConstructor([typeof(ModData)]);
			if (terrainLoader == null || !terrainLoader.GetInterfaces().Contains(typeof(ITerrainLoader)) || terrainCtor == null)
				throw new InvalidOperationException($"Unable to find a terrain loader for type '{terrainFormat.Type}'.");

			TerrainLoader = (ITerrainLoader)terrainCtor.Invoke([this]);

			var sequenceFormat = Manifest.Get<SpriteSequenceFormat>();
			var sequenceLoader = ObjectCreator.FindType(sequenceFormat.Type + "Loader");
			var sequenceCtor = sequenceLoader?.GetConstructor([typeof(ModData)]);
			if (sequenceLoader == null || !sequenceLoader.GetInterfaces().Contains(typeof(ISpriteSequenceLoader)) || sequenceCtor == null)
				throw new InvalidOperationException($"Unable to find a sequence loader for type '{sequenceFormat.Type}'.");

			SpriteSequenceLoader = (ISpriteSequenceLoader)sequenceCtor.Invoke([this]);

			Hotkeys = new HotkeyManager(ModFiles, Game.Settings.Keys, Manifest);

			defaultRules = Exts.Lazy(() => Ruleset.LoadDefaults(this));
			defaultTerrainInfo = Exts.Lazy(() =>
			{
				var items = new Dictionary<string, ITerrainInfo>();

				foreach (var file in Manifest.TileSets)
				{
					var t = TerrainLoader.ParseTerrain(DefaultFileSystem, file);
					items.Add(t.Id, t);
				}

				return (IReadOnlyDictionary<string, ITerrainInfo>)new ReadOnlyDictionary<string, ITerrainInfo>(items);
			});

			initialThreadId = Environment.CurrentManagedThreadId;
		}

		// HACK: Only update the loading screen if we're in the main thread.
		readonly int initialThreadId;
		internal void HandleLoadingProgress()
		{
			if (LoadScreen != null && IsOnMainThread)
				LoadScreen.Display();
		}

		internal bool IsOnMainThread => Environment.CurrentManagedThreadId == initialThreadId;

		public void InitializeLoaders(IReadOnlyFileSystem fileSystem)
		{
			// all this manipulation of static crap here is nasty and breaks
			// horribly when you use ModData in unexpected ways.
			ChromeMetrics.Initialize(this);
			ChromeProvider.Initialize(this);
			FluentProvider.Initialize(this, fileSystem);

			Game.Sound.Initialize(SoundLoaders, fileSystem);

			CursorProvider = new CursorProvider(this);
		}

		public IEnumerable<string> Languages { get; }

		public void PrepareMap(Map map)
		{
			LoadScreen?.Display();

			// Reinitialize all our assets
			InitializeLoaders(map);
			map.Sequences.LoadSprites();

			// Load music with map assets mounted
			using (new Support.PerfTimer("Map.Music"))
				foreach (var entry in map.Rules.Music)
					entry.Value.Load(map);
		}

		public MiniYamlNode[][] GetRulesYaml()
		{
			var stringPool = new HashSet<string>(); // Reuse common strings in YAML
			return Manifest.Rules.Select(s => MiniYaml.FromStream(DefaultFileSystem.Open(s), s, stringPool: stringPool).ToArray()).ToArray();
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

	public interface IFileSystemLoader
	{
		void Mount(FS fileSystem, ObjectCreator objectCreator);
	}
}
