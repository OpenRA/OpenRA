using System;

namespace OpenRA
{
	public interface ILog
	{
		void Write(string channel, string format, params object[] args);
	}

	public class LogProxy : ILog 
	{
		public void Write(string channel, string format, params object[] args)
		{
			Log.Write(channel, format, args);
		}
	}
}
