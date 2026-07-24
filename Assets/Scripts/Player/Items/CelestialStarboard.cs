using System;

[Serializable]
public class CelestialStarboard : ItemBehaviour
{
    public override void ApplyModifiers(PlayerModifiers modifiers, int itemCount)
    {
        modifiers.DashVerticallyEnabled.SetValue(true);
        
        // todo: count>1 -> increase dash speed
    }
}