
namespace OpenRa.Game.Traits
{
	class TakeCoverInfo : ITraitInfo
	{
		public object Create(Actor self) { return new TakeCover(self); }
	}

	// infantry prone behavior
	class TakeCover : ITick, INotifyDamage, IDamageModifier, ISpeedModifier
	{
		const int defaultProneTime = 100;	/* ticks, =4s */
		const float proneDamage = .5f;
		const float proneSpeed = .5f;

		[Sync]
		int remainingProneTime = 0;

		public bool IsProne { get { return remainingProneTime > 0; } }

		public TakeCover(Actor self) {}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.Damage > 0)		/* fix to allow healing via `damage` */
				remainingProneTime = defaultProneTime;
		}

		public void Tick(Actor self)
		{
			if (IsProne)
				--remainingProneTime;
		}

		public float GetDamageModifier()
		{
			return IsProne ? proneDamage : 1f;
		}

		public float GetSpeedModifier()
		{
			return IsProne ? proneSpeed : 1f;
		}
	}
}
