using OpenRA.FileFormats;
#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

namespace OpenRA.Traits
{
	class SharesCellInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new SharesCell(init); }
	}
	public class SharesCell : IOffsetCenterLocation
	{
		[Sync]
		public int Position;

		public SharesCell(ActorInitializer init)
		{
			Position = init.Contains<SubcellInit>() ? init.Get<SubcellInit,int>() : 0;
		}

		public float2 CenterOffset
		{ get {	
			switch (Position)
			{
				case 1:
					return new float2(-5f,-5f);
				case 2:
					return new float2(5f,-5f);
				case 3:
					return new float2(-5f,5f);
				case 4:
					return new float2(5f,5f);
				default:
					return new float2(-5f, -5f);
			}
		}}
	}
	
	public class SubcellInit : IActorInit<int>
	{
		[FieldFromYamlKey]
		public readonly int value = 0;
		
		public SubcellInit() { }
		
		public SubcellInit( int init )
		{
			value = init;
		}
		
		public int Value( World world )
		{
			return value;	
		}
	}
}
