namespace BuildAndRepair.Torch;

[Flags]
public enum VisualAndSoundEffects
{
    WeldingVisualEffect = 0x00000001,
    WeldingSoundEffect = 0x00000010,
    GrindingVisualEffect = 0x00000100,
    GrindingSoundEffect = 0x00001000,
    TransportVisualEffect = 0x00010000
}