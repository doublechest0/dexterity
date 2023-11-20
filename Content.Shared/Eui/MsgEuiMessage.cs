using System.IO;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Eui
{
    public sealed class MsgEuiMessage : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public uint Id;
        public EuiMessageBase Message = default!;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer ser)
        {
            Id = buffer.ReadUInt32();

            var len = buffer.ReadVariableInt32();
            using var stream = new MemoryStream();
            buffer.ReadAlignedMemory(stream, len);
            Message = ser.Deserialize<EuiMessageBase>(stream);
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer ser)
        {
            buffer.Write(Id);
            var stream = new MemoryStream();

            ser.Serialize(stream, Message);
            var length = (int)stream.Length;
            buffer.WriteVariableInt32(length);
            buffer.Write(stream.GetBuffer().AsSpan(0, length));
        }
    }
}
