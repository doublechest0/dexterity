using System;
using System.Collections.Generic;
using System.Text;
using Content.Server.Disposal.Unit.Components;
using Content.Server.Hands.Components;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using static Content.Shared.Disposal.Components.SharedDisposalRouterComponent;

namespace Content.Server.Disposal.Tube.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IDisposalTubeComponent))]
    public sealed class DisposalRouterComponent : DisposalJunctionComponent, IActivate
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        [ViewVariables]
        private readonly HashSet<string> _tags = new();

        [ViewVariables]
        public bool Anchored =>
            !_entMan.TryGetComponent(Owner, out IPhysBody? physics) ||
            physics.BodyType == BodyType.Static;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(DisposalRouterUiKey.Key);

        [DataField("clickSound")] private SoundSpecifier _clickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        public override Direction NextDirection(DisposalHolderComponent holder)
        {
            var directions = ConnectableDirections();

            if (holder.Tags.Overlaps(_tags))
            {
                return directions[1];
            }

            return _entMan.GetComponent<TransformComponent>(Owner).LocalRotation.GetDir();
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }

            UpdateUserInterface();
        }

        /// <summary>
        /// Handles ui messages from the client. For things such as button presses
        /// which interact with the  world and require server action.
        /// </summary>
        /// <param name="obj">A user interface message from the client.</param>
        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (obj.Session.AttachedEntity == null)
            {
                return;
            }

            var msg = (UiActionMessage) obj.Message;

            if (!Anchored)
                return;

            //Check for correct message and ignore maleformed strings
            if (msg.Action == UiAction.Ok && TagRegex.IsMatch(msg.Tags))
            {
                _tags.Clear();
                foreach (var tag in msg.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    _tags.Add(tag.Trim());
                    ClickSound();
                }
            }
        }

        /// <summary>
        /// Gets component data to be used to update the user interface client-side.
        /// </summary>
        /// <returns>Returns a <see cref="DisposalRouterUserInterfaceState"/></returns>
        private DisposalRouterUserInterfaceState GetUserInterfaceState()
        {
            if (_tags.Count <= 0)
            {
                return new DisposalRouterUserInterfaceState("");
            }

            var taglist = new StringBuilder();

            foreach (var tag in _tags)
            {
                taglist.Append(tag);
                taglist.Append(", ");
            }

            taglist.Remove(taglist.Length - 2, 2);

            return new DisposalRouterUserInterfaceState(taglist.ToString());
        }

        private void UpdateUserInterface()
        {
            var state = GetUserInterfaceState();
            UserInterface?.SetState(state);
        }

        private void ClickSound()
        {
            SoundSystem.Play(Filter.Pvs(Owner), _clickSound.GetSound(), Owner, AudioParams.Default.WithVolume(-2f));
        }

        /// <summary>
        /// Called when you click the owner entity with an empty hand. Opens the UI client-side if possible.
        /// </summary>
        /// <param name="args">Data relevant to the event such as the actor which triggered it.</param>
        void IActivate.Activate(ActivateEventArgs args)
        {
            if (!_entMan.TryGetComponent(args.User, out ActorComponent? actor))
            {
                return;
            }

            if (!_entMan.TryGetComponent(args.User, out HandsComponent? hands))
            {
                Owner.PopupMessage(args.User, Loc.GetString("disposal-router-window-tag-input-activate-no-hands"));
                return;
            }

            var activeHandEntity = hands.CurrentlyHeldEntity;
            if (activeHandEntity == null)
            {
                OpenUserInterface(actor);
            }
        }

        protected override void OnRemove()
        {
            UserInterface?.CloseAll();
            base.OnRemove();
        }

        public void OpenUserInterface(ActorComponent actor)
        {
            UpdateUserInterface();
            UserInterface?.Open(actor.PlayerSession);
        }
    }
}
