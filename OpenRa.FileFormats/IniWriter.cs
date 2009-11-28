using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace OpenRa.FileFormats
{
	public class IniWriter
	{
		readonly string Filename;

		public IniWriter(string filename) { Filename = Path.GetFullPath(filename); }

		public void Set(string section, string key, string value)
		{
			WritePrivateProfileString(section, key, value, Filename);
		}

		public string Get(string section, string key, string defaultValue)
		{
			var sb = new StringBuilder(1024);
			GetPrivateProfileString(section, key, defaultValue, sb, sb.Length, Filename);
			return sb.ToString();
		}

		public string Get(string section, string key)
		{
			return Get(section, key, "");
		}

		[DllImport("kernel32")]
		static extern int WritePrivateProfileString(string section, string key, string value, string filename);
		[DllImport("kernel32")]
		static extern int GetPrivateProfileString(string section, string key, string defaultValue, 
			StringBuilder value, int length, string filename);
	}
}
