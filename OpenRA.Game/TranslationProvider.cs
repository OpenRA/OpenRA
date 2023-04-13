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
	public static class TranslationProvider
	{
		// Ensure thread-safety.
		static readonly object SyncObject = new();
		static Translation modTranslation;
		static Translation mapTranslation;

		public static void Initialize(ModData modData, IReadOnlyFileSystem fileSystem)
		{
			lock (SyncObject)
			{
				modTranslation = new Translation(Game.Settings.Player.Language, modData.Manifest.Translations, fileSystem);
				mapTranslation = fileSystem is Map map && map.TranslationDefinitions != null
					? new Translation(Game.Settings.Player.Language, FieldLoader.GetValue<string[]>("value", map.TranslationDefinitions.Value), fileSystem)
					: null;
			}
		}

		public static string GetString(string key, IDictionary<string, object> args = null)
		{
			lock (SyncObject)
    		{
				// By prioritizing mod-level translations we prevent maps from overwriting translation keys. We do not want to
				// allow maps to change the UI nor any other strings not exposed to the map.
				if (modTranslation.TryGetString(key, out var message, args))
					return message;

				if (mapTranslation != null)
					return mapTranslation.GetString(key, args);

				return key;
			}
		}

		public static bool TryGetString(string key, out string message, IDictionary<string, object> args = null)
		{
			lock (SyncObject)
    		{
				// By prioritizing mod-level translations we prevent maps from overwriting translation keys. We do not want to
				// allow maps to change the UI nor any other strings not exposed to the map.
				if (modTranslation.TryGetString(key, out message, args))
					return true;

				if (mapTranslation != null && mapTranslation.TryGetString(key, out message, args))
					return true;

				return false;
			}
		}

		/// <summary>Should only be used by <see cref="MapPreview"/>.</summary>
		internal static bool TryGetModString(string key, out string message, IDictionary<string, object> args = null)
		{
			lock (SyncObject)
			{
				return modTranslation.TryGetString(key, out message, args);
			}
		}
	}
}
