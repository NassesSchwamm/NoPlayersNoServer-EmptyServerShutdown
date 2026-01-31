using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace NoPlayersNoServer
{
	public enum LogMode
	{
		Off,
		Console,
		Chat,
		All
	}

	public enum TimerStartMode
	{
		OnWorldLoad,
		OnLastPlayerDisconnect
	}

	public class NoPlayersNoServerConfig : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ServerSide;

		[Label("Shutdown Timer (Seconds)")]
		[Tooltip("The duration (in seconds) the server waits after the last player disconnects before shutting down.\nRange: 1 to 3600 seconds.")]
		[DefaultValue(30)]
		[Range(1, 3600)]
		public int ShutdownTimerSeconds;

		[Label("Logging Level")]
		[Tooltip("Controls where the shutdown notifications are displayed.\nValues:\n0: Off: No messages.\n1. Console: Messages only in the server console.\n2: Chat: Messages only in the in-game chat.\n3: All: Messages in both console and chat.")]
		[DefaultValue(LogMode.Console)]
		[DrawTicks]
		public LogMode LoggingLevel;

		[Label("Timer Start Mode")]
		[Tooltip("Determines when the empty server check begins.\nValues:\n0: OnWorldLoad: The timer starts as soon as the world loads, even if no player has ever connected.\n1: OnLastPlayerDisconnect: The timer only starts after at least one player has connected and then the server becomes empty.")]
		[DefaultValue(TimerStartMode.OnWorldLoad)]
		[DrawTicks]
		public TimerStartMode StartMode;
	}
}
