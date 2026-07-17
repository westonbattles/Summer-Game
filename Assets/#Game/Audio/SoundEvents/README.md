# How to make sounds using the sound system!

### Overview
1. In the project folder, Create -> Audio -> SoundEvent.
2. In the SoundEvent, add your clips to the list and configure your sound settings appropriately. Make sure to assign it a mixer group. Check notes for more information.
3. In the code where you want the sound to play, add
    - `private SoundEvent` (name it something sane like `sound`).
    - `private SoundHandle` (name it something sane like `handle`).
4. To play a sound, there's two functions.
    - `sound.Play(Vector3 position)` plays the sound at a fixed world-position.
    - `sound.PlayAttatched(Transform follow)` plays the sound following a transform. This can be used for stuff like hums or engine loops. Moving stuff.
        - Note that there is a `PlayInternal(...)` function. Do not touch it. its for composition purposes!!!

#### Thats it! Have fun :)

---
---
---
---
### For The Nerds
This has pooling built into the AudioPlayer script. Check that out if you think its cool.
If you want to add more sound tuning features, add it to the SoundEvent SO. Also, its an internal readonly so it really can be managed well in ASM.