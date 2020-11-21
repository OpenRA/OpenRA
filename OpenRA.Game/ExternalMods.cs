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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA
{
	[Flags]
	enum ModRegistration { User = 1, System = 2 }

	public class ExternalMod
	{
		public readonly string Id;
		public readonly string Version;
		public readonly string Title;
		public readonly string LaunchPath;
		public readonly string[] LaunchArgs;
		public Sprite Icon { get; internal set; }
		public Sprite Icon2x { get; internal set; }
		public Sprite Icon3x { get; internal set; }

		public static string MakeKey(Manifest mod) { return MakeKey(mod.Id, mod.Metadata.Version); }
		public static string MakeKey(ExternalMod mod) { return MakeKey(mod.Id, mod.Version); }
		public static string MakeKey(string modId, string modVersion) { return modId + "-" + modVersion; }
	}

	public class ExternalMods : IReadOnlyDictionary<string, ExternalMod>
	{
		readonly Dictionary<string, ExternalMod> mods = new Dictionary<string, ExternalMod>();
		readonly SheetBuilder sheetBuilder;

		Sheet CreateSheet()
		{
			var sheet = new Sheet(SheetType.BGRA, new Size(512, 512));

			// We must manually force the buffer creation to avoid a crash
			// that is indirectly triggered by rendering from a Sheet that
			// has not yet been written to.
			sheet.CreateBuffer();
			sheet.GetTexture().ScaleFilter = TextureScaleFilter.Linear;

			return sheet;
		}

		public ExternalMods()
		{
			// Don't try to load mod icons if we don't have a texture to put them in
			if (Game.Renderer != null)
				sheetBuilder = new SheetBuilder(SheetType.BGRA, CreateSheet);

			// Several types of support directory types are available, depending on
			// how the player has installed and launched the game.
			// Read registration metadata from all of them
			var sources = Enum.GetValues(typeof(SupportDirType))
				.Cast<SupportDirType>()
				.Select(t => Platform.GetSupportDir(t))
				.Distinct();

			foreach (var source in sources)
			{
				var metadataPath = Path.Combine(source, "ModMetadata");
				if (!Directory.Exists(metadataPath))
					continue;

				foreach (var path in Directory.GetFiles(metadataPath, "*.yaml"))
				{
					try
					{
						var yaml = MiniYaml.FromStream(File.OpenRead(path), path).First().Value;
						LoadMod(yaml, path);
					}
					catch (Exception e)
					{
						Log.Write("debug", "Failed to parse mod metadata file '{0}'", path);
						Log.Write("debug", e.ToString());
					}
				}
			}
		}

		void LoadMod(MiniYaml yaml, string path = null, bool forceRegistration = false)
		{
			var mod = FieldLoader.Load<ExternalMod>(yaml);

			if (sheetBuilder != null)
			{
				var iconNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Icon");
				if (iconNode != null && !string.IsNullOrEmpty(iconNode.Value.Value))
					using (var stream = new MemoryStream(Convert.FromBase64String(iconNode.Value.Value)))
						mod.Icon = sheetBuilder.Add(new Png(stream));

				var icon2xNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Icon2x");
				if (icon2xNode != null && !string.IsNullOrEmpty(icon2xNode.Value.Value))
					using (var stream = new MemoryStream(Convert.FromBase64String(icon2xNode.Value.Value)))
						mod.Icon2x = sheetBuilder.Add(new Png(stream), 1f / 2);

				var icon3xNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Icon3x");
				if (icon3xNode != null && !string.IsNullOrEmpty(icon3xNode.Value.Value))
					using (var stream = new MemoryStream(Convert.FromBase64String(icon3xNode.Value.Value)))
						mod.Icon3x = sheetBuilder.Add(new Png(stream), 1f / 3);
			}

			// Avoid possibly overwriting a valid mod with an obviously bogus one
			var key = ExternalMod.MakeKey(mod);
			if ((forceRegistration || File.Exists(mod.LaunchPath)) && (path == null || Path.GetFileNameWithoutExtension(path) == key))
				mods[key] = mod;
		}

		internal void Register(Manifest mod, string launchPath, IEnumerable<string> launchArgs, ModRegistration registration)
		{
			if (mod.Metadata.Hidden)
				return;

			var key = ExternalMod.MakeKey(mod);
			var yaml = new MiniYamlNode("Registration", new MiniYaml("", new List<MiniYamlNode>()
			{
				new MiniYamlNode("Id", mod.Id),
				new MiniYamlNode("Version", mod.Metadata.Version),
				new MiniYamlNode("Title", mod.Metadata.Title),
				new MiniYamlNode("LaunchPath", launchPath),
				new MiniYamlNode("LaunchArgs", new[] { "Game.Mod=" + mod.Id }.Concat(launchArgs).JoinWith(", "))
			}));

			using (var stream = mod.Package.GetStream("icon.png"))
				if (stream != null)
					yaml.Value.Nodes.Add(new MiniYamlNode("Icon", Convert.ToBase64String(stream.ReadAllBytes())));

			using (var stream = mod.Package.GetStream("icon-2x.png"))
				if (stream != null)
					yaml.Value.Nodes.Add(new MiniYamlNode("Icon2x", Convert.ToBase64String(stream.ReadAllBytes())));

			using (var stream = mod.Package.GetStream("icon-3x.png"))
				if (stream != null)
					yaml.Value.Nodes.Add(new MiniYamlNode("Icon3x", Convert.ToBase64String(stream.ReadAllBytes())));

			var sources = new List<string>();
			if (registration.HasFlag(ModRegistration.System))
				sources.Add(Platform.GetSupportDir(SupportDirType.System));

			if (registration.HasFlag(ModRegistration.User))
			{
				sources.Add(Platform.GetSupportDir(SupportDirType.User));

				// If using the modern support dir we must also write the registration
				// to the legacy support dir for older engine versions, but ONLY if it exists
				var legacyPath = Platform.GetSupportDir(SupportDirType.LegacyUser);
				if (Directory.Exists(legacyPath))
					sources.Add(legacyPath);
			}

			// Make sure the mod is available for this session, even if saving it fails
			LoadMod(yaml.Value, forceRegistration: true);

			var lines = new List<MiniYamlNode> { yaml }.ToLines().ToArray();
			foreach (var source in sources.Distinct())
			{
				var metadataPath = Path.Combine(source, "ModMetadata");

				try
				{
					Directory.CreateDirectory(metadataPath);
					File.WriteAllLines(Path.Combine(metadataPath, key + ".yaml"), lines);
				}
				catch (Exception e)
				{
					Log.Write("debug", "Failed to register current mod metadata");
					Log.Write("debug", e.ToString());
				}
			}
		}

		/// <summary>
		/// Removes invalid mod registrations:
		/// * LaunchPath no longer exists
		/// * LaunchPath and mod id matches the active mod, but the version is different
		/// * Filename doesn't match internal key
		/// * Fails to parse as a mod registration
		/// </summary>
		internal void ClearInvalidRegistrations(ExternalMod activeMod, ModRegistration registration)
		{
			var sources = new List<string>();
			if (registration.HasFlag(ModRegistration.System))
				sources.Add(Platform.GetSupportDir(SupportDirType.System));

			if (registration.HasFlag(ModRegistration.User))
			{
				// User support dir may be using the modern or legacy value, or overridden by the user
				// Add all the possibilities and let the .Distinct() below ignore the duplicates
				sources.Add(Platform.GetSupportDir(SupportDirType.User));
				sources.Add(Platform.GetSupportDir(SupportDirType.ModernUser));
				sources.Add(Platform.GetSupportDir(SupportDirType.LegacyUser));
			}

			var activeModKey = ExternalMod.MakeKey(activeMod);
			foreach (var source in sources.Distinct())
			{
				var metadataPath = Path.Combine(source, "ModMetadata");
				if (!Directory.Exists(metadataPath))
					continue;

				foreach (var path in Directory.GetFiles(metadataPath, "*.yaml"))
				{
					string modKey = null;
					try
					{
						var yaml = MiniYaml.FromStream(File.OpenRead(path), path).First().Value;
						var m = FieldLoader.Load<ExternalMod>(yaml);
						modKey = ExternalMod.MakeKey(m);

						// Continue to the next entry if it is the active mod (even if the LaunchPath is bogus)
						if (modKey == activeModKey)
							continue;

						// Continue to the next entry if this one is valid
						if (File.Exists(m.LaunchPath) && Path.GetFileNameWithoutExtension(path) == modKey &&
							!(activeMod != null && m.LaunchPath == activeMod.LaunchPath && m.Id == activeMod.Id && m.Version != activeMod.Version))
							continue;
					}
					catch (Exception e)
					{
						Log.Write("debug", "Failed to parse mod metadata file '{0}'", path);
						Log.Write("debug", e.ToString());
					}

					// Remove from the ingame mod switcher
					if (Path.GetFileNameWithoutExtension(path) == modKey)
						mods.Remove(modKey);

					// Remove stale or corrupted metadata
					try
					{
						File.Delete(path);
						Log.Write("debug", "Removed invalid mod metadata file '{0}'", path);
					}
					catch (Exception e)
					{
						Log.Write("debug", "Failed to remove mod metadata file '{0}'", path);
						Log.Write("debug", e.ToString());
					}
				}
			}
		}

		internal void Unregister(Manifest mod, ModRegistration registration)
		{
			var sources = new List<string>();
			if (registration.HasFlag(ModRegistration.System))
				sources.Add(Platform.GetSupportDir(SupportDirType.System));

			if (registration.HasFlag(ModRegistration.User))
			{
				// User support dir may be using the modern or legacy value, or overridden by the user
				// Add all the possibilities and let the .Distinct() below ignore the duplicates
				sources.Add(Platform.GetSupportDir(SupportDirType.User));
				sources.Add(Platform.GetSupportDir(SupportDirType.ModernUser));
				sources.Add(Platform.GetSupportDir(SupportDirType.LegacyUser));
			}

			var key = ExternalMod.MakeKey(mod);
			mods.Remove(key);

			foreach (var source in sources.Distinct())
			{
				var path = Path.Combine(source, "ModMetadata", key + ".yaml");
				try
				{
					if (File.Exists(path))
						File.Delete(path);
				}
				catch (Exception e)
				{
					Log.Write("debug", "Failed to remove mod metadata file '{0}'", path);
					Log.Write("debug", e.ToString());
				}
			}
		}

		public ExternalMod this[string key] { get { return mods[key]; } }
		public int Count { get { return mods.Count; } }
		public ICollection<string> Keys { get { return mods.Keys; } }
		public ICollection<ExternalMod> Values { get { return mods.Values; } }
		public bool ContainsKey(string key) { return mods.ContainsKey(key); }
		public IEnumerator<KeyValuePair<string, ExternalMod>> GetEnumerator() { return mods.GetEnumerator(); }
		public bool TryGetValue(string key, out ExternalMod value) { return mods.TryGetValue(key, out value); }
		IEnumerator IEnumerable.GetEnumerator() { return mods.GetEnumerator(); }
	}
}
