using System.Collections.Generic;
using System.Drawing;
using IjwFramework.Types;
using OpenRa.Game.GameRules;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	enum DamageState { Normal, Half, Dead };
	
	// depends on the order of pips in WorldRenderer.cs!
	enum PipType { Transparent, Green, Yellow, Red, Gray };
	enum TagType { None, Fake, Primary };

	interface ITick { void Tick(Actor self); }
	interface IRender { IEnumerable<Renderable> Render(Actor self); }
	interface IIssueOrder { Order IssueOrder( Actor self, int2 xy, MouseInput mi, Actor underCursor ); }
	interface IResolveOrder { void ResolveOrder( Actor self, Order order ); }

	interface INotifySold { void Sold(Actor self); }
	interface INotifyDamage { void Damaged(Actor self, AttackInfo e); }
	interface INotifyBuildComplete { void BuildingComplete (Actor self); }
	interface INotifyProduction { void UnitProduced(Actor self, Actor other); }

	interface IAcceptThief { void OnSteal(Actor self, Actor thief); }
	interface IAcceptSpy { void OnInfiltrate(Actor self, Actor spy); }

	interface IProducer
	{
		bool Produce( Actor self, NewUnitInfo producee );
		void SetPrimaryProducer(Actor self, bool isPrimary);
	}
	interface IOccupySpace { IEnumerable<int2> OccupiedCells(); }
	interface INotifyAttack { void Attacking(Actor self); }
	interface IRenderModifier { IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r); }
	interface IDamageModifier { float GetDamageModifier(); }
	interface ISpeedModifier { float GetSpeedModifier(); }
	interface IPaletteModifier { void AdjustPalette(Bitmap b); }
	interface IPips { IEnumerable<PipType> GetPips(Actor self); }
	interface ITags { IEnumerable<TagType> GetTags(); }
	interface IMovement
	{
		UnitMovementType GetMovementType();
		bool CanEnterCell(int2 location);
	}
	
	interface ICrushable
	{
		void OnCrush(Actor crusher);
		bool IsCrushableBy(UnitMovementType umt, Player player);
		bool IsPathableCrush(UnitMovementType umt, Player player);
	}
	struct Renderable
	{
		public readonly Sprite Sprite;
		public readonly float2 Pos;
		public readonly PaletteType Palette;
		public readonly int ZOffset;

		public Renderable(Sprite sprite, float2 pos, PaletteType palette, int zOffset)
		{
			Sprite = sprite;
			Pos = pos;
			Palette = palette;
			ZOffset = zOffset;
		}

		public Renderable(Sprite sprite, float2 pos, PaletteType palette)
			: this(sprite, pos, palette, 0) { }

		public Renderable WithPalette(PaletteType newPalette) { return new Renderable(Sprite, Pos, newPalette, ZOffset); }
		public Renderable WithZOffset(int newOffset) { return new Renderable(Sprite, Pos, Palette, newOffset); }
		public Renderable WithPos(float2 newPos) { return new Renderable(Sprite, newPos, Palette, ZOffset); }
	}

	interface ITraitInfo { object Create(Actor self); }

	class StatelessTraitInfo<T> : ITraitInfo
		where T : new()
	{
		static Lazy<T> Instance = Lazy.New(() => new T());
		public object Create(Actor self) { return Instance.Value; }
	}

}
