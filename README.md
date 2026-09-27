# Metin2-REBORN

A standalone MMORPG project inspired by classic 3D MMORPG systems.

## Foundation

The initial technical reference is the GPL-3.0 licensed Anka2 project. The reference repository contains a Windows C++ client, C++ game/DB server, Lua quest infrastructure and supporting tools.

## Project goals

- Real client/server MMORPG architecture
- Character creation and progression
- Four initial classes: Warrior, Ninja, Sura, Shaman
- Movement and combat
- Monsters and bosses
- EXP, levels and character stats
- Equipment and inventory
- Skills and effects
- Quests and dungeons
- Party and guild systems
- Mounts and pets
- Shops and trading
- Original game content and assets

## Repository layout

- `Client/` - game client integration
- `Server/` - game and database server
- `Database/` - schema, seed data and migrations
- `GameData/` - original game definitions and configuration
- `Scripts/` - quests and server scripts
- `Tools/` - build/data utilities
- `Docs/` - architecture and development notes
- `Assets/` - original game assets; proprietary third-party assets are not included

## Development order

1. Bootstrap source tree
2. Build client
3. Build server
4. Connect client <-> server <-> database
5. Character creation/login
6. First playable map
7. Movement/combat
8. EXP/level/stats
9. Items/equipment
10. Skills/effects
11. Monsters/bosses
12. Quests/dungeons
13. Party/guild/trading
14. Original content and polish

## Licensing

Code imported from third-party projects remains subject to its original license. This repository will preserve required license notices. Game assets must be independently licensed or created for Metin2-REBORN.
