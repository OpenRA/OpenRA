using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace OpenRa
{
	public class AssetInfo
	{
		public readonly string Filename;
		public readonly string Hash;

		public AssetInfo(string filename, string hash)
		{
			Filename = filename;
			Hash = hash;
		}

		static string GetHash(string filename)
		{
			using (var csp = SHA1.Create())
				return new string(csp.ComputeHash(File.ReadAllBytes(filename))
					.SelectMany(a => a.ToString("x2")).ToArray());
		}

		public static AssetInfo FromLocalFile(string filename)
		{
			return new AssetInfo(filename, GetHash(filename));
		}

		/* todo: perf: cache this */
		public bool IsPresent() { return File.Exists(Filename) && (Hash == GetHash(Filename)); }
	}
}
