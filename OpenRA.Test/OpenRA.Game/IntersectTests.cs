using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using OpenRA.Traits;

namespace OpenRA.Test
{
	[TestFixture]
	class IntersectTests
	{
		readonly int2[] intersectPassData1 = { new int2(0, 0), new int2(4, 4), new int2(4, 0), new int2(0, 4) };
		readonly int2[] intersectPassData2 = { new int2(0, int.MinValue), new int2(0, int.MaxValue), new int2(int.MinValue, 0), new int2(int.MaxValue, 0) };
		readonly int2[] intersectFailData1 = { new int2(0, 0), new int2(0, 4), new int2(1, 1), new int2(1, 4) };
		readonly int2[] intersectFailData2 = { new int2(0, 0), new int2(0, 4), new int2(0, 0), new int2(0, 5) };

		readonly int2 passPoint1 = new int2(2, 2);
		readonly int2 passPoint2 = new int2(0, 0);

		readonly int2[] lineCastLineData1 = { new int2(1, 1), new int2(4, 4) };
		readonly int2[] lineCastPointData1 = { new int2(1, 1), new int2(2, 2), new int2(3, 3), new int2(4, 4) };
		readonly int2[] lineCastLineData2 = { new int2(0, 0), new int2(0, 3) };
		readonly int2[] lineCastPointData2 = { new int2(0, 0), new int2(0, 1), new int2(0, 2), new int2(0, 3) };

		readonly int2 mapSize = new int2(50, 50);

		[Test]
		public void IntersectPoint()
		{
			Assert.AreEqual(passPoint1, Util.IntersectionPoint(intersectPassData1[0], intersectPassData1[1], intersectPassData1[2], intersectPassData1[3]));
			Assert.AreEqual(passPoint2, Util.IntersectionPoint(intersectPassData2[0], intersectPassData2[1], intersectPassData2[2], intersectPassData2[3]));
			Assert.Null(Util.IntersectionPoint(intersectFailData1[0], intersectFailData1[1], intersectFailData1[2], intersectFailData1[3]));
			Assert.Null(Util.IntersectionPoint(intersectFailData2[0], intersectFailData2[1], intersectFailData2[2], intersectFailData2[3]));
		}

		[Test]
		public void IntersectLineCast()
		{
			Assert.AreEqual(lineCastPointData1, Util.LineCast(lineCastLineData1[0], lineCastLineData1[1], new int2(50, 50)).ToArray());
			Assert.AreEqual(lineCastPointData2, Util.LineCast(lineCastLineData2[0], lineCastLineData2[1], new int2(50, 50)).ToArray());
		}
	}
}
