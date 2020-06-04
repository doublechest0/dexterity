using Content.Shared.GameObjects.Components.Weapons.Melee;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Weapons.Melee
{
    [RegisterComponent]
    public sealed class MeleeWeaponArcAnimationComponent : Component
    {
        public override string Name => "MeleeWeaponArcAnimation";

        private MeleeWeaponAnimationPrototype _meleeWeaponAnimation;

        private float _timer;
        private SpriteComponent _sprite;
        private Angle _baseAngle;
        private IEntity _attacker;

        public override void Initialize()
        {
            base.Initialize();

            _sprite = Owner.GetComponent<SpriteComponent>();
        }

        public void SetData(MeleeWeaponAnimationPrototype prototype, Angle baseAngle, IEntity attacker)
        {
            _meleeWeaponAnimation = prototype;
            _sprite.AddLayer(new RSI.StateId(prototype.State));
            _baseAngle = baseAngle;
            _attacker = attacker;
        }

        internal void Update(float frameTime)
        {
            if (_meleeWeaponAnimation == null)
            {
                return;
            }

            _timer += frameTime;

            var (r, g, b, a) =
                Vector4.Clamp(_meleeWeaponAnimation.Color + _meleeWeaponAnimation.ColorDelta * _timer, Vector4.Zero, Vector4.One);
            _sprite.Color = new Color(r, g, b, a);

            if (_attacker != null && _attacker.IsValid())
            {
                Owner.Transform.GridPosition = _attacker.Transform.GridPosition;
            }

            switch (_meleeWeaponAnimation.ArcType)
            {
                case WeaponArcType.Slash:
                    var angle = Angle.FromDegrees(_meleeWeaponAnimation.Width)/2;
                    Owner.Transform.LocalRotation =
                        _baseAngle + Angle.Lerp(-angle, angle, (float) (_timer / _meleeWeaponAnimation.Length.TotalSeconds));
                    break;

                case WeaponArcType.Poke:
                    _sprite.Offset += (_meleeWeaponAnimation.Speed * frameTime, 0);
                    break;
            }


            if (_meleeWeaponAnimation.Length.TotalSeconds <= _timer)
            {
                Owner.Delete();
            }
        }
    }
}
