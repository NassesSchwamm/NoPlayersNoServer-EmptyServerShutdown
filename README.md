NoPlayersNoServer (EmptyServerShutdown) is a server-side utility mod for servers that automatically shuts down the server when no players are connected. This helps save resources and ensures your server doesn't run unnecessarily when empty.

Features:

- Configurable shutdown timer: Set how long the server should wait before shutting down.
- Configurable logging: Choose whether to see notifications in the server console, in-game chat, both, or neither.
- Timer start modes: Choose whether the shutdown timer starts immediately on world load or only after the last player disconnects.

If I decide to (or you ask nicely - through Steam or github) i may be willing to add more features and configuration options to this mod in the future. For now tough everything stays as is.

Configuration:
- Shutdown Timer (Seconds): Default is 30 seconds. Range: 1 to 3600 seconds.
- Logging Level: Off, Console (default), Chat, All.
- Timer Start Mode: OnWorldLoad (default), OnLastPlayerDisconnect.

This mod is intended for dedicated servers (Main.dedServ). It will (and should) not affect single-player or hosted multiplayer games where the host is a player.
