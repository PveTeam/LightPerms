using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Maintenance.Managers;
using Microsoft.Extensions.Configuration;
using NLog;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Steamworks;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.Managers.PatchManager;
using Torch.Utils;
using VRage.Game;
using VRage.GameServices;
using VRage.Library.Utils;
using VRage.Utils;

namespace Maintenance.Patches;

[PatchShim]
public static class SteamQueryPatch
{
    private static readonly ILogger Log = LogManager.GetCurrentClassLogger();
    
    [ReflectedMethodInfo(null, "SteamServerEntryPoint", TypeName = "VRage.Steam.MySteamGameServer, VRage.Steam")]
    private static readonly MethodInfo EntryPointMethod = null!;

    [ReflectedMethodInfo(typeof(SteamQueryPatch), nameof(Prefix))]
    private static readonly MethodInfo PrefixMethod = null!;

#pragma warning disable CS0618 // Type or member is obsolete
    private static ITorchBase Torch => TorchBase.Instance;
#pragma warning restore CS0618 // Type or member is obsolete
    
    private static bool MaintenanceEnabled =>
        Torch.CurrentSession?.Managers.GetManager<MaintenanceManager>().MaintenanceEnabled is true;

    public static void Patch(PatchContext context)
    {
        context.GetPattern(EntryPointMethod).Prefixes.Add(PrefixMethod);
    }
    
    private static bool Prefix(IMyGameServer __instance, object argument)
    {
        var socket = (Socket)argument;
        RunServerAsync(__instance, socket);

        return false;
    }
    
