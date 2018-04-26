#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class SwitchShapePointsX : UpdateRule
	{
		public override string Name { get { return "Switch positive and negative X values on HitShape points"; } }
		public override string Description
		{
			get
			{
				return "Positive 'X' values on Rectangle shape 'TopLeft' and 'BottomRight',\n" +
					"Capsule shape 'PointA' and 'PointB', and Polygon shape 'Points' now mean 'forward'.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var hitShapes = actorNode.ChildrenMatching("HitShape");
			foreach (var hs in hitShapes)
			{
				var type = hs.LastChildMatching("Type");
				if (type != null)
				{
					if (type.Value.Value == "Capsule")
					{
						var pointA = type.LastChildMatching("PointA");
						var pointB = type.LastChildMatching("PointB");

						var pointAField = FieldLoader.GetValue<int2>("PointA", pointA.Value.Value);
						var pointBField = FieldLoader.GetValue<int2>("PointB", pointB.Value.Value);

						pointAField = new int2(-pointAField.X, pointAField.Y);
						pointBField = new int2(-pointBField.X, pointBField.Y);

						pointA.Value.Value = pointAField.ToString();
						pointB.Value.Value = pointBField.ToString();
					}
					else if (type.Value.Value == "Rectangle")
					{
						var topLeft = type.LastChildMatching("TopLeft");
						var bottomRight = type.LastChildMatching("BottomRight");

						var topLeftField = FieldLoader.GetValue<int2>("TopLeft", topLeft.Value.Value);
						var bottomRightField = FieldLoader.GetValue<int2>("BottomRight", bottomRight.Value.Value);

						topLeftField = new int2(-topLeftField.X, topLeftField.Y);
						bottomRightField = new int2(-bottomRightField.X, bottomRightField.Y);

						topLeft.Value.Value = topLeftField.ToString();
						bottomRight.Value.Value = bottomRightField.ToString();
					}
				}
			}

			yield break;
		}
	}
}
