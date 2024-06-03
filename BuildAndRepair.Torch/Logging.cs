using NLog;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace BuildAndRepair.Torch;

public class Logging(ILogger log)
{
    [Flags]
    public enum BlockNameOptions
    {
        None = 0x0000,
        IncludeTypename = 0x0001
    }

    [Flags]
    public enum Level
    {
        Error = 0x0001,
        Event = 0x0002,
        Info = 0x0004,
        Verbose = 0x0008,
        Special1 = 0x100000,
        Communication = 0x1000,
        All = 0xFFFF
    }

    public static string BlockName(object block, BlockNameOptions options = BlockNameOptions.IncludeTypename)
    {
        if (block is IMyInventory inventory)
            block = inventory.Owner;

        if (block is IMySlimBlock { FatBlock: not null } slimBlock1)
            block = slimBlock1.FatBlock;

        return block switch
        {
            IMySlimBlock slimBlock => $"{(slimBlock.CubeGrid != null ? slimBlock.CubeGrid.DisplayName : "Unknown Grid")}.{slimBlock.BlockDefinition.DisplayNameText}",
            IMyTerminalBlock terminalBlock => (options & BlockNameOptions.IncludeTypename) != 0
                ? $"{(terminalBlock.CubeGrid != null ? terminalBlock.CubeGrid.DisplayName : "Unknown Grid")}.{terminalBlock.CustomName} [{terminalBlock.BlockDefinition.TypeIdString}]"
                : $"{(terminalBlock.CubeGrid != null ? terminalBlock.CubeGrid.DisplayName : "Unknown Grid")}.{terminalBlock.CustomName}",
            IMyCubeBlock cubeBlock => $"{(cubeBlock.CubeGrid != null ? cubeBlock.CubeGrid.DisplayName : "Unknown Grid")} [{cubeBlock.BlockDefinition.TypeIdString}/{cubeBlock.BlockDefinition.SubtypeName}]",
            IMyEntity entity when (options & BlockNameOptions.IncludeTypename) != 0 => $"{(string.IsNullOrEmpty(entity.DisplayName) ? entity.GetFriendlyName() : entity.DisplayName)} ({entity.EntityId}) [{entity.GetType().Name}]",
            IMyEntity entity => $"{entity.DisplayName} ({entity.EntityId})",
            _ => block != null ? block.ToString() : "NULL"
        };
    }

    /// <summary>
    ///     Precheckl to avoid retriveing large amout of data,
    ///     that might be not needed afterwards
    /// </summary>
    /// <param name="level"></param>
    /// <returns></returns>
    public bool ShouldLog(Level level)
    {
        return log.IsEnabled(ConvertLogLevel(level));
    }

    /// <summary>
    /// </summary>
    public void IncreaseIndent(Level level)
    {
    }

    /// <summary>
    /// </summary>
    public void DecreaseIndent(Level level)
    {
    }

    /// <summary>
    /// </summary>
    public void ResetIndent(Level level)
    {
    }

    /// <summary>
    /// </summary>
    public void Error(Exception e)
    {
        log.Error(e);
    }

    /// <summary>
    /// </summary>
    public void Error(string msg, params object[] args)
    {
        log.Error(msg, args);
    }

    /// <summary>
    /// </summary>
    public void Write(Level level, string msg, params object[] args)
    {
        log.Log(ConvertLogLevel(level), msg, args);
    }

    private static LogLevel ConvertLogLevel(Level level)
    {
        return level switch
        {
            Level.Error => LogLevel.Error,
            Level.Event => LogLevel.Debug,
            Level.Info => LogLevel.Debug,
            Level.Verbose => LogLevel.Trace,
            Level.Special1 => LogLevel.Info,
            Level.Communication => LogLevel.Trace,
            _ => LogLevel.Info
        };
    }

    /// <summary>
    /// </summary>
    public void Write(string msg, params object[] args)
    {
        log.Info(msg, args);
    }
}