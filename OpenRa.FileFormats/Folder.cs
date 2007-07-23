using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace OpenRa.FileFormats
{
	public class Folder : IFolder
	{
		readonly string path;

		public Folder(string path) { this.path = path; }

		public Stream GetContent(string filename)
		{
			try { return File.OpenRead(path + filename); }
			catch { throw new FileNotFoundException("File not found", filename); }
		}
	}
}
