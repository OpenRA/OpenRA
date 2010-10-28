#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Network;

namespace OpenRA.Traits
{
	// depends on the order of pips in WorldRenderer.cs!
	public enum PipType { Transparent, Green, Yellow, Red, Gray };
	public enum TagType { None, Fake, Primary };
	public enum Stance { Enemy, Neutral, Ally };

	public class AttackInfo
	{
		public Actor Attacker;
		public WarheadInfo Warhead;
		public int Damage;
		public DamageState DamageState;
		public DamageState PreviousDamageState;
		public bool DamageStateChanged;
	}

	public interface ITick { void Tick(Actor self); }
	public interface IRender { IEnumerable<Renderable> Render(Actor self); }
	public interface IIssueOrder
	{
		IEnumerable<IOrderTargeter> Orders { get; }
		Order IssueOrder( Actor self, IOrderTargeter order, Target target );
	}
	public interface IOrderTargeter
	{
		string OrderID { get; }
		int OrderPriority { get; }
		bool CanTargetUnit( Actor self, Actor target, bool forceAttack, bool forceMove, ref string cursor );
		bool CanTargetLocation( Actor self, int2 location, List<Actor> actorsAtLocation, bool forceAttack, bool forceMove, ref string cursor );
    }
    public interface IResolveOrder { void ResolveOrder(Actor self, Order order); }
    public interface IValidateOrder { bool OrderValidation(OrderManager orderManager, World world, int clientId, Order order);
    }
	public interface IOrderCursor { string CursorForOrder(Actor self, Order order); }
	public interface IOrderVoice { string VoicePhraseForOrder(Actor self, Order order); }

