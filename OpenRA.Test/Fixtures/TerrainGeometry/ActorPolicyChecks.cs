// SPDX-License-Identifier: GPL-3.0-or-later
#if ACTOR_POLICY_PROBE
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.TerrainProbe;

public sealed partial class TerrainRuntimeProbe
{
    Actor? gatedActor, gatedIdle;
    PolicyCountingActivity? counting;
    int activityTicks, idleTicks, traitTicks;
    readonly List<(Actor Passenger, bool Survives)> policyPassengers = [];
    void ActorPolicyChecks(int tick)
    {
        if (tick == 250)
        {
            gatedActor = Spawn("actorpolicyunit", center + new CVec(0, 15));
            gatedIdle = Spawn("actorpolicyunit", center + new CVec(1, 15));
            counting = new PolicyCountingActivity(); gatedActor.QueueActivity(counting);
        }
        if (tick == 255)
        {
            activityTicks = counting!.Ticks;
            var idle = gatedIdle!.Trait<ActorPolicyObserver>(); idleTicks = idle.IdleTicks; traitTicks = idle.TraitTicks;
            gatedActor!.Trait<ActorPolicyObserver>().Prevent = true; idle.Prevent = true;
        }
        if (tick == 260)
        {
            var idle = gatedIdle!.Trait<ActorPolicyObserver>();
            Check("policy-noninterruptible-activity-suspended", activityTicks > 0 && counting!.Ticks == activityTicks);
            Check("policy-idle-suspended", idle.IdleTicks == idleTicks);
            Check("policy-trait-ticks-continue", idle.TraitTicks >= traitTicks + 4);
            gatedActor!.Trait<ActorPolicyObserver>().Prevent = false; idle.Prevent = false;
        }
        if (tick == 265)
        {
            Check("policy-activity-resumes", counting!.Ticks > activityTicks);
            Check("policy-idle-resumes", gatedIdle!.Trait<ActorPolicyObserver>().IdleTicks > idleTicks);
        }
        if (tick == 275)
        {
            foreach (var (type, allow, survives) in new[] { ("plaincargo", true, true), ("policycargo", true, true), ("policycargo", false, false), ("deniedcargo", true, false) })
            {
                var parent = Spawn(type, center + new CVec(policyPassengers.Count, 18));
                var policy = parent.TraitOrDefault<ActorPolicyObserver>(); if (policy != null) policy.AllowEjection = allow;
                var passenger = world.CreateActor(false, "e1", new OpenRA.Primitives.TypeDictionary { new OwnerInit(parent.Owner) });
                parent.Trait<Cargo>().Load(parent, passenger); policyPassengers.Add((passenger, survives));
                parent.Kill(world.WorldActor);
            }
        }
        if (tick == 280)
            for (var i = 0; i < policyPassengers.Count; i++)
            {
                var (passenger, survives) = policyPassengers[i];
                Check("policy-cargo-ejection-" + i, (passenger.IsInWorld && !passenger.IsDead && !passenger.Disposed) == survives);
            }
    }
}
public sealed class ActorPolicyObserverInfo : TraitInfo
{ public override object Create(ActorInitializer init) => new ActorPolicyObserver(); }
public sealed class ActorPolicyObserver : IPreventActivity, ICargoEjectionPolicy, INotifyIdle, ITick, ISync
{
    [VerifySync] public bool Prevent, AllowEjection = true;
    [VerifySync] public int IdleTicks, TraitTicks;
    bool IPreventActivity.PreventActivity(Actor self) => Prevent;
    bool ICargoEjectionPolicy.CanEjectOnDeath(Actor self, AttackInfo attack) => AllowEjection;
    void INotifyIdle.TickIdle(Actor self) => IdleTicks++;
    void ITick.Tick(Actor self) => TraitTicks++;
}
public sealed class DenyCargoEjectionInfo : TraitInfo
{ public override object Create(ActorInitializer init) => new DenyCargoEjection(); }
public sealed class DenyCargoEjection : ICargoEjectionPolicy
{ bool ICargoEjectionPolicy.CanEjectOnDeath(Actor self, AttackInfo attack) => false; }
public sealed class PolicyCountingActivity : Activity
{
    public int Ticks;
    public PolicyCountingActivity() { IsInterruptible = false; }
    public override bool Tick(Actor self) { Ticks++; return false; }
}
#endif
