# Metin2-REBORN Architecture

## Runtime

Client
  -> Login/Channel
  -> Game Server
  -> Database Server
  -> Persistent database

## Client modules

- Rendering
- Actor/character system
- Network stream
- UI
- Input/camera
- Effects
- Audio
- Data/pack loading

## Server modules

- Connection/session
- Character
- Combat
- Item
- Mob
- Skill
- Quest
- Party
- Guild
- Shop/trade
- Dungeon
- World/map

## Data

Game definitions should be separated from executable logic wherever practical:

- characters
- classes
- items
- mobs
- bosses
- skills
- drops
- maps
- quests

## First playable milestone

The first milestone is deliberately small:

1. Start database
2. Start server
3. Start client
4. Create/login one character
5. Enter one map
6. Move
7. Spawn one original test mob
8. Attack the mob
9. Receive EXP
10. Level up

Only after this loop works will larger systems be added.
