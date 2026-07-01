# Path Puzzle

Path Puzzle is an Augmented Reality (AR) puzzle game for iOS where players arrange path tiles to guide a ball from the start tile to the finish tile. The game combines real-world surface detection with grid-based puzzle mechanics, making the player's physical environment part of the gameplay arena.

## Overview

In Path Puzzle, players scan a flat surface using the device camera, place the puzzle arena, then drag and rotate path tiles before starting the simulation. Once the simulation begins, the ball moves automatically based on the arranged path. Players must plan carefully so the ball reaches the finish tile without leaving the grid.

The game is designed as a level-based puzzle experience with increasing difficulty, reward progression, unlockable ball skins, and visual trail effects.

## Gameplay

The main gameplay flow consists of:

1. Select a level from the level menu.
2. Scan a flat surface using the iOS camera.
3. Place the arena on the detected surface.
4. Arrange path tiles using drag and drop.
5. Rotate tiles to connect the correct route.
6. Start the simulation and guide the ball to the finish.
7. Earn stars and coins based on level performance.

## Main Features

- Augmented Reality surface detection
- Grid-based path puzzle gameplay
- Drag and rotate tile interaction
- Level selection system
- Locked and unlocked level progression
- Tutorial system for level 0
- Timer and move counter
- Star rating system
- Coin reward system
- Ball skin shop
- Buy, equip, and equipped skin states
- Basic and special ball skin categories
- Trail effect system for ball skins
- Current skin display in menu, shop, and gameplay
- Pause menu with resume, restart, exit, and audio controls
- Audio settings for master volume, music, and SFX
- Level complete and level failed panels
- Credit scene after completing the final level
- Developer cheat tools for testing progress, coins, skins, and auto-solve paths

## Scenes

Important scenes in this project:

- `Assets/Scenes/MainMenu.unity` - Main menu, level selection, settings, shop, and about menu.
- `Assets/Scenes/ARSceneDS.unity` - Main AR gameplay scene.
- `Assets/CreditScene.unity` - Credit scene shown after the final level.
- `Assets/SkinTrailPreview.unity` - Preview scene for checking ball skins and trail effects.

## Built With

- Unity 2022.3.62f3
- C#
- TextMeshPro
- Unity UI / UGUI
- ARKit XR Plugin
- Unity Physics
- Unity Audio System

## Platform

This project is developed for:

- iOS
- Minimum recommended device: iPhone 11
- Minimum recommended iOS version: iOS 11
- Camera and AR support are required

## Project Structure

Important folders in this repository:

- `Assets/` - Main project assets, scripts, scenes, prefabs, UI, materials, audio, and gameplay resources.
- `Assets/deepseek/` - Main custom C# gameplay scripts and system logic.
- `Assets/Scenes/` - Main menu and AR gameplay scenes.
- `Assets/Audio/` - Music and sound effect resources.
- `Assets/Images/` - UI images, icons, and visual assets.
- `Assets/Prefab/` - Game prefabs used in gameplay and UI.
- `Assets/Rolling_Balls-Sci-fi_Pack/` - Ball skin asset package.
- `Assets/TextMesh Pro/` - TextMeshPro resources.
- `Assets/XR/` - XR and AR related resources.
- `Packages/` - Unity package dependencies.
- `ProjectSettings/` - Unity project configuration.

## How To Open This Project

1. Install Unity Hub.
2. Install Unity version `2022.3.62f3`.
3. Open Unity Hub.
4. Add this project folder.
5. Open the project through Unity Hub.

## How To Run In Unity Editor

1. Open the project in Unity.
2. Open the `MainMenu` scene from `Assets/Scenes/`.
3. Press Play in the Unity Editor.

Note: AR gameplay features require an iOS device build because camera-based AR surface detection cannot be fully tested inside the Unity Editor.

## How To Build For iOS

1. Open the project in Unity.
2. Go to `File > Build Settings`.
3. Select `iOS` as the target platform.
4. Add the required scenes to Build Settings.
5. Click `Build` to generate the Xcode project.
6. Open the generated project in Xcode.
7. Connect an iPhone device.
8. Build and run the project from Xcode.

## Notes

This is a Unity project. Folders such as `Library/`, `Temp/`, `Logs/`, and `UserSettings/` are generated locally by Unity and are not required in the GitHub repository.

Build outputs should be placed outside the source project folder before publishing or uploading the repository.

## Repository Purpose

This repository is used to store the source code, gameplay systems, scenes, and assets for Path Puzzle. It helps manage development progress, track revisions, and support future improvements to the game.
