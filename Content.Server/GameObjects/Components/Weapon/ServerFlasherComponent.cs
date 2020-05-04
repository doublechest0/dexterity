using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Weapons;
using Content.Shared.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Weapon
{
    /// <summary>
    /// Handheld flasher
    /// </summary>
    [RegisterComponent]
    public class ServerFlasherComponent : SharedFlasherComponent, IAfterAttack
    {
        public override string Name => "Flasher";
        public override uint? NetID => ContentNetIDs.FLASHER;
        private SoundComponent _soundComponent;
        private double _duration;
        private string _sound;
        private double _lightDuration;

        public override void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _duration, "duration", 8.0);
            serializer.DataField(ref _sound, "use_sound", "/Audio/weapons/flash.ogg");
            serializer.DataField(ref _lightDuration, "light_duration", 1.0);
        }

        protected override void Startup()
        {
            if (Owner.TryGetComponent(out SoundComponent soundComponent))
            {
                _soundComponent = soundComponent;
            }
        }

        public void AfterAttack(AfterAttackEventArgs eventArgs)
        {
            if (eventArgs.User.TryGetComponent(out CombatModeComponent combatModeComponent) &&
                !combatModeComponent.IsInCombatMode)
            {
                return;
            }

            if (eventArgs.Attacked != null && TryFlash(eventArgs.Attacked))
            {
                return;
            }

            var locManager = IoCManager.Resolve<ILocalizationManager>();
            Owner.PopupMessage(eventArgs.User, locManager.GetString("No effect"));
        }

        private bool TryFlash(IEntity entity)
        {
            if (!entity.TryGetComponent(out ServerFlashableComponent flashable))
            {
                return false;
            }

            flashable.Flash(_duration);
            SendNetworkMessage(new FlasherComponentMessage());
            BriefLightSystem.BriefLightHelper(Owner, _lightDuration);

            if (_soundComponent != null && _sound != null)
            {
                _soundComponent.Play(_sound);
            }

            return true;
        }
    }
}
