using System;
using System.Collections.Generic;
using UnityEngine;

public class BossDoorLightsController : MonoBehaviour
{

    BossDoorLight[] lights;


    void Start()
    {
        Player.Instance.Inventory.OnItemCountChanged += UpdateLights;
    }

    void OnDestroy()
    {
        Player.Instance.Inventory.OnItemCountChanged -= UpdateLights;
    }

    void Awake()
    {
        lights = GetComponentsInChildren<BossDoorLight>();
        ResetLights();
    }

    void ResetLights()
    {
        foreach (BossDoorLight l in lights) l.TurnOff();
    }

    void UpdateLights(ItemDefinition item, int count)
    {
        int itemCount = Player.Instance.Inventory.TotalItemCount;
        for (int i = 0; i < lights.Length; i++)
        {
            if (i < itemCount)
            {
                if (!lights[i].IsOn) lights[i].TurnOn();
            }
            else lights[i].TurnOff();
        }
    }
    
}
