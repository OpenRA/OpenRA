#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common
{
	public class NetGraphWidget : Widget
	{
		private const float OrderLatencyScale = 25;

		public override void Draw()
		{
			var cr = Game.Renderer.RgbaColorRenderer;
			var rect = RenderBounds;
			var origin = new float2(rect.Right, rect.Bottom);
			var basis = new float2(
				(float)rect.Width / (float)NetHistory.NetHistoryLength,
				-(float)rect.Height / OrderLatencyScale);

			cr.DrawLine(new[]
			{
				new float3(rect.Left, rect.Top, 0),
				new float3(rect.Left, rect.Bottom, 0),
				new float3(rect.Right, rect.Bottom, 0)
			}, 1, Color.White);

			var u = new float2(rect.Left, rect.Bottom);

			using (var it = NetHistory.GetHistory().GetEnumerator())
			{
				if (it.MoveNext())
				{
					var previous = it.Current;

					for (int i = 0; it.MoveNext(); i++)
					{
						// Catchup
						cr.DrawLine(
							u + new float2(i, previous.CatchUpNetFrames) * basis,
							u + new float2(i + 1, it.Current.CatchUpNetFrames) * basis,
							1, Color.Aqua);

						// Order latency
						cr.DrawLine(
							u + new float2(i, previous.MeasuredLatency * 0.01f) * basis,
							u + new float2(i + 1, it.Current.MeasuredLatency * 0.01f) * basis,
							1, it.Current.Ticked ? Color.Green : (it.Current.CurrentClientBufferSize > 0 ? Color.Orange : Color.Red));

						previous = it.Current;
					}
				}
			}
		}
	}
}
