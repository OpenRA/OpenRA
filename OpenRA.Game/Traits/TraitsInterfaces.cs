#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Network;

namespace OpenRA.Traits
{
	// depends on the order of pips in WorldRenderer.cs!
	public enum PipType { Transparent, Green, Yellow, Red, Gray, Blue };
	public enum TagType { None, Fake, Primary };
	public enum Stance { Enemy, Neutral, Ally };

	public class AttackInfo
	{
		public Actor Attacker;
		public WarheadInfo Warhead;
		public int Damage;
		public DamageState DamageState;
		public DamageState PreviousDamageState;
	}

	public interface ITick { void Tick(Actor self); }
	public interface ITickRender { void TickRender(WorldRenderer wr, Actor self); }
	public interface IRender { IEnumerable<Renderable> Render(Actor self, WorldRenderer wr); }
	public interface IAutoSelectionSize { int2 SelectionSize(Actor self); }

	public interface IIssueOrder
	{
		IEnumerable<IOrderTargeter> Orders { get; }
		Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued);
	}

	public interface IOrderTargeter
	{
		string OrderID { get; }
		bool IsImmediate { get; }
		int OrderPriority { get; }
		bool CanTargetActor(Actor self, Actor target, bool forceAttack, bool forceQueue, ref string cursor);
		bool CanTargetLocation(Actor self, CPos location, List<Actor> actorsAtLocation, bool forceAttack, bool forceQueue, ref string cursor);
		bool IsQueued { get; }
	}

	public interface IResolveOrder { void ResolveOrder(Actor self, Order order); }
	public interface IValidateOrder { bool OrderValidation(OrderManager orderManager, World world, int clientId, Order order); }
	public interface IOrderVoice { string VoicePhraseForOrder(Actor self, Order order); }
	public interface INotify { void Play(Player p, string notification); }
	public interface INotifySold { void Selling(Actor self); void Sold(Actor self); }
	public interface INotifyDamage { void Damaged(Actor self, AttackInfo e); }
	public interface INotifyDamageStateChanged { void DamageStateChanged(Actor self, AttackInfo e); }
	public interface INotifyKilled { void Killed(Actor self, AttackInfo e); }
	public interface INotifyAppliedDamage { void AppliedDamage(Actor self, Actor damaged, AttackInfo e); }
	public interface INotifyBuildComplete { void BuildingComplete(Actor self); }
	public interface INotifyProduction { void UnitProduced(Actor self, Actor other, CPos exit); }
	public interface INotifyOwnerChanged { void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner); }
	public interface INotifyCapture { void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner); }
	public interface INotifyOtherCaptured { void OnActorCaptured(Actor self, Actor captured, Actor captor, Player oldOwner, Player newOwner); }
	public interface IAcceptInfiltrator { void OnInfiltrate(Actor self, Actor infiltrator); }
	public interface IStoreOre { int Capacity { get; } }
	public interface IToolTip
	{
		string Name();
		Player Owner();
		Stance Stance();
	}

	public interface IDisable { bool Disabled { get; } }
	public interface IExplodeModifier { bool ShouldExplode(Actor self); }
	public interface IHuskModifier { string HuskActor(Actor self); }

	public interface IRadarSignature
	{
		IEnumerable<CPos> RadarSignatureCells(Actor self);
		Color RadarSignatureColor(Actor self);
	}

	public interface IVisibilityModifier { bool IsVisible(Actor self, Player byPlayer); }
	public interface IRadarColorModifier { Color RadarColorOverride(Actor self); }
	public interface IHasLocation { PPos PxPosition { get; } }

	public interface IOccupySpace : IHasLocation
	{
		CPos TopLeft { get; }
		IEnumerable<Pair<CPos, SubCell>> OccupiedCells();
	}

	public static class IOccupySpaceExts
	{
		public static CPos NearestCellTo(this IOccupySpace ios, CPos other)
		{
			var nearest = ios.TopLeft;
			var nearestDistance = int.MaxValue;
			foreach (var cell in ios.OccupiedCells())
			{
				var dist = (other - cell.First).LengthSquared;
				if (dist < nearestDistance)
				{
					nearest = cell.First;
					nearestDistance = dist;
				}
			}
			return nearest;
		}
	}

	public interface INotifyAttack { void Attacking(Actor self, Target target); }
	public interface IRenderModifier { IEnumerable<Renderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<Renderable> r); }
	public interface IDamageModifier { float GetDamageModifier(Actor attacker, WarheadInfo warhead); }
	public interface ISpeedModifier { decimal GetSpeedModifier(); }
	public interface IFirepowerModifier { float GetFirepowerModifier(); }
	public interface IPalette { void InitPalette(WorldRenderer wr); }
	public interface IPaletteModifier { void AdjustPalette(Dictionary<string, Palette> b); }
	public interface IPips { IEnumerable<PipType> GetPips(Actor self); }
	public interface ITags { IEnumerable<TagType> GetTags(); }
	public interface ISelectionBar { float GetValue(); Color GetColor(); }

	public interface ITeleportable : IHasLocation /* crap name! */
	{
		bool CanEnterCell(CPos location);
		void SetPosition(Actor self, CPos cell);
		void SetPxPosition(Actor self, PPos px);
		void AdjustPxPosition(Actor self, PPos px);	/* works like SetPxPosition, but visual only */
	}

	public interface IMove : ITeleportable { int Altitude { get; set; } }
	public interface INotifyBlockingMove { void OnNotifyBlockingMove(Actor self, Actor blocking); }

	public interface IFacing
	{
		int ROT { get; }
		int Facing { get; set; }
		int InitialFacing { get; }
	}

	public interface IFacingInfo {}		/* tag interface for infoclasses whose corresponding trait has IFacing */

	public interface ICrushable
	{
		void OnCrush(Actor crusher);
		void WarnCrush(Actor crusher);
		bool CrushableBy(string[] crushClasses, Player owner);
	}

	public struct Renderable
	{
		public readonly Sprite Sprite;
		public readonly float2 Pos;
		public readonly PaletteReference Palette;
		public readonly int Z;
		public readonly int ZOffset;
		public float Scale;

		public Renderable(Sprite sprite, float2 pos, PaletteReference palette, int z, int zOffset, float scale)
		{
			Sprite = sprite;
			Pos = pos;
			Palette = palette;
			Z = z;
			ZOffset = zOffset;
			Scale = scale; /* default */
		}

		public Renderable(Sprite sprite, float2 pos, PaletteReference palette, int z)
			: this(sprite, pos, palette, z, 0, 1f) { }

		public Renderable(Sprite sprite, float2 pos, PaletteReference palette, int z, float scale)
			: this(sprite, pos, palette, z, 0, scale) { }

		public Renderable WithScale(float newScale) { return new Renderable(Sprite, Pos, Palette, Z, ZOffset, newScale); }
		public Renderable WithPalette(PaletteReference newPalette) { return new Renderable(Sprite, Pos, newPalette, Z, ZOffset, Scale); }
		public Renderable WithZOffset(int newOffset) { return new Renderable(Sprite, Pos, Palette, Z, newOffset, Scale); }
		public Renderable WithPos(float2 newPos) { return new Renderable(Sprite, newPos, Palette, Z, ZOffset, Scale); }
	}

	public interface ITraitInfo { object Create(ActorInitializer init); }

	public class TraitInfo<T> : ITraitInfo where T : new() { public virtual object Create(ActorInitializer init) { return new T(); } }

	public interface Requires<T> where T : class { }
	public interface UsesInit<T> where T : IActorInit { }

	public interface INotifySelection { void SelectionChanged(); }
	public interface IWorldLoaded { void WorldLoaded(World w); }
	public interface ICreatePlayers { void CreatePlayers(World w); }

	public interface IBotInfo { string Name { get; } }
	public interface IBot
	{
		void Activate(Player p);
		IBotInfo Info { get; }
	}

	public interface IRenderOverlay { void Render(WorldRenderer wr); }
	public interface INotifyIdle { void TickIdle(Actor self); }

	public interface IBlocksBullets { }

	public interface IPostRender { void RenderAfterWorld(WorldRenderer wr, Actor self); }

	public interface IPostRenderSelection { void RenderAfterWorld(WorldRenderer wr); }
	public interface IPreRenderSelection { void RenderBeforeWorld(WorldRenderer wr, Actor self); }
	public interface IRenderAsTerrain { IEnumerable<Renderable> RenderAsTerrain(WorldRenderer wr, Actor self); }
	public interface ILocalCoordinatesModel
	{
		WVec LocalToWorld(WVec vec);
		WRot QuantizeOrientation(Actor self, WRot orientation);
	}
	public interface LocalCoordinatesModelInfo {}

	public interface ITargetable
	{
		string[] TargetTypes { get; }
		IEnumerable<CPos> TargetableCells(Actor self);
		bool TargetableBy(Actor self, Actor byActor);
	}

	public interface INotifyStanceChanged
	{
		void StanceChanged(Actor self, Player a, Player b,
			Stance oldStance, Stance newStance);
	}

	public interface ILintPass { void Run(Action<string> emitError, Action<string> emitWarning); }

	public interface IObjectivesPanel { string ObjectivesPanel { get; } }
	
	public static class DisableExts
	{
		public static bool IsDisabled(this Actor a)
		{
			return a.TraitsImplementing<IDisable>().Any(d => d.Disabled);
		}
	}
}
