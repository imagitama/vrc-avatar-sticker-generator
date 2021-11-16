# VRC Avatar Sticker Generator

Generates Telegram stickers from a VRChat avatar.

How it works:

1. Adds a simple white outline to all renderers in the scene
2. Shrinks everything in the armature except for the head
3. Takes all of the animator controllers from the `VRC Avatar Descriptor`, flattens them, adds them to avatar 
4. For each gesture combination it sets the animator parameter
5. Exports as a PNG

## Usage

1. Drop this folder into your Assets in your Unity project.
2. Go to PeanutTools -> VRC Avatar Sticker Generator.
3. Fill out the form.
4. Click "Create Stickers". Unity will go into Play Mode and will output to a Stickers folder (root of project).
5. Exit play mode.