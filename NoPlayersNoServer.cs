using System;
using System.Threading;
using System.Timers;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace NoPlayersNoServer
{
	public class NoPlayersNoServer : Mod
	{
	}

	public class NoPlayersNoServerSystem : ModSystem
	{
		private System.Timers.Timer checkTimer;
		private long shutdownDeadlineUtcTicks;
		private int lastAnnouncedSeconds = -1;
		private volatile bool isShutdownScheduled = false;
		private volatile bool hasPlayerConnectedOnce = false;
		private volatile bool isShuttingDown = false;
		private volatile int cachedActivePlayers = 0;
		private volatile int cachedShutdownTimerSeconds = 30;
		private volatile TimerStartMode cachedStartMode = TimerStartMode.OnWorldLoad;
		private volatile LogMode cachedLogMode = LogMode.Console;

		public override void OnWorldLoad()
		{
			if (Main.dedServ)
			{
				hasPlayerConnectedOnce = false;
				isShutdownScheduled = false;
				isShuttingDown = false;
				shutdownDeadlineUtcTicks = 0;
				lastAnnouncedSeconds = -1;
				UpdateConfigCache();
				StartTimer();
			}
		}

		public override void OnWorldUnload()
		{
			StopTimer();
			shutdownDeadlineUtcTicks = 0;
			isShutdownScheduled = false;
			isShuttingDown = false;
			lastAnnouncedSeconds = -1;
		}

		public override void PostUpdateEverything()
		{
			if (!Main.dedServ) return;
			if (isShuttingDown || Netplay.Disconnect) return;

			UpdateConfigCache();
			UpdateActivePlayerCount();

			if (cachedActivePlayers > 0)
			{
				hasPlayerConnectedOnce = true;
				if (isShutdownScheduled)
				{
					CancelShutdown("Player connected. Shutdown cancelled.");
				}
			}
		}

		private void StartTimer()
		{
			if (checkTimer == null)
			{
				checkTimer = new System.Timers.Timer(1000);
				checkTimer.Elapsed += OnCheckTimerElapsed;
				checkTimer.AutoReset = true;
				checkTimer.Start();
				Log("Player activity monitor started.", allowChat: false);
			}
		}

		private void StopTimer()
		{
			if (checkTimer != null)
			{
				checkTimer.Stop();
				checkTimer.Dispose();
				checkTimer = null;
				Log("Player activity monitor stopped.", allowChat: false);
			}
		}

		private void UpdateConfigCache()
		{
			var config = ModContent.GetInstance<NoPlayersNoServerConfig>();
			if (config == null) return;

			cachedShutdownTimerSeconds = config.ShutdownTimerSeconds;
			cachedStartMode = config.StartMode;
			cachedLogMode = config.LoggingLevel;
		}

		private void UpdateActivePlayerCount()
		{
			int activePlayers = 0;
			for (int i = 0; i < Main.maxPlayers; i++)
			{
				if (Main.player[i] != null && Main.player[i].active)
				{
					activePlayers++;
				}
			}

			cachedActivePlayers = activePlayers;
		}

		private void CancelShutdown(string reason)
		{
			isShutdownScheduled = false;
			Interlocked.Exchange(ref shutdownDeadlineUtcTicks, 0);
			Interlocked.Exchange(ref lastAnnouncedSeconds, -1);
			Log(reason);
		}

		private void Log(string message, bool allowChat = true)
		{
			var currentLevel = cachedLogMode;
			if (currentLevel == LogMode.Off) return;

			string fullMessage = $"[NoPlayersNoServer] {message}";

			if (currentLevel == LogMode.Console || currentLevel == LogMode.All)
			{
				Console.WriteLine(fullMessage);
			}

			if (allowChat && (currentLevel == LogMode.Chat || currentLevel == LogMode.All))
			{
				if (Main.netMode == Terraria.ID.NetmodeID.Server)
				{
					try
					{
						Terraria.Chat.ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(fullMessage), Color.Red);
					}
					catch (Exception)
					{
						// Ignore chat errors
					}
				}
			}
		}

		private void OnCheckTimerElapsed(object sender, ElapsedEventArgs e)
		{
			if (!Main.dedServ) return;
			if (isShuttingDown || Netplay.Disconnect) return;

			if (cachedActivePlayers > 0)
			{
				if (isShutdownScheduled)
				{
					CancelShutdown("Player connected. Shutdown cancelled.");
				}

				return;
			}

			if (cachedStartMode == TimerStartMode.OnLastPlayerDisconnect && !hasPlayerConnectedOnce)
			{
				return;
			}

			if (!isShutdownScheduled)
			{
				isShutdownScheduled = true;
				var deadlineTicks = DateTime.UtcNow.AddSeconds(cachedShutdownTimerSeconds).Ticks;
				Interlocked.Exchange(ref shutdownDeadlineUtcTicks, deadlineTicks);
				Interlocked.Exchange(ref lastAnnouncedSeconds, -1);
				var shutdownAtLocal = DateTime.Now.AddSeconds(cachedShutdownTimerSeconds);
				string shutdownAtText = shutdownAtLocal.ToString("yyyy:MM:dd:HH:mm:ss");
				Log($"Server empty. Shutdown scheduled in {cachedShutdownTimerSeconds} seconds. Scheduled at {shutdownAtText}.", allowChat: false);
				return;
			}

			long currentDeadlineTicks = Interlocked.Read(ref shutdownDeadlineUtcTicks);
			if (currentDeadlineTicks == 0)
			{
				currentDeadlineTicks = DateTime.UtcNow.AddSeconds(cachedShutdownTimerSeconds).Ticks;
				Interlocked.Exchange(ref shutdownDeadlineUtcTicks, currentDeadlineTicks);
			}

			var remaining = new DateTime(currentDeadlineTicks, DateTimeKind.Utc) - DateTime.UtcNow;
			int remainingSeconds = (int)Math.Ceiling(remaining.TotalSeconds);

			if (remainingSeconds <= 0)
			{
				Log("Timer expired. Shutting down...", allowChat: false);
				isShuttingDown = true;
				Netplay.Disconnect = true;
				StopTimer();
				isShutdownScheduled = false;
				Interlocked.Exchange(ref shutdownDeadlineUtcTicks, 0);
				Interlocked.Exchange(ref lastAnnouncedSeconds, -1);
				return;
			}

			int lastAnnounced = Interlocked.CompareExchange(ref lastAnnouncedSeconds, lastAnnouncedSeconds, lastAnnouncedSeconds);
			if ((remainingSeconds % 10 == 0 || remainingSeconds <= 5) && remainingSeconds != lastAnnounced)
			{
				Interlocked.Exchange(ref lastAnnouncedSeconds, remainingSeconds);
				Log($"Shutdown in {remainingSeconds} seconds...", allowChat: false);
			}
		}
	}
}
