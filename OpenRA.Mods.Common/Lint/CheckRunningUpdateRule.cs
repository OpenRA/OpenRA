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
using System.Linq;
using OpenRA.Mods.Common.UpdateRules;
using OpenRA.Mods.Common.UpdateRules.Rules;

namespace OpenRA.Mods.Common.Lint
{
	/// <summary>
	/// Checks the loading of YAML files for mods without ignoring comments and whitespace.
	/// </summary>
	sealed class CheckRunningUpdateRule : ILintRulesPass
	{
		void ILintRulesPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Ruleset rules)
		{
			try
			{
				var externalFilenames = new HashSet<string>();
				UpdateUtils.UpdateMod(modData, new MockUpdateRule(), out var allFiles, externalFilenames);

				foreach (var (package, file, nodes) in allFiles)
				{
					if (package == null)
						continue;

					var textData = nodes.WriteToString();
					var packagesStream = package.GetStream(file);
					var packageData = packagesStream.ReadAllText();
					if (!Enumerable.SequenceEqual(textData, packageData))
					{
						emitError($"Mock update rule has tried to modify file {file}.");

						var packageLines = packageData.Split(["\r\n", "\n"], StringSplitOptions.None).GetEnumerator();
						var textLines = textData.Split(["\r\n", "\n"], StringSplitOptions.None).GetEnumerator();

						var n = 1;
#pragma warning disable RCS1239
						while (packageLines.MoveNext() && textLines.MoveNext())
						{
							if ((string)packageLines.Current != (string)textLines.Current)
								emitError($"Syntax or whitespace mismatch detected at line {n}.");

							n++;
						}
#pragma warning restore RCS1239
					}
				}
			}
			catch (Exception ex)
			{
				emitError($"Mock update rule failed with message: {ex.Message}");
			}
		}
	}
}
