# Instant Mode for Slay the Spire 2

A high-performance game acceleration mod for **Slay the Spire 2**, designed to eliminate downtime and provide a near-instantaneous gameplay experience.

## Features

- **10x Game Speed**: Globally accelerates animations and game logic via `Engine.TimeScale`.
- **Zero-Latency Actions**: Forces `Instant` mode and patches `Cmd.Wait` to eliminate logical delays.
- **Persistent Speed**: Uses a background manager to ensure the speed multiplier stays active across scene changes.
- **In-Game Toggle**: Toggle the mod on/off instantly with **F8**, featuring an on-screen notification.

## Controls

- **F8**: Toggle Instant Mode On/Off.

## Installation

1. Ensure you have a mod loader installed for Slay the Spire 2.
2. Download the latest `InstantMode.dll` and `InstantMode.pck`.
3. Place both files in your game's mod directory:
   `...\SteamLibrary\steamapps\common\Slay the Spire 2\mods\InstantMode\`
4. Launch the game and enjoy the speed!

## How it Works (Technical)

This mod performs surgical patches using **Harmony**:

1. **Forced Instant Mode**: Patches the game's internal preference getter to always return `FastModeType.Instant`.
2. **Global TimeScale**: Maintains a consistent 10.0x multiplier via a background engine node.
3. **Command Overrides**: Divides all remaining wait times by the speed multiplier to ensure consistent performance.
4. **VFX Notifications**: Uses the game's internal `NFullscreenTextVfx` for toggle feedback.

---
*Created by dukelukem53*
