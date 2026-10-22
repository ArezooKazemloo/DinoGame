# Dino Runner - Unity 6.6

## Controls

- **Space:** jump
- **Hold Shift:** speed up the game
- **Escape:** pause or resume
- **Sound:** toggle with the speaker icon in the lower-right corner during a run; the setting is saved

## Project setup

Open `Assets/Scenes/SampleScene.unity` in Unity 6.6. The project uses the Input System package and its input actions are created with `UnityEngine.InputSystem.InputAction`.

After Unity imports the assets, run **Dino Game > Setup Animator (Run, Jump, Hit, Death)** once. The editor utility creates and assigns the `Animator Controller` with Run, Jump, Hit, Death, and Dash states. It uses the smooth cartoon frames in `Assets/Art/FreeDinoPack/png` when available and keeps the original pixel-art frames as a fallback. The cartoon pack is by pzUH and is released under CC0: https://opengameart.org/content/free-dino-sprites.

The game creates its Main Menu and Pause Menu at runtime. `GameManager` owns score, saved high score, run state, and menus; `ObstacleSpawner` creates rounded ground and airborne hazards and Sunstone, Life Blossom, and Aether Shield pickups. All hazards and pickups derive from `Interactable`. `AudioManager` creates lightweight cartoon-style sound effects for hits, scoring, healing, shields, and game over; the in-game speaker icon saves the player's on/off setting. `ParallaxBackground` builds three smooth, storybook-style looping layers with different scroll speeds.

## Build

In Unity, open **File > Build Profiles**, select the intended desktop target, and build the project. Keep `Library`, `Temp`, and `Logs` out of the submitted archive; `.gitignore` excludes generated Unity and IDE files.
