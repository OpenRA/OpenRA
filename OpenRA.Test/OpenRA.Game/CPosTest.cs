using NUnit.Framework;

namespace OpenRA.Test
{
	[TestFixture]
	public class CPosTest
	{
		[TestCase(TestName = "Packing x,y and layer into int")]
		public void PackUnpackBits()
		{
			var values = new int[] { -2048, -1024, 0, 1024, 2047 };
			var layerValues = new byte[] { 0, 128, 255 };

			foreach (var x in values)
			{
				foreach (var y in values)
				{
					foreach (var layer in layerValues)
					{
						var cell = new CPos(x, y, layer);

						Assert.AreEqual(x, cell.X);
						Assert.AreEqual(y, cell.Y);
						Assert.AreEqual(layer, cell.Layer);
					}
				}
			}
		}
	}
}