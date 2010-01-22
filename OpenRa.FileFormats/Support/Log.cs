using System.IO;

namespace OpenRa
{
	public static class Log
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