	public interface INotifySold { void Selling( Actor self );  void Sold( Actor self ); }
	public interface INotifyDamage { void Damaged(Actor self, AttackInfo e); }
	public interface INotifyBuildComplete { void BuildingComplete(Actor self); }
	public interface INotifyProduction { void UnitProduced(Actor self, Actor other, int2 exit); }
	public interface INotifyCapture { void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner); }
	public interface IAcceptSpy { void OnInfiltrate(Actor self, Actor spy); }
	public interface IStoreOre { int Capacity { get; }}

	public interface IDisable { bool Disabled { get; } }
	public interface IExplodeModifier { bool ShouldExplode(Actor self); }
	public interface INudge { void OnNudge(Actor self, Actor nudger); }

	public interface IRadarSignature
	{
		IEnumerable<int2> RadarSignatureCells(Actor self);
		Color RadarSignatureColor(Actor self);
	}
	
	public interface IVisibilityModifier { bool IsVisible(Actor self, Player byPlayer); }
	public interface IRadarColorModifier { Color RadarColorOverride(Actor self); }
	public interface IHasLocation
	{
		int2 PxPosition { get; }
	}

	public interface IOccupySpace : IHasLocation
	{
		int2 TopLeft { get; }
		IEnumerable<int2> OccupiedCells();
	}
	
	public static class IOccupySpaceExts
	{
		public static int2 NearestCellTo( this IOccupySpace ios, int2 other )
		{
			var nearest = ios.TopLeft;
			var nearestDistance = int.MaxValue;
			foreach( var cell in ios.OccupiedCells() )
			{
				var dist = ( other - cell ).LengthSquared;
				if( dist < nearestDistance )
				{
					nearest = cell;
					nearestDistance = dist;
				}
			}
			return nearest;
		}
	}

	public interface INotifyAttack { void Attacking(Actor self); }
	public interface IRenderModifier { IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r); }
	public interface IDamageModifier { float GetDamageModifier( WarheadInfo warhead ); }
	public interface ISpeedModifier { float GetSpeedModifier(); }
	public interface IFirepowerModifier { float GetFirepowerModifier(); }
	public interface IPalette { void InitPalette( WorldRenderer wr ); }
	public interface IPaletteModifier { void AdjustPalette(Dictionary<string,Palette> b); }
	public interface IPips { IEnumerable<PipType> GetPips(Actor self); }
	public interface ITags { IEnumerable<TagType> GetTags(); }

	public interface ITeleportable : IHasLocation /* crap name! */
	{
		bool CanEnterCell(int2 location);
		void SetPosition(Actor self, int2 cell);
		void SetPxPosition(Actor self, int2 px);
	}

	public interface IMove : ITeleportable
	{
		int Altitude { get; set; }
	}
	
	public interface IFacing
	{
		int ROT { get; }
		int Facing { get; set; }
		int InitialFacing { get; }
	}
	
	public interface IOffsetCenterLocation { float2 CenterOffset { get; } }
	public interface ICrushable
	{
		void OnCrush(Actor crusher);
		IEnumerable<string> CrushClasses { get; }
	}
		
	public struct Renderable
	{
		public readonly Sprite Sprite;
		public readonly float2 Pos;
		public readonly string Palette;
		public readonly int Z;
		public readonly int ZOffset;
	    public float Scale;

        public Renderable(Sprite sprite, float2 pos, string palette, int z, int zOffset)
        {
            Sprite = sprite;
            Pos = pos;
            Palette = palette;
            Z = z;
            ZOffset = zOffset;
            Scale = 1f; /* default */
        }
        public Renderable(Sprite sprite, float2 pos, string palette, int z, int zOffset, float scale)
        {
            Sprite = sprite;
            Pos = pos;
            Palette = palette;
            Z = z;
            ZOffset = zOffset;
            Scale = scale; /* default */
        }

		public Renderable(Sprite sprite, float2 pos, string palette, int z)
			: this(sprite, pos, palette, z, 0) { }

        public Renderable WithPalette(string newPalette) { return new Renderable(Sprite, Pos, newPalette, Z, ZOffset, Scale); }
        public Renderable WithZOffset(int newOffset) { return new Renderable(Sprite, Pos, Palette, Z, newOffset, Scale); }
		public Renderable WithPos(float2 newPos) { return new Renderable(Sprite, newPos, Palette, Z, ZOffset, Scale); }
	}

	public interface ITraitInfo { object Create(ActorInitializer init); }

	public class TraitInfo<T> : ITraitInfo where T : new() { public virtual object Create(ActorInitializer init) { return new T(); } }

	public interface ITraitPrerequisite<T> where T : ITraitInfo { }

	public interface INotifySelection { void SelectionChanged(); }
	public interface IWorldLoaded { void WorldLoaded(World w); }
	public interface ICreatePlayers { void CreatePlayers(World w); }

	public interface IBot { void Activate(Player p); }
	
	public interface IActivity
	{
		IActivity Tick(Actor self);
		void Cancel(Actor self);
		void Queue(IActivity activity);
		IEnumerable<float2> GetCurrentPath();
	}

	public interface IRenderOverlay { void Render( WorldRenderer wr ); }
	public interface INotifyIdle { void Idle(Actor self); }

	public interface IBlocksBullets { }

	public interface IPostRender { void RenderAfterWorld(WorldRenderer wr, Actor self); }

	public interface IPostRenderSelection { void RenderAfterWorld(WorldRenderer wr, Actor self); }
	public interface IPreRenderSelection { void RenderBeforeWorld(WorldRenderer wr, Actor self); }

	public interface ITraitNotSynced{} // Traits marked with NotSynced arent sync-checked

	public struct Target		// a target: either an actor, or a fixed location.
	{
		Actor actor;
		float2 pos;
		bool valid;

		public static Target FromActor(Actor a) { return new Target { actor = a, valid = true }; }
		public static Target FromPos(float2 p) { return new Target { pos = p, valid = true }; }
		public static Target FromCell(int2 c) { return new Target { pos = Util.CenterOfCell(c), valid = true }; }
		public static Target FromOrder(Order o)
		{
			return o.TargetActor != null 
				? Target.FromActor(o.TargetActor) 
				: Target.FromCell(o.TargetLocation);
		}

		public static readonly Target None = new Target();

		public bool IsValid { get { return valid && (actor == null || actor.IsInWorld); } }
		public int2 PxPosition { get { return IsActor ? actor.Trait<IHasLocation>().PxPosition : pos.ToInt2(); } }
		public float2 CenterLocation { get { return PxPosition; } }

		public Actor Actor { get { return IsActor ? actor : null; } }
		public bool IsActor { get { return actor != null && !actor.Destroyed; } }
	}
}
