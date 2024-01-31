using ProtoBuf;

namespace ZLimits;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
public record LimitChangeMessage(bool IsIncrease, string BlockType, long IdentityId);