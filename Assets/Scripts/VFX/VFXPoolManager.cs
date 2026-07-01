using System;
using UnityEngine;

public class VFXPoolManager : MonoBehaviour
{
    public static VFXPoolManager Instance { get; private set; }

    [Header("Budget")]
    [SerializeField, Min(1)] 
    
    
    void Awake()
    {
        throw new NotImplementedException();
    }
}