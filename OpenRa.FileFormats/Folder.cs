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
			catch { return null; }
		}
	}
}
