using System.Collections.Generic;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Radio.Components
{
    [RegisterComponent]
    public class RadioListenerComponent : Component, IListen, IRadio, IExamine
    {
        public override string Name => "Headset";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("channels")]
        public List<int> Channels = new(){ 1459 };

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("broadcastChannel")]
        public int BroadcastFrequency { get; set; } = 1459;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("listenRange")]
        public int ListenRange { get; private set; } = 1;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("enabled")]
        public bool Enabled = true;

        public void Receive(string message, int channel, IEntity source)
        {
            if (Owner.TryGetContainer(out var container))
            {
                if (!container.Owner.TryGetComponent(out ActorComponent? actor))
                    return;

                var playerChannel = actor.PlayerSession.ConnectedClient;

                var msg = _netManager.CreateNetMessage<MsgChatMessage>();

                msg.Channel = ChatChannel.Radio;
                msg.Message = message;
                //Square brackets are added here to avoid issues with escaping
                msg.MessageWrap = Loc.GetString("chat-radio-message-wrap", ("channel", $"\\[{channel}\\]"), ("name", source.Name));
                _netManager.ServerSendMessage(msg, playerChannel);
            }
        }

        public void Listen(string message, IEntity speaker)
        {
            Broadcast(message, speaker);
        }

        public void Broadcast(string message, IEntity speaker)
        {
            _radioSystem.SpreadMessage(this, speaker, message, BroadcastFrequency);
            RadioRequested = false;
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddText(Loc.GetString("examine-radio-frequency", ("frequency", BroadcastFrequency)));
            message.AddText("\n");
            message.AddText(Loc.GetString("examine-headset"));
            message.AddText("\n");
            message.AddText(Loc.GetString("examine-headset-chat-prefix", ("prefix", ";")));
        }
    }
}
