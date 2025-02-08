using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace Toastingway;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
public sealed class Service
{
    [PluginService]
    internal static IDalamudPluginInterface PluginInterface { get; set; }

    [PluginService]
    internal static ICommandManager CommandManager { get; set; }

    [PluginService]
    public static IToastGui ToastGui { get; set; }

    [PluginService]
    internal static IGameInventory GameInventory { get; set; }

    [PluginService]
    internal static IPluginLog PluginLog { get; set; }

    [PluginService]
    internal static IDataManager DataManager { get; set; }

    [PluginService]
    public static IClientState ClientState { get; set; }

    [PluginService]
    public static IFramework Framework { get; set; }
}
