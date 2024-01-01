# VRC Avatar Sticker Generator

Generates Telegram stickers from a VRChat (or ChilloutVR) avatar. It loops through each hand gesture and plays the animation on your avatar and outputs a 512x512 transparent sticker with a white outline.

<img src="screenshots/output.png" width="500px" />

Tested in Unity 2019.4.31f1 on Windows 10 with the Canis Woof by Rezillo Ryker.

## Usage

1. Import the `.unitypackage` into your Unity project
2. Duplicate your avatar scene
3. Add `PeanutTools/VRC_Avatar_Sticker_Generator/Prefabs/Sticker_Generator.prefab` to your scene and configure the settings:

- drag your VRChat avatar into the Animator slot
- set the armature to hide (if you want this)
- configure your animators and apply them

4. Enter play mode

Your stickers will be outputted to a new folder called "stickers" in the root of your project.

## FAQ

### How do I change the camera, lighting, etc.?

Everything is in the prefab. Change it as much as you like. I recommend setting the lighting of the scene to a gradient of white, black and white.

### How do I give it a cartoony look?

Use the supplied shader `Custom/StylisedShader` ([source](https://github.com/ardahamamcioglu/Unity-Stylised-Shader)) and play around with the settings. Set the "Hatch Pattern" to `Shaders/HalftoneDot.jpg`.

### How do I set the trigger amount?

Add "GestureLeftWeight" and "GestureRightWeight" as floats and set them to 1.

### How do I show accessories if my body is hidden?

Move them to the head bone.

### How do I hide specific parts of my avatar?

Add `PeanutTools/VRC_Avatar_Sticker_Generator/Prefabs/HideBehind.prefab` and position as you like.
