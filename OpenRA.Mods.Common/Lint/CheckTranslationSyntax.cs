#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
using Fluent.Net;
using Fluent.Net.Ast;

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
					var parser = new Parser();
					var resource = parser.Parse(reader);
					foreach (var entry in resource.Body)
					{
						if (entry is Junk junk)
							foreach (var annotation in junk.Annotations)
								emitError($"{annotation.Code}: {annotation.Message} in {file} line {annotation.Span.Start.Line}");

						if (entry is MessageTermBase message)
						{
							if (ids.Contains(message.Id.Name))
								emitWarning($"Duplicate ID `{message.Id.Name}` in {file} line {message.Span.Start.Line}");

							ids.Add(message.Id.Name);
						}
					}
				}
			}
		}
	}
}
