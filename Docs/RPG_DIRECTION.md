# Metin2-Reborn — Mobile RPG Direction

The project is now a mobile-first RPG prototype inspired by the progression and presentation style of lightweight idle/action RPGs.

## Core loop

Character -> Zone -> Auto Battle -> Skills -> Loot -> Equipment -> Level -> Next Zone.

## First vertical slice

- Warrior as the first playable class
- Small village-themed combat zone
- Three enemy targets
- Automatic combat
- Two active skills
- XP and level progression
- Yang and item drops
- Equipment screen
- Mobile-first HUD

## Architecture

Unity owns presentation and client gameplay flow. Game data should remain data-driven. Online/server authority is postponed until the offline vertical slice is fun and stable.

## Asset policy

Metin2-derived proprietary assets must not be redistributed unless the project has the necessary rights. The prototype can use legally available/licensed assets or locally owned assets. The code should not depend on a specific proprietary model existing in the repository.

## Development order

1. Combat sandbox
2. Skills
3. Loot and equipment
4. Progression
5. Zone selection
6. Mobile UI polish
7. Android build
8. Replace temporary art with properly licensed/original content
