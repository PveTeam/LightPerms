using ProtoBuf;

namespace BuildAndRepair.Torch;

[ProtoContract(UseProtoMembersOnly = true)]
public class SyncTargetEntityData
{
    [ProtoMember(1)]
    public SyncEntityId Entity { get; set; }

    [ProtoMember(2)]
    public double Distance { get; set; }
}