using System;
using UnityEngine;

[Serializable]
public class CelestialStarboard : ItemBehaviour
{

    //float DashVerticalIncreaseFunction(float x) => Mathf.Log(x+1)/2;
    float DashVerticalIncreaseFunction(float x) => (0.6f*x) / (x+2f);
    
    public override void ApplyModifiers(PlayerModifiers modifiers, int itemCount)
    {
        modifiers.DashVerticalMultiplier.AddValue(DashVerticalIncreaseFunction(itemCount));
    }
}