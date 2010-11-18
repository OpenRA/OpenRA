using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Launcher
{
	public class Mod
	{
		public string Title { get; private set; }
		public string Version { get; private set; }
		public string Author { get; private set; }
		public string Description { get; private set; }
		public string Requires { get; private set; }
		public bool Standalone { get; private set; }

		public Mod(string title, string version, string author, string description, string requires, bool standalone)
		{
			Title = title;
			Version = version;
			Author = author;
			Description = description;
			Requires = requires;
			Standalone = standalone;
		}
	}
}
