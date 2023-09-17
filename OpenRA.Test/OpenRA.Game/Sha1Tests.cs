using NUnit.Framework;
using OpenRA.Primitives;

namespace OpenRA.Test
{
	[TestFixture]
	sealed class Sha1Tests
	{
		/// <summary>
		/// https://en.wikipedia.org/wiki/SHA-1#Examples_and_pseudocode.
		/// </summary>
		/// <param name="input">The input string.</param>
		/// <param name="expected">The expected hex string of the SHA1.</param>
		[TestCase("The quick brown fox jumps over the lazy dog", "2fd4e1c67a2d28fced849ee1bb76e7391b93eb12")]
		[TestCase("The quick brown fox jumps over the lazy cog", "de9f2c7fd25e1b3afad3e85a0bd17d9b100db4b3")]
		[TestCase("", "da39a3ee5e6b4b0d3255bfef95601890afd80709")]
		public void Sha1HexConvert(string input, string expected)
		{
			var actual = CryptoUtil.SHA1Hash(input);

			Assert.AreEqual(expected, actual);
		}

		[TestCase(0xFF0000FF, "0000FF")]
		[TestCase(0xFF00FFFF, "00FFFF")]
		[TestCase(0xFFFF00FF, "FF00FF")]
		[TestCase(0xAAFF00FF, "FF00FFAA")]
		public void ColorsToHex(uint value, string expected)
		{
			var color = Color.FromArgb(value);
			var actual = color.ToString();
			Assert.AreEqual(expected, actual);
		}
	}
}
