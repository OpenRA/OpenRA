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
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class CheckMissingSprites : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--check-missing-sprites"; } }

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return true;
		}

		[Desc("Check tileset and sequence definitions for missing sprite files.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			var modData = Game.ModData = utility.ModData;
			var failed = false;

			// We need two levels of YamlException handling to provide the desired behaviour:
			// Parse errors within a single tileset should skip that tileset and allow the rest to be tested
			// however, certain errors will be thrown by the outer modData.DefaultSequences, which prevent
			// any tilesets from being checked further.
			try
			{
				// DefaultSequences is a dictionary of tileset: SequenceProvider
				// so we can also use this to key our tileset checks
				foreach (var kv in modData.DefaultSequences)
				{
					try
					{
						Console.WriteLine("Tileset: " + kv.Key);
						var tileset = modData.DefaultTileSets[kv.Key];
						var missingImages = new HashSet<string>();
						Action<uint, string> onMissingImage = (id, f) =>
						{
							Console.WriteLine("\tTemplate `{0}` references sprite `{1}` that does not exist.", id, f);
							missingImages.Add(f);
							failed = true;
						};

						var theater = new Theater(tileset, onMissingImage);
						foreach (var t in tileset.Templates)
						{
							for (var v = 0; v < t.Value.Images.Length; v++)
							{
								if (!missingImages.Contains(t.Value.Images[v]))
								{
									for (var i = 0; i < t.Value.TilesCount; i++)
									{
										if (t.Value[i] == null || theater.HasTileSprite(new TerrainTile(t.Key, (byte)i), v))
											continue;

										Console.WriteLine("\tTemplate `{0}` references frame {1} that does not exist in sprite `{2}`.", t.Key, i, t.Value.Images[v]);
										failed = true;
									}
								}
							}
						}

						foreach (var image in kv.Value.Images)
						{
							foreach (var sequence in kv.Value.Sequences(image))
							{
								var s = kv.Value.GetSequence(image, sequence) as FileNotFoundSequence;
								if (s == null)
									continue;

								Console.WriteLine("\tSequence `{0}.{1}` references sprite `{2}` that does not exist.", image, sequence, s.Filename);
								failed = true;
							}
						}
					}
					catch (YamlException e)
					{
						// The stacktrace associated with yaml errors are not very useful
						// Suppress them to make the lint output less intimidating for modders
						Console.WriteLine("\t{0}".F(e.Message));
						failed = true;
					}
					catch (Exception e)
					{
						Console.WriteLine("Failed with exception: {0}".F(e));
						failed = true;
					}
				}
			}
			catch (YamlException e)
			{
				// The stacktrace associated with yaml errors are not very useful
				// Suppress them to make the lint output less intimidating for modders
				Console.WriteLine("{0}".F(e.Message));
				failed = true;
			}

			if (failed)
				Environment.Exit(1);
		}
	}
}
