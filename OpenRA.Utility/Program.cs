using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;

namespace OpenRA.Utility
{
	class Program
	{
		static void Main(string[] args)
		{
			for (int i = 0; i < args.Length; i++)
			{
				switch (args[i])
				{
					case "--list-mods":
						foreach (var m in Mod.AllMods.Select( x => x.Key ))
							Console.WriteLine(m);
						break;
					default:
						break;
				}
			}
		}
	}
}
