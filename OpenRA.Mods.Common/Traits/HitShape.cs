#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.HitShapes;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Shape of actor for damage calculations.")]
	public class HitShapeInfo : ConditionalTraitInfo
	{
		[FieldLoader.LoadUsing("LoadShape")]
		public readonly IHitShape Type;

		static object LoadShape(MiniYaml yaml)
		{
			IHitShape ret;

			var shapeNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Type");
			var shape = shapeNode != null ? shapeNode.Value.Value : string.Empty;

			if (!string.IsNullOrEmpty(shape))
			{
				try
				{
					ret = Game.CreateObject<IHitShape>(shape + "Shape");
					FieldLoader.Load(ret, shapeNode.Value);
				}
				catch (YamlException e)
				{
					throw new YamlException("HitShape {0}: {1}".F(shape, e.Message));
				}
			}
			else
				ret = new CircleShape();

			ret.Initialize();
			return ret;
		}

		public override object Create(ActorInitializer init) { return new HitShape(init.Self, this); }
	}

	public class HitShape : ConditionalTrait<HitShapeInfo>
	{
		public HitShape(Actor self, HitShapeInfo info)
			: base(info) { }
	}
}
