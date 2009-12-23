using System.Collections.Generic;
using OpenRa.Game.GameRules;
using OpenRa.Game.Graphics;
using System.Drawing;

namespace OpenRa.Game.Traits
{
	enum DamageState { Normal, Half, Dead };
	
	// depends on the order of pips in WorldRenderer.cs!
	enum PipType { Transparent, Green, Yellow, Red, Gray };
	enum TagType { None, Fake, Primary };

	struct Renderable
	{
		public readonly Sprite Sprite;
		public readonly float2 Pos;
		public readonly int Palette;
		public readonly int ZOffset;

		public Renderable(Sprite sprite, float2 pos, int palette, int zOffset)
		{
			Sprite = sprite;
			Pos = pos;
			Palette = palette;
			ZOffset = zOffset;
		}

		public Renderable(Sprite sprite, float2 pos, int palette)
			: this(sprite, pos, palette, 0) { }

		public Renderable WithPalette(int newPalette) { return new Renderable(Sprite, Pos, newPalette, ZOffset); }
		public Renderable WithZOffset(int newOffset) { return new Renderable(Sprite, Pos, Palette, newOffset); }
		public Renderable WithPos(float2 newPos) { return new Renderable(Sprite, newPos, Palette, ZOffset); }
	}
	
	interface ITick { void Tick(Actor self); }
	interface IRender { IEnumerable<Renderable> Render(Actor self); }
	interface INotifyDamage { void Damaged(Actor self, AttackInfo e); }
	interface INotifyBuildComplete { void BuildingComplete (Actor self); }
	interface INotifyProduction { void UnitProduced(Actor self, Actor other); }
	interface IOrder
	{
		Order IssueOrder( Actor self, int2 xy, MouseInput mi, Actor underCursor );
		void ResolveOrder( Actor self, Order order );
	}
	interface IProducer { bool Produce( Actor self, UnitInfo producee ); }
	interface IOccupySpace { IEnumerable<int2> OccupiedCells(); }
	interface INotifyAttack { void Attacking(Actor self); }
	interface IRenderModifier { IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r); }
	interface IDamageModifier { float GetDamageModifier(); }
	interface ISpeedModifier { float GetSpeedModifier(); }
	interface IPips { IEnumerable<PipType> GetPips(); }
	interface ITags { IEnumerable<TagType> GetTags(); }
	interface IMovement
	{
		UnitMovementType GetMovementType();
		bool CanEnterCell(int2 location);
	}
	
	interface ICrushable
	{
		bool IsCrushableByFriend();
		bool IsCrushableByEnemy();
		void OnCrush(Actor crusher);
		IEnumerable<UnitMovementType>CrushableBy();
	}
}
