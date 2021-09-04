using Content.Server.Damage.Components;
using Content.Shared.Damage;
using Content.Shared.Throwing;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Damage
{
    public class DamageOtherOnHitSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        
        public override void Initialize()
        {
            SubscribeLocalEvent<DamageOtherOnHitComponent, ThrowDoHitEvent>(OnDoHit);
        }

        private void OnDoHit(EntityUid uid, DamageOtherOnHitComponent component, ThrowDoHitEvent args)
        {
            // Get damage from component, and apply to the target.
            _damageableSystem.TryChangeDamage(args.Target, component.Damage, component.IgnoreResistances);
        }
    }
}
