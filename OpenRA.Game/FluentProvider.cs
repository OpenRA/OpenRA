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

using OpenRA.FileSystem;

namespace OpenRA
{
	public static class FluentProvider
	{
		// Ensure thread-safety.
		static readonly object SyncObject = new();
		static FluentBundle modFluentBundle;
		static FluentBundle mapFluentBundle;

		public static void Initialize(ModData modData, IReadOnlyFileSystem fileSystem)
		{
			lock (SyncObject)
			{
				modFluentBundle = new FluentBundle(Game.Settings.Player.Language, modData.Manifest.Translations, fileSystem);
				mapFluentBundle = fileSystem is Map map && map.TranslationDefinitions != null
					? new FluentBundle(Game.Settings.Player.Language, FieldLoader.GetValue<string[]>("value", map.TranslationDefinitions.Value), fileSystem)
					: null;
			}
		}

		public static string GetMessage(string key, params object[] args)
		{
			lock (SyncObject)
			{
				// By prioritizing mod-level fluent bundles we prevent maps from overwriting string keys. We do not want to
				// allow maps to change the UI nor any other strings not exposed to the map.
				if (modFluentBundle.TryGetMessage(key, out var message, args))
					return message;

				if (mapFluentBundle != null)
					return mapFluentBundle.GetMessage(key, args);

				return key;
			}
		}

		public static bool TryGetMessage(string key, out string message, params object[] args)
		{
			lock (SyncObject)
			{
				// By prioritizing mod-level bundle we prevent maps from overwriting string keys. We do not want to
				// allow maps to change the UI nor any other strings not exposed to the map.
				if (modFluentBundle.TryGetMessage(key, out message, args))
					return true;

				if (mapFluentBundle != null && mapFluentBundle.TryGetMessage(key, out message, args))
					return true;

				return false;
			}
		}

		/// <summary>Should only be used by <see cref="MapPreview"/>.</summary>
		internal static bool TryGetModMessage(string key, out string message, params object[] args)
		{
			lock (SyncObject)
			{
				return modFluentBundle.TryGetMessage(key, out message, args);
			}
		}
	}
}
