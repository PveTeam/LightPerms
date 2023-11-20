using java.util;
using LuckPerms.Torch.Extensions;
using LuckPerms.Torch.Utils.Extensions;
using me.lucko.luckperms.common.locale;
using me.lucko.luckperms.common.plugin;
using me.lucko.luckperms.common.sender;
using net.kyori.adventure.text;
using net.kyori.adventure.text.serializer.plain;
using net.luckperms.api.util;
using Torch.API;
using Torch.API.Managers;
using Torch.Commands;
using Torch.Managers;
using Torch.Managers.ChatManager;
using VRage.Game;

namespace LuckPerms.Torch.Impl;

public class LpSenderFactory(LuckPermsPlugin plugin) : SenderFactory(plugin), IManager
{
    [Manager.Dependency]
    private ChatManagerServer? _chatManager = null;

    [Manager.Dependency]
    private CommandManager? _commandManager = null;

    public Sender Wrap(object obj, CommandContext context) => wrap(new SenderWrapper(obj, context));

    protected override UUID getUniqueId(object obj)
    {
        if (obj is IPlayer player)
            return player.SteamId.GetUuid();
        
        return Sender.CONSOLE_UUID;
    }

    protected override string getName(object obj)
    {
        if (obj is IPlayer player)
            return player.Name;
        
        return Sender.CONSOLE_NAME;
    }

    protected override void sendMessage(object obj, Component c)
    {
        if (_chatManager is null)
            return;

        c = TranslationManager.render(c, Locale.ENGLISH); // TODO make use of client side lang

        var plainText = PlainTextComponentSerializer.plainText().serialize(c);
        
        switch (obj)
        {
            case IPlayer player:
                _chatManager.SendMessageAsOther("LuckPerms", plainText, targetSteamId: player.SteamId);
                break;
            case SenderWrapper wrapper:
                wrapper.Context.Respond(plainText, "LuckPerms");
                break;
            default:
                _chatManager.DisplayMessageOnSelf("LuckPerms", plainText, MyFontEnum.White);
                break;
        }
    }

    protected override Tristate getPermissionValue(object obj, string str)
    {
        if (obj is not IPlayer)
            return Tristate.UNDEFINED;
        
        return Tristate.UNDEFINED; // idk what to do here cuz torch/se doesnt have any decent permissions at all
    }

    protected override bool hasPermission(object obj, string str) => getPermissionValue(obj, str).asBoolean();

    protected override void performCommand(object obj, string str)
    {
        if (_commandManager is null)
            return;
        
        if (obj is IPlayer player)
        {
            var consumed = false;
            _commandManager.HandleCommand(str, player.SteamId, ref consumed);
        }
        else _commandManager.HandleCommandFromServer(str, _ => { });
    }

    protected override bool isConsole(object obj)
    {
        return obj switch
        {
            SenderWrapper wrapper => wrapper.IsConsole,
            IPlayer => false,
            ITorchBase => true,
            _ => false
        };
    }

    public void Attach()
    {
    }

    public void Detach()
    {
    }

    private sealed record SenderWrapper(object Obj, CommandContext Context)
    {
        public bool IsConsole => Obj is not IPlayer;
    }
}