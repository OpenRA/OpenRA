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
using System.IO;
using System.Linq;
using System.Reflection;
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
		public readonly RulesetCache RulesetCache;
		public ILoadScreen LoadScreen { get; private set; }
		public VoxelLoader VoxelLoader { get; private set; }
		public CursorProvider CursorProvider { get; private set; }
		public FS ModFiles = new FS();

		readonly Lazy<Ruleset> defaultRules;
		public Ruleset DefaultRules { get { return defaultRules.Value; } }

		public ModData(string mod, bool useLoadScreen = false)
		{
			Languages = new string[0];
			Manifest = new Manifest(mod);

			// Allow mods to load types from the core Game assembly, and any additional assemblies they specify.
			var assemblies =
				new[] { typeof(Game).Assembly }.Concat(
					Manifest.Assemblies.Select(path => Assembly.LoadFrom(Platform.ResolvePath(path))));
			ObjectCreator = new ObjectCreator(assemblies);
			Manifest.LoadCustomData(ObjectCreator);

			if (useLoadScreen)
			{
				LoadScreen = ObjectCreator.CreateObject<ILoadScreen>(Manifest.LoadScreen.Value);
				LoadScreen.Init(Manifest, Manifest.LoadScreen.ToDictionary(my => my.Value));
				LoadScreen.Display();
			}

			WidgetLoader = new WidgetLoader(this);
			RulesetCache = new RulesetCache(this);
			RulesetCache.LoadingProgress += HandleLoadingProgress;
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

			defaultRules = Exts.Lazy(() => RulesetCache.Load());

			initialThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
		}

		// HACK: Only update the loading screen if we're in the main thread.
		int initialThreadId;
		void HandleLoadingProgress(object sender, EventArgs e)
		{
			if (LoadScreen != null && System.Threading.Thread.CurrentThread.ManagedThreadId == initialThreadId)
				LoadScreen.Display();
		}

		public void MountFiles()
		{
			ModFiles.LoadFromManifest(Manifest);
		}

		public void InitializeLoaders()
		{
			// all this manipulation of static crap here is nasty and breaks
			// horribly when you use ModData in unexpected ways.
			ChromeMetrics.Initialize(Manifest.ChromeMetrics);
			ChromeProvider.Initialize(Manifest.Chrome);

			if (VoxelLoader != null)
				VoxelLoader.Dispose();
			VoxelLoader = new VoxelLoader();

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

			var partial = Manifest.Translations.Select(MiniYaml.FromFile).Aggregate(MiniYaml.MergePartial);
			Languages = partial.Select(t => t.Key).ToArray();

			var yaml = MiniYaml.Merge(map.TranslationDefinitions, partial);
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

			// Operate on a copy of the map to avoid gameplay state leaking into the cache
			var map = new Map(MapCache[uid].Map.Path);

			LoadTranslations(map);

			// Reinitialize all our assets
			InitializeLoaders();
			ModFiles.LoadFromManifest(Manifest);

			// Mount map package so custom assets can be used. TODO: check priority.
			ModFiles.Mount(ModFiles.OpenPackage(map.Path, null, int.MaxValue));

			using (new Support.PerfTimer("Map.PreloadRules"))
				map.PreloadRules();
			using (new Support.PerfTimer("Map.SequenceProvider.Preload"))
				map.SequenceProvider.Preload();

			// Load music with map assets mounted
			using (new Support.PerfTimer("Map.Music"))
				foreach (var entry in map.Rules.Music)
					entry.Value.Load();

			VoxelProvider.Initialize(Manifest.VoxelSequences, map.VoxelSequenceDefinitions);
			VoxelLoader.Finish();

			return map;
		}

		public void Dispose()
		{
			if (LoadScreen != null)
				LoadScreen.Dispose();
			RulesetCache.Dispose();
			MapCache.Dispose();
			if (VoxelLoader != null)
				VoxelLoader.Dispose();
		}
	}

	public interface ILoadScreen : IDisposable
	{
		void Init(Manifest m, Dictionary<string, string> info);
		void Display();
		void StartGame(Arguments args);
	}
}
