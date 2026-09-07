using System;
using UnityEngine;

[Serializable]
public class CelestialStarboard : ItemBehaviour
{

    float DashVerticalIncreaseFunction(float x) => Mathf.Log(x+1)/2;
    
    public override void ApplyModifiers(PlayerModifiers modifiers, int itemCount)
    {
        modifiers.DashVerticalMultiplier.AddValue(DashVerticalIncreaseFunction(itemCount));
    }
}