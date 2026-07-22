// rough example for now - these are base stats, don't change these in file. we'll use Player.Instance.Modifiers to change them

// bonus        -> addition modifiers
// multiplier   -> multiplicative modifiers

using System.Collections.Generic;

public class PlayerModifiers
{

    static readonly List<IModifier> Modifiers = new();
    
    // we initialize with this instead of a constructor so we can ensure the modifiers get reset and have no duplicates
    public static PlayerModifiers CreateFresh()
    {
        Modifiers.Clear();
        return new PlayerModifiers();
    }
    
    public void ResetAll()
    {
        foreach (IModifier modifier in Modifiers) modifier.ResetValue();
    }

    // Base Player Stats
    public Modifier<float> DamageMultiplier = new(1f);
    public Modifier<float> MaxHealthBonus = new(0f);
    public Modifier<float> BhopMomentumCarryPercent = new(0.33f); // how much horizontal velocity you keep on perfect bhops
    
    
    
    
    // Eye Abilities
    
    // Revolver
    public Modifier<int> RevolverBulletsBonus = new(0);                             // how many extra bullets in the chamber the revolver gets
    public Modifier<float> RevolverLastBulletDamageMultiplier = new(1f);            // damage multiplier for the last bullet in the revolvers chamber
    // Dash
    public Modifier<bool> DashVerticallyEnabled = new(false);
    
    
    
    
    
    // Hand Abilities
    
    // Brass Knuckles
    public Modifier<int> BrassKnucklesComboStagesBonus = new(0);                    // how many extra combo stages do we get (each stage gives us an extra 25% * BrassKnucklesComboDamageMultiplier damage)
    public Modifier<float> BrassKnucklesComboIncreaseDamageMultiplier = new(1f);    // scaling of damage increase per combo level. if base increase is 25% for example (1x -> 1.25x -> 1.5x), setting this value to two would make it 50% (1x -> 1.5x -> 2x)
    // Whirlwind
    public Modifier<float> WhirlwindRadiusBonus = new(0f);                          // additive radius for the base whirlwind damage dcircle
    // Ground Pound
    public Modifier<float> GroundPoundRadiusBonus = new(0f);                        // additive radius for the base ground pound damage circle
    public Modifier<float> GroundPoundDamageScalingMultiplier = new(1f);            // multiplier for the base height-damage scaling function. value of 2 scales twice as fast

    
    
    
    
    public interface IModifier
    {
        public void ResetValue(); // contract for reset value so we can store the modifiers in our list using IModifier, and call ResetValue() on them
    }
    
    // modifier class (how we can define them)
    public class Modifier<T> : IModifier
    {
        readonly T _baseValue;
        T _currentValue;
        
        public T BaseValue => _baseValue;

        public Modifier(T value)
        {
            _baseValue = value;
            _currentValue = value;
            Modifiers.Add(this);
        }
        
        public T GetValue() => _currentValue;
        /// sets the current modifier value (we can still access and use BaseValue if we want, that doesn't get modified. only currentvalue does)
        public void SetValue(T value) { _currentValue = value; }
        public void ResetValue() { _currentValue = _baseValue; }
    }
}

public static class ModifierExtensions
{
    public static void AddValue(this PlayerModifiers.Modifier<int> modifier, int value) { modifier.SetValue(modifier.GetValue() + value); }
    public static void AddValue(this PlayerModifiers.Modifier<float> modifier, float value) { modifier.SetValue(modifier.GetValue() + value); }
}