using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace OpenRa.Game
{
	static class Log
	{
		static StreamWriter writer = File.CreateText("log.txt");

		static Log()
		{
			writer.AutoFlush = true;
		}

		public static void Write(string format, params object[] args)
		{
			writer.WriteLine(format, args);
		}
	}
}
