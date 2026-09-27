# Metin2-REBORN

Mobile-first MMORPG project targeting Android/iOS with a Metin2-compatible visual/gameplay direction.

## Current foundation

The project has switched from the previous Anka2/PC-client direction to a **Unity mobile client** architecture.

The technical reference for the asset-import side is the open-source **Metin2-to-Unity** tooling ecosystem, including GR2→FBX, map, mesh, material, tree and DDS conversion tools. The published project specifically targets bringing the Metin2 experience to mobile platforms with Unity and reports successful original-map import experiments. We will use only assets we are legally entitled to use; proprietary Metin2 assets are not stored in this repository.

## Target game loop

1. Create/select character
2. Receive starter equipment
3. Enter the first map
4. Fight mobs and break Metin Stones
5. Collect Yang and item drops
6. Upgrade equipment
7. Increase level and stats
8. Enter boss content
9. Obtain stronger equipment/materials
10. Unlock stronger maps and repeat the progression loop

## Mobile-first goals

- Android first, iOS after the Android vertical slice
- Touch joystick and action controls
- Target selection and mobile-friendly combat
- Camera and UI designed for phones
- 3D character, monsters, Metin Stones, maps and effects
- Inventory, equipment and upgrade systems
- Online client/server architecture
- Persistent character progression
- Mobile performance profiling from the beginning

## Repository layout

- `UnityClient/` - Unity mobile client foundation
- `Server/` - future authoritative game server
- `Database/` - schema, migrations and seed data
- `GameData/` - items, mobs, maps, skills, drops and progression definitions
- `Scripts/` - gameplay/data scripts
- `Tools/` - import and development utilities
- `Assets/` - project-owned/licensed assets only
- `Docs/` - architecture and development notes
- `Client/` - legacy placeholder from the earlier PC-client experiment

## First playable milestone

The first milestone is intentionally small:

**Warrior -> first map -> camera -> movement -> one Metin Stone -> attack -> destruction -> drop.**

After that works reliably, we add inventory, equipment, upgrade, EXP/level and boss progression.

## Development rules

- Keep the mobile client separate from server authority.
- Keep gameplay data data-driven where practical.
- Profile on a real Android device early.
- Do not commit proprietary game files, credentials or server keys.
- Preserve the original license of every third-party code/tool component we integrate.
- Do not put third-party Metin2 assets into this repository unless we have the rights to redistribute them.