    private static async void RunServerAsync(IMyGameServer server, Socket socket)
    {
        var localEndPoint = socket.LocalEndPoint;
        var zeroEndPoint = new IPEndPoint(0L, 0);
        // better to calc based on mtu but i dont care
        var buffer = new byte[1500];

        while (server.Running)
        {
            SocketReceiveFromResult result;
            try
            {
                result = await socket.ReceiveFromAsync(new(buffer), SocketFlags.None, zeroEndPoint);
            }
            catch (SocketException ex)
            {
                if (!server.Running)
                    break;

                try
                {
                    socket.Close();
                }
                catch
                {
                    // ignored
                }

                Log.Warn($"Received socket exception with error code: {ex.ErrorCode}, {ex.SocketErrorCode}", ex);
                Log.Info("Attempting to create new socket.");
                socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                try
                {
                    socket.Bind(localEndPoint);
                    continue;
                }
                catch (SocketException e)
                {
                    Log.Fatal(e, "Error binding server endpoint");

                    socket.Close();
                    server.GetType().GetProperty("Running")?.SetValue(server, false);
                    GameServer.Shutdown();
                    break;
                }
            }

            if (!CheckHeader(buffer.AsSpan(0, 4)))
                continue;

            if (result.ReceivedBytes > 4 && MySession.Static is not null)
            {
                Debug.Write("byte is ");
                Debug.WriteLine(buffer[4].ToString("X"));
                switch (buffer[4])
                {
                    case 0x54:
                        await HandleInfoRequestAsync(socket, result.RemoteEndPoint);
                        continue;
                    // players request without challenge 
                    case 0x55 when result.ReceivedBytes > 8 && !CheckHeader(buffer.AsSpan(5, 4)):
                        await HandlePlayersRequestAsync(socket, result.RemoteEndPoint);
                        continue;
                }
            }

            var remoteEndPoint = (IPEndPoint)result.RemoteEndPoint;

#pragma warning disable CS0618
            SteamGameServer.HandleIncomingPacket(buffer, result.ReceivedBytes, (uint)remoteEndPoint.Address.Address,
                                                 (ushort)remoteEndPoint.Port);
#pragma warning restore CS0618

            int length;
            while ((length = SteamGameServer.GetNextOutgoingPacket(buffer, buffer.Length, out var addr, out var port)) >
                   0)
            {
#pragma warning disable CS0618
                remoteEndPoint.Address.Address = addr;
#pragma warning restore CS0618
                remoteEndPoint.Port = port;

                await socket.SendToAsync(new(buffer, 0, length), SocketFlags.None, remoteEndPoint);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CheckHeader(Span<byte> header) =>
        Unsafe.ReadUnaligned<uint>(ref header.GetPinnableReference()) == 0xFFFFFFFF;

    private static Task HandlePlayersRequestAsync(Socket socket, EndPoint sender)
    {
        if (Sync.Players is null)
            return Task.CompletedTask;

        Debug.WriteLine($"players request from {sender}");
        using var ms = new MemoryStream();

        // header
        ms.WriteNoAlloc(0xFFFFFFFF);
        ms.WriteByte(0x44);
        
        if (MaintenanceEnabled)
        {
            ms.WriteByte(0); // count
            return SendAsync(ms, socket, sender);
        }

        var players = Sync.Clients.GetClients().Where(b => !b.IsLocal).Select(b => (b.DisplayName,
            (float?)(DateTime.Now - Sync.Players.TryGetIdentity((long)b.SteamUserId)?.LastLoginTime)?.TotalSeconds ??
            0f)).ToArray();

        var count = (byte)players.Length;

        // total
        ms.WriteByte(count);

        for (byte i = 0; i < count; i++)
        {
            // i
            ms.WriteByte(i);

            var (name, duration) = players[i];

            // name
            WriteUtf8String(ms, name);

            // score
            ms.WriteNoAlloc(0U);

            // duration
            ms.WriteNoAlloc(duration);
        }

        return SendAsync(ms, socket, sender);
    }

    private static Task HandleInfoRequestAsync(Socket socket, EndPoint sender)
    {
        if (Sync.Players is null)
            return Task.CompletedTask;

        Debug.WriteLine($"info request from {sender}");
        using var ms = new MemoryStream();

        var mp = MyMultiplayer.Static;

        // header
        ms.WriteNoAlloc(0xFFFFFFFF);
        ms.WriteByte(0x49);

        // protocol
        ms.WriteByte(0x11);
        
        var maintenanceEnabled = MaintenanceEnabled;
        
        // name
        WriteUtf8String(ms, mp.HostName);

        if (maintenanceEnabled)
        {
            var config = Torch.Managers.GetManager<ConfigManager>().Configuration;
            var formatter = Torch.Managers.GetManager<LanguageManager>().Formatter;

            if (config.GetValue<bool>(ConfigKeys.EnableWorldMessage))
            {
                var randomItem = config.GetSection(ConfigKeys.WorldMessage).GetChildren().Select(b => b.Value!).ToArray()
                    .GetRandomItem();
                var schedule = Torch.CurrentSession.Managers.GetManager<MaintenanceScheduleManager>().CurrentSchedule;
                
                // map
                WriteUtf8String(ms, schedule.EndTime.HasValue ? formatter.Format(randomItem, schedule) : randomItem);
            }
            else
            {
                // map
                WriteUtf8String(ms, mp.WorldName);
            }
        }
        else
        {
            // map
            WriteUtf8String(ms, mp.WorldName);
        }

        // folder
        WriteUtf8String(ms, "Space Engineers");

        // game full name
        WriteUtf8String(ms, "Space Engineers");

        // app id
        ms.WriteNoAlloc((short)0);

        // players
        ms.WriteByte(maintenanceEnabled ? default : (byte)(Sync.Clients.Count - 1));

        // max players
        ms.WriteByte((byte)(maintenanceEnabled ? 0x00 : mp.MaxPlayers));

        // bots
        ms.WriteByte(0x00);

        // server type
        ms.WriteByte((byte)'d');

        // env
        ms.WriteByte((byte)'w');

        // visibility
        ms.WriteByte(0x00);

        // vac
        ms.WriteByte(0x01);

        // version
        WriteUtf8String(ms, MyFinalBuildConstants.APP_VERSION.Version.ToString());

        // edf (GameID SteamID Keywords Port)
        ms.WriteByte(177);

        // edf port
        ms.WriteNoAlloc((short)((IPEndPoint)socket.LocalEndPoint).Port);

        // edf steam id
        ms.WriteNoAlloc(Sync.MyId);

        // edf keywords
        var gameMode = mp.GameMode is MyGameModeEnum.Survival
            ? $"S{mp.InventoryMultiplier}-{mp.BlocksInventoryMultiplier}-{mp.AssemblerMultiplier}-{mp.RefineryMultiplier}"
            : "C";
        WriteUtf8String(
            ms,
            $"groupId{MySandboxGame.ConfigDedicated.GroupID} version{MyFinalBuildConstants.APP_VERSION} datahasheRRN1/jJ7J2ZlR7GB1D5PDzn0sQ= mods{mp.ModCount} gamemode{gameMode} view{mp.SyncDistance}");

        // edf game id
        ms.WriteNoAlloc((ulong)244850);

        return SendAsync(ms, socket, sender);
    }

    private static async Task SendAsync(MemoryStream ms, Socket socket, EndPoint sender)
    {
        Debug.WriteLine(string.Join(" ", ms.GetBuffer().Take((int)ms.Length).Select(b => b.ToString("X"))));

        const int packetSize = 1200;

        if (ms.Length < packetSize)
        {
            await socket.SendToAsync(new(ms.GetBuffer(), 0, (int)ms.Length), SocketFlags.None, sender);
            return;
        }

        var id = (uint)MyRandom.Instance.Next();
        id &= ~(1 << 31);

        var msLength = ms.Length - 4;
        for (var i = 0; i < msLength; i += packetSize)
        {
            var length = Math.Min(packetSize, (int)msLength - i);

            var buffer = new byte[length + 10];

            // header
            Unsafe.WriteUnaligned(ref buffer.AsSpan(0, 4).GetPinnableReference(), 0xFFFFFFFE);

            // id
            Unsafe.WriteUnaligned(ref buffer.AsSpan(4, 4).GetPinnableReference(), id);

            // total
            buffer[8] = (byte)Math.Ceiling((float)msLength / packetSize);

            // number
            buffer[9] = (byte)(i / packetSize);

            ms.GetBuffer().AsSpan(i + 4, length).CopyTo(buffer.AsSpan(10));

            await socket.SendToAsync(new(buffer), SocketFlags.None, sender);
        }
    }

    private static unsafe void WriteUtf8String(Stream stream, string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            stream.WriteByte(0x00);
            return;
        }

        var chars = str.AsSpan();
        Span<byte> bytes = stackalloc byte[Encoding.UTF8.GetByteCount(str)];

        var length = Encoding.UTF8.GetBytes((char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(chars)),
                                            chars.Length,
                                            (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(bytes)),
                                            bytes.Length);

        stream.WriteNoAlloc((byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(bytes)), 0, length);
        stream.WriteByte(0x00);
    }
}