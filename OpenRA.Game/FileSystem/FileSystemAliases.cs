using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.FileSystem
{
	public class FileSystemAliases
	{
		public static Dictionary<string, string> Aliases = new Dictionary<string, string>();

		// Sets an alias for a given filename ("speech01.mix" -> "speech01")
		public static void Add(string alias, string filename)
		{
			if(!ContainsAlias(alias)) Aliases.Add(alias, filename);	
		}

		public static void RemoveByAlias(string alias)
		{
			if (Aliases.ContainsKey(alias))	Aliases.Remove(alias);
		}

		public static void RemoveByValue(string filename)
		{
			if (Aliases.ContainsValue(filename)) Aliases.Remove(GetAlias(filename));
		}

		public static bool ContainsAlias(string alias)	
		{ 
			return Aliases.ContainsKey(alias); 
		}
		
		public static bool ContainsValue(string filename) 
		{ 
			return Aliases.ContainsValue(filename); 
		}

		public static void Clear() { Aliases.Clear(); }

		// Returns the Alias for the filename ("speech01" -> "speech01.mix")
		public static string GetFileName(string alias) 
		{
			return Aliases[alias]; 
		}

		// Returns the fileame for the given alias ("Speech01.mix" -> "speech01")
		public static string GetAlias(string filename) 
		{
			var entry = "";

			foreach (var a in Aliases)
				if (a.Value == filename)
					entry = a.Key;
				else
					entry = null;
			return entry;
		}
	}
}
