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
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using OpenRA.FileSystem;

namespace OpenRA
{
	public static class FluentProvider
	{
		// Ensure thread-safety.
		static readonly Lock SyncObject = new();
		static FluentBundle modFluentBundle;
		static FluentBundle mapFluentBundle;

		public static void Initialize(Manifest manifest, IReadOnlyFileSystem fileSystem, string language = "en")
		{
			lock (SyncObject)
			{
				var allPaths = BuildLanguagePaths(manifest.FluentMessages, fileSystem, language);
				modFluentBundle = new FluentBundle(language, allPaths, fileSystem);
				if (fileSystem is Map map && map.FluentMessageDefinitions != null)
				{
					var files = ImmutableArray<string>.Empty;
					if (map.FluentMessageDefinitions.Value != null)
						files = FieldLoader.GetValue<ImmutableArray<string>>("value", map.FluentMessageDefinitions.Value);

					string text = null;
					if (map.FluentMessageDefinitions.Nodes.Length > 0)
					{
						var builder = new StringBuilder();
						foreach (var node in map.FluentMessageDefinitions.Nodes)
							if (node.Key == "base64")
								builder.Append(Encoding.UTF8.GetString(Convert.FromBase64String(node.Value.Value)));

						text = builder.ToString();
					}

					files = BuildLanguagePaths(files, fileSystem, language);
					mapFluentBundle = new FluentBundle(language, files, fileSystem, text);
				}
			}
		}

		// Later paths override earlier ones. English stays the fallback; language files overlay it.
		internal static ImmutableArray<string> BuildLanguagePaths(ImmutableArray<string> basePaths, IReadOnlyFileSystem fileSystem, string language)
		{
			if (string.IsNullOrEmpty(language) || language == "en")
				return basePaths;

			var paths = new List<string>(basePaths.Length * 2);
			paths.AddRange(basePaths);
			foreach (var basePath in basePaths)
			{
				var slash = basePath.LastIndexOf('/');
				var langPath = slash < 0
					? language + "/" + basePath
					: string.Concat(basePath.AsSpan(0, slash + 1), language, "/", basePath.AsSpan(slash + 1));

				if (fileSystem.Exists(langPath))
					paths.Add(langPath);
			}

			return paths.ToImmutableArray();
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
