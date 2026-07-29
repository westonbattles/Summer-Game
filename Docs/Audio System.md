# How-to Audio

It's super duper simple!

First thing you'll want to do is to create a SoundEvent.

To-do so: In your project window, right-click and click through: `Create->Audio->SoundEvent`

You will then see this config menu for the sound! Do all of your tweaking here.

![alt text](<Audio System Inspector Viewport.png>)

I recommend reading the next section to understand how the code works.

Here's a code snippet to show you guys how it has been used before.

```c#
// adds the sound on collecting an xp orb.
public void CollectOrb(float amount, int soundCount = 1)
{
    if (sound != null)
    {
        sound.Play(transform.position, soundCount);
    }

    AddXP(amount);
}
```
You can play sounds using `SoundEvents` and `SoundHandles`.

Here's the (currently) available `Play` functions and what they do:
```c#
// Plays a sound at runtime at a position.
public SoundHandle Play(Vector3 position);
```

```c#
// Plays a sound at runtime, but you can configure the # of sounds at burst.
public SoundHandle Play(Vector3 position, int burstOverride);
```

```c#
// Plays a sound that follows a target game object.
public SoundHandle PlayAttached(Transform follow);
```

All of these functions should create a SoundHandle which is what pools the sounds. My goal was to make adding a sound in your code as friction-less as possible.

The `.Play()` function builds all of your sound settings from the SoundEvent you made and it intelligently picks the right internal Play functions to get your sound right!

Unity's base audio system makes it difficult to make audio changes, and has you writing too much boilerplate slop.