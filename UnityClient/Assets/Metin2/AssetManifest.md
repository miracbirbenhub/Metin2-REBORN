# Metin2 Asset Manifest

The first playable build will use the following exact-content categories.

| Category | Required content | Unity result |
|---|---|---|
| Warrior | body + armor + weapon + animations | Player prefab |
| Monsters | model + skeleton + animations | Monster prefab |
| Metin Stones | model + hit/death visuals | Metin prefab |
| Map 1 | terrain + objects + trees + textures + collision | Playable scene |
| Items | weapon/armor/item icons + world drops | Inventory + drops |
| VFX | hit, destroy, pickup, upgrade effects | Combat feedback |

## Import order

1. Maps and terrain
2. Warrior model/skeleton/animations
3. Metin Stone
4. One monster
5. Weapons/armor
6. Items and drops
7. VFX
8. Mobile UI

## Fidelity rule

Do not redesign the original character, monster, Metin Stone or map when an authorized original asset is available. The Unity layer should reproduce the source asset's geometry, texture, scale, animation and placement as closely as technically possible.

## Current blocker

The repository does not contain original Metin2 proprietary asset files. They must be supplied by the developer from a lawful source before the 1:1 visual layer can be populated.

For GR2-based content, conversion/import is required because Unity does not natively consume Metin2's Granny GR2 format. Community Metin2-to-Unity projects use GR2 -> FBX conversion and preserve original filenames/relationships for map imports.
