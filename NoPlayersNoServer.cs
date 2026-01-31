using System;
using System.Timers;
using Terraria;
using Terraria.IO;
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
		private Timer checkTimer;
		private int shutdownCountdown;
		private bool isShutdownScheduled = false;
		private bool hasPlayerConnectedOnce = false;
		private bool isServerFullyInitialized = false;

		private readonly object _lock = new object();

		public override void OnWorldLoad() {
			if (Main.dedServ) {
				lock (_lock) {
					hasPlayerConnectedOnce = false;
					isShutdownScheduled = false;
					isServerFullyInitialized = false;
				}
			}
		}

		public override void PostUpdateWorld() {
			if (Main.dedServ && !isServerFullyInitialized) {
				lock (_lock) {
					if (!isServerFullyInitialized) {
						isServerFullyInitialized = true;
						StartTimer();
					}
				}
			}
		}

		public override void OnWorldUnload() {
			StopTimer();
			lock (_lock) {
				isServerFullyInitialized = false;
			}
		}

		private void StartTimer() {
			lock (_lock) {
				if (checkTimer == null) {
					checkTimer = new Timer(1000);
					checkTimer.Elapsed += OnCheckTimerElapsed;
					checkTimer.AutoReset = true;
					checkTimer.Start();
					Log("Server Initialized. Monitoring player count...", LogMode.Console);
				}
			}
		}

		private void StopTimer() {
			lock (_lock) {
				if (checkTimer != null) {
					checkTimer.Stop();
					checkTimer.Dispose();
					checkTimer = null;
					Log("Timer stopped.", LogMode.Console);
				}
			}
		}

		private void Log(string message, LogMode minLevel = LogMode.Console) {
			var config = ModContent.GetInstance<NoPlayersNoServerConfig>();
			if (config == null) return;

			var currentLevel = config.LoggingLevel;

			if (currentLevel == LogMode.Off) return;

			string fullMessage = $"[NoPlayersNoServer] {message}";

			if (currentLevel == LogMode.Console || currentLevel == LogMode.All) {
				Console.WriteLine(fullMessage);
			}

			if (currentLevel == LogMode.Chat || currentLevel == LogMode.All) {
				if (Main.netMode == Terraria.ID.NetmodeID.Server) {
					try {
						Terraria.Chat.ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(fullMessage), Color.Red);
					} catch (Exception) {
					}
				}
			}
		}

		private void OnCheckTimerElapsed(object sender, ElapsedEventArgs e) {
			if (!Main.dedServ) return;

			int activePlayers = 0;
			try {
				for (int i = 0; i < Main.maxPlayers; i++) {
					if (Main.player[i] != null && Main.player[i].active) {
						activePlayers++;
					}
				}
			} catch (Exception) {
				activePlayers = 1;
			}

			lock (_lock) {
				if (activePlayers > 0) {
					hasPlayerConnectedOnce = true;
				}

				var config = ModContent.GetInstance<NoPlayersNoServerConfig>();
				if (config == null) return;

				int configuredTime = config.ShutdownTimerSeconds;
				TimerStartMode startMode = config.StartMode;

				if (activePlayers == 0) {
					if (startMode == TimerStartMode.OnLastPlayerDisconnect && !hasPlayerConnectedOnce) {
						return;
					}

					if (!isShutdownScheduled) {
						isShutdownScheduled = true;
						shutdownCountdown = configuredTime;

						DateTime scheduledTime = DateTime.Now.AddSeconds(shutdownCountdown);
						string dateString = scheduledTime.ToString("yyyy:MM:dd:HH:mm:ss");

						Log($"Server empty. Shutdown scheduled in {shutdownCountdown} seconds. (Target: {dateString})");
					} else {
						shutdownCountdown--;

						if (shutdownCountdown % 10 == 0 || shutdownCountdown <= 5) {
							Log($"Shutdown in {shutdownCountdown} seconds...");
						}

						if (shutdownCountdown <= 0) {
							Log("Timer expired. Saving and shutting down...");

							if (checkTimer != null) {
								checkTimer.Stop();
								checkTimer.Dispose();
								checkTimer = null;
							}

							try {
								WorldFile.SaveWorld();
								Netplay.Disconnect = true;
							} catch (Exception ex) {
								Log($"Error during shutdown: {ex.Message}");
							}
						}
					}
				} else {
					if (isShutdownScheduled) {
						Log("Player connected. Shutdown cancelled.");
						isShutdownScheduled = false;
						shutdownCountdown = configuredTime;
					}
				}
			}
		}
	}
}
