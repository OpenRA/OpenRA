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
using System.IO;
using Linguini.Syntax.Ast;
using Linguini.Syntax.Parser;
using OpenRA.FileSystem;

namespace OpenRA.Mods.Common.Lint
{
	sealed class CheckFluentSyntax : ILintPass, ILintMapPass
	{
		void ILintMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Map map)
		{
			if (map.FluentMessageDefinitions == null)
				return;

			Run(emitError, emitWarning, map, FieldLoader.GetValue<string[]>("value", map.FluentMessageDefinitions.Value));
		}

		void ILintPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData)
		{
			Run(emitError, emitWarning, modData.DefaultFileSystem, modData.Manifest.FluentMessages);
		}

		static void Run(Action<string> emitError, Action<string> emitWarning, IReadOnlyFileSystem fileSystem, IEnumerable<string> paths)
		{
			foreach (var path in paths)
			{
				var stream = fileSystem.Open(path);
				using (var reader = new StreamReader(stream))
				{
					var ids = new List<string>();
					var parser = new LinguiniParser(reader);
					var resource = parser.Parse();
					foreach (var entry in resource.Entries)
					{
						if (entry is Junk junk)
							emitError($"{junk.GetId()}: {junk.AsStr()} in {path} {junk.Content}.");

						if (entry is AstMessage message)
						{
							if (ids.Contains(message.Id.Name.ToString()))
								emitWarning($"Duplicate ID `{message.Id.Name}` in {path}.");

							ids.Add(message.Id.Name.ToString());
						}
					}
				}
			}
		}
	}
}
