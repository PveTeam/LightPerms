using ProtoBuf;

namespace BuildAndRepair.Torch.Messages;

[ProtoContract(SkipConstructor = true, UseProtoMembersOnly = true)]
public class MsgBlockSettings
{
    [ProtoMember(1)]
    public long EntityId { get; set; }

    [ProtoMember(2)]
    public SyncBlockSettings Settings { get; set; }
}