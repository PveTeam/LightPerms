using ProtoBuf;

namespace BuildAndRepair.Torch.Messages;

[ProtoContract(SkipConstructor = true, UseProtoMembersOnly = true)]
public class MsgModDataRequest
{
    [ProtoMember(1)]
    public ulong SteamId { get; set; }
}