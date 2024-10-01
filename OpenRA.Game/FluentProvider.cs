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

using System.Collections.Generic;
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

		public static string GetString(string key, IDictionary<string, object> args = null)
		{
			lock (SyncObject)
			{
				// By prioritizing mod-level fluent bundles we prevent maps from overwriting string keys. We do not want to
				// allow maps to change the UI nor any other strings not exposed to the map.
				if (modFluentBundle.TryGetString(key, out var message, args))
					return message;

				if (mapFluentBundle != null)
					return mapFluentBundle.GetString(key, args);

				return key;
			}
		}

		public static bool TryGetString(string key, out string message, IDictionary<string, object> args = null)
		{
			lock (SyncObject)
			{
				// By prioritizing mod-level bundle we prevent maps from overwriting string keys. We do not want to
				// allow maps to change the UI nor any other strings not exposed to the map.
				if (modFluentBundle.TryGetString(key, out message, args))
					return true;

				if (mapFluentBundle != null && mapFluentBundle.TryGetString(key, out message, args))
					return true;

				return false;
			}
		}

		/// <summary>Should only be used by <see cref="MapPreview"/>.</summary>
		internal static bool TryGetModString(string key, out string message, IDictionary<string, object> args = null)
		{
			lock (SyncObject)
			{
				return modFluentBundle.TryGetString(key, out message, args);
			}
		}
	}
}
