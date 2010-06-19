#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.GameRules;
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	public enum DamageState { Normal, Half, Dead };
	
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
		public bool DamageStateChanged;
	}

	public interface ITick { void Tick(Actor self); }
	public interface IRender { IEnumerable<Renderable> Render(Actor self); }
	public interface IIssueOrder { Order IssueOrder( Actor self, int2 xy, MouseInput mi, Actor underCursor ); }
	public interface IResolveOrder { void ResolveOrder(Actor self, Order order); }

	public interface INotifySold { void Selling( Actor self );  void Sold( Actor self ); }
	public interface INotifyDamage { void Damaged(Actor self, AttackInfo e); }
	public interface INotifyBuildComplete { void BuildingComplete(Actor self); }
	public interface INotifyProduction { void UnitProduced(Actor self, Actor other); }
	public interface INotifyCapture { void OnCapture(Actor self, Actor captor); }
	public interface IAcceptSpy { void OnInfiltrate(Actor self, Actor spy); }
	public interface INotifyEnterCell { void OnEnterCell(Actor self, int2 cell); }

	public interface ICustomTerrain
	{
		float GetCost(int2 p, UnitMovementType umt);
		float GetSpeedModifier(int2 p, UnitMovementType umt);
	}

	public interface IDisable { bool Disabled { get; } }
	
	public interface IOccupySpace { IEnumerable<int2> OccupiedCells(); }
	public interface INotifyAttack { void Attacking(Actor self); }
	public interface IRenderModifier { IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r); }
	public interface IDamageModifier { float GetDamageModifier( WarheadInfo warhead ); }
	public interface ISpeedModifier { float GetSpeedModifier(); }
	public interface IPowerModifier { float GetPowerModifier(); }
	public interface IFirepowerModifier { float GetFirepowerModifier(); }
	public interface IPaletteModifier { void AdjustPalette(Bitmap b); }
	public interface IPips { IEnumerable<PipType> GetPips(Actor self); }
	public interface ITags { IEnumerable<TagType> GetTags(); }
	public interface IMovement
	{
		UnitMovementType GetMovementType();
		bool CanEnterCell(int2 location);
	}
	
	public interface ICrushable
	{
		void OnCrush(Actor crusher);
		bool IsCrushableBy(UnitMovementType umt, Player player);
		bool IsPathableCrush(UnitMovementType umt, Player player);
	}
		
	public struct Renderable
	{
		public readonly Sprite Sprite;
		public readonly float2 Pos;
		public readonly string Palette;
		public readonly int ZOffset;

		public Renderable(Sprite sprite, float2 pos, string palette, int zOffset)
		{
			Sprite = sprite;
			Pos = pos;
			Palette = palette;
			ZOffset = zOffset;
		}

		public Renderable(Sprite sprite, float2 pos, string palette)
			: this(sprite, pos, palette, 0) { }

		public Renderable WithPalette(string newPalette) { return new Renderable(Sprite, Pos, newPalette, ZOffset); }
		public Renderable WithZOffset(int newOffset) { return new Renderable(Sprite, Pos, Palette, newOffset); }
		public Renderable WithPos(float2 newPos) { return new Renderable(Sprite, newPos, Palette, ZOffset); }
	}

	public interface ITraitInfo { object Create(ActorInitializer init); }

	public class TraitInfo<T> : ITraitInfo where T : new() { public virtual object Create(ActorInitializer init) { return new T(); } }

	public interface ITraitPrerequisite<T> { }

	public interface INotifySelection { void SelectionChanged(); }
	public interface ILoadWorldHook { void WorldLoaded(World w); }
	public interface IGameStarted { void GameStarted(World w); }

	public interface IActivity
	{
		IActivity NextActivity { get; set; }
		IActivity Tick(Actor self);
		void Cancel(Actor self);
	}

	public interface IChromeButton
	{
		string Image { get; }
		bool Enabled { get; }
		bool Pressed { get; }
		void OnClick();
		string Description { get; }
		string LongDesc { get; }
	}

	public interface IRenderOverlay { void Render(); }
	public interface INotifyIdle { void Idle(Actor self); }

	public interface IVictoryConditions { bool HasLost { get; } bool HasWon { get; } }
	public interface IBlocksBullets { }
}
