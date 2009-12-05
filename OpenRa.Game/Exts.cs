
namespace OpenRa.Game
{
	static class Exts
	{
		public static string F(this string fmt, params object[] args)
		{
			return string.Format(fmt, args);
		}
	}
}
