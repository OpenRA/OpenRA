#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Linq;

namespace OpenRA.PostProcess
{
	class Program
	{
		static void Main(string[] args)
		{
			var assembly = args.First();
			var flags = args.Skip(1).ToArray();

			Console.WriteLine("Post-processing {0}", assembly);
			var data = File.ReadAllBytes(assembly);
			var peOffset = BitConverter.ToInt32(data, 0x3c);

			foreach (var flag in flags)
			{
				if (flag == "-LAA")
				{
					// Set /LARGEADDRESSAWARE Flag (Application can handle large (>2GB) addresses)
					Console.WriteLine(" - Enabling /LARGEADDRESSAWARE");
					data[peOffset + 4 + 18] |= 0x20;
				}
				else
					Console.WriteLine("error: Unknown flag '{0}'", flag);
			}

			if (flags.Length == 0)
				Console.WriteLine("error: No flags were specified");

			File.WriteAllBytes(args[0], data);
		}
	}
}
