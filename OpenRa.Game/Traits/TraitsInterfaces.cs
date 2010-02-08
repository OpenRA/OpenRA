using System.Collections.Generic;
using System.Drawing;
using IjwFramework.Types;
using OpenRa.GameRules;
using OpenRa.Graphics;
using OpenRa.Traits.Activities;

namespace OpenRa.Traits
{
	public enum DamageState { Normal, Half, Dead };
	
	// depends on the order of pips in WorldRenderer.cs!
	public enum PipType { Transparent, Green, Yellow, Red, Gray };
	public enum TagType { None, Fake, Primary };

	public interface ITick { void Tick(Actor self); }
	public interface IRender { IEnumerable<Renderable> Render(Actor self); }
	public interface IIssueOrder { Order IssueOrder( Actor self, int2 xy, MouseInput mi, Actor underCursor ); }
	public interface IResolveOrder { void ResolveOrder(Actor self, Order order); }

	public interface INotifySold { void Selling( Actor self );  void Sold( Actor self ); }
	public interface INotifyDamage { void Damaged(Actor self, AttackInfo e); }
	public interface INotifyBuildComplete { void BuildingComplete(Actor self); }
	public interface INotifyProduction { void UnitProduced(Actor self, Actor other); }
	public interface IAcceptOre
	{
		void OnDock(Actor harv, DeliverOre dockOrder);
		int2 DeliverOffset { get; }
	}
	public interface IAcceptThief { void OnSteal(Actor self, Actor thief); }
	public interface IAcceptSpy { void OnInfiltrate(Actor self, Actor spy); }

	public interface ICustomTerrain { float GetCost(int2 p, UnitMovementType umt); }
	
	public interface IDisable {	bool Disabled { get; set; } }
	
	interface IProducer
	{
		bool Produce( Actor self, ActorInfo producee );
		void SetPrimaryProducer(Actor self, bool isPrimary);
	}
	public interface IOccupySpace { IEnumerable<int2> OccupiedCells(); }
	public interface INotifyAttack { void Attacking(Actor self); }
	public interface IRenderModifier { IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r); }
	public interface IDamageModifier { float GetDamageModifier(); }
	public interface ISpeedModifier { float GetSpeedModifier(); }
	public interface IPowerModifier { float GetPowerModifier(); }
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
	
	public interface ICrateAction
	{
		int SelectionShares { get; }
		void Activate(Actor collector);
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

	public interface ITraitInfo { object Create(Actor self); }

	public class StatelessTraitInfo<T> : ITraitInfo
		where T : new()
	{
		static Lazy<T> Instance = Lazy.New(() => new T());
		public object Create(Actor self) { return Instance.Value; }
	}

	public interface ITraitPrerequisite<T> { }

	public interface INotifySelection { void SelectionChanged(); }
}
