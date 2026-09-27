# Unity Mobile Plan

## Decision

Metin2-REBORN will use a Unity-based mobile client instead of the previous PC DirectX client approach.

The public Metin2-to-Unity tooling ecosystem provides separate conversion/import projects for GR2/FBX, maps, meshes, materials, trees and DDS/PNG workflows. We treat those projects as technical references/tools and keep third-party licensing and asset rights separate from this repository.

## Milestones

### M0 - Toolchain
- Install Unity
- Create Unity 6 project
- Verify Android build support
- Verify a blank APK builds and launches

### M1 - Warrior vertical slice
- Authorized Warrior model
- Authorized Warrior animations
- One map
- Camera
- Movement
- Touch joystick
- One Metin Stone
- Attack/damage
- Metin destruction
- One test drop

### M2 - Core progression
- Character stats
- EXP/levels
- Inventory
- Equipment
- +0 to +9 upgrade loop
- Yang
- Drop tables

### M3 - PvE progression
- Mobs
- Metin variants
- Bosses
- Boss rewards
- Map unlocks
- Respawn rules

### M4 - Online
- Authentication
- Character persistence
- Authoritative combat
- Inventory persistence
- Reconnect handling

### M5 - Mobile production
- Android optimization
- Device profiling
- UI scaling
- Battery/thermal checks
- iOS build
- Crash/error telemetry

## Asset boundary

This repository does not contain third-party proprietary Metin2 assets. Any asset imported into the working project must be legally available to the project owner for the intended use and redistribution.

## First success condition

When M1 is complete, launching the Android build should show the Warrior in the first map and allow the player to move, target a Metin Stone, attack it, destroy it and receive a test drop.
