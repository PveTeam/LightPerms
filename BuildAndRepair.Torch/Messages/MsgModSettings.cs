using ProtoBuf;

namespace BuildAndRepair.Torch.Messages;

[ProtoContract(SkipConstructor = true, UseProtoMembersOnly = true)]
public class MsgModSettings
{
    [ProtoMember(2)]
    public SyncModSettings Settings { get; set; }
}