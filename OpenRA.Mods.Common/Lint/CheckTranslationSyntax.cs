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

namespace OpenRA.Mods.Common.Lint
{
	class CheckTranslationSyntax : ILintPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, ModData modData)
		{
			foreach (var file in modData.Manifest.Translations)
			{
				var stream = modData.DefaultFileSystem.Open(file);
				using (var reader = new StreamReader(stream))
				{
					var ids = new List<string>();
					var parser = new LinguiniParser(reader);
					var resource = parser.Parse();
					foreach (var entry in resource.Entries)
					{
						if (entry is Junk junk)
							emitError($"{junk.GetId()}: {junk.AsStr()} in {file} {junk.Content}");

						if (entry is AstMessage message)
						{
							if (ids.Contains(message.Id.Name.ToString()))
								emitWarning($"Duplicate ID `{message.Id.Name}` in {file}.");

							ids.Add(message.Id.Name.ToString());
						}
					}
				}
			}
		}
	}
}
