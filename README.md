# COM3D2.VoiceChanger.Plugin

Allows the game to change the voice of the voice. Use WebSocket to interact with the voice changer server.

## Install

Download from [release](https://github.com/silver1145/COM3D2.VoiceChanger.Plugin/releases)
Unzip to `game root directory/BepinEx/plugins/`

## Use

1. Start the supported voice changer server.
2. Launch the game.
3. Set the server URL to take effect. It is recommended to use [ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) (F1), otherwise you need to modify COM3D2.`VoiceChanger.Plugin.cfg`

* Pressing the GUI key (default V) will display the status window on the left side of the screen. It will display the connection status, and provide temporary disable and clear cache functions.
* If you change the voice changer settings and try to play the same ADV, you need to clear the cache.

## Introduction

The plugin uses a preloader to predict voice, currently only LinearPreloader, and a predictor based on a graph database may be implemented in the future.

* Preloader
  * [x] LinearPreloader
  * [ ] GraphDBPreloader
  * [ ] KagPreloader
