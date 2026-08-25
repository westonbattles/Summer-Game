// Naming convention:
//   ...Bonus     
//   ...Multiplier
//   ...Percent (Same as multiplier but usually only between 0-1)
//   ...Enabled   
//
// REMEMBER: Bonuses don't have to be changed additively. And multipliers/percents don't have to be changed multiplicatively
// it all just depends on the modifier itself and how we want it to scale
using System.Collections.Generic;


public class PlayerModifiers
{
    
    // Base Player Modifiers
    public Modifier<float, Multiplicative> DamageMultiplier = new(1f);
    public Modifier<float, Additive> MaxHealthBonus = new(0f);
    public Modifier<float, Multiplicative> DamageTakenPercent = new(1f);        // 1.0 = 100% damage taken, 0.5 = 50% damage takrn, etc
    public Modifier<float> AttackSpeedMultiplier = new(1f);                     // 1.0 = 100% speed, 2.0 = 200% speed, etc
    public Modifier<float> MovementSpeedMultiplier = new(1f);                   // 1.0 = 100% speed, 2.0 = 200% speed, etc
    public Modifier<float> BhopMomentumCarryPercent = new(0.33f);               // how much horizontal velocity you keep on perfect bhops 
    public Modifier<float> CooldownDurationMultiplier = new(1f);                // (0.0 < x < 2.0). You can have LONGER cooldowns
    
    
    /*-----------------------------  EYE ABILITIES  ------------------------------- */
    // Revolver
    public Modifier<int, Additive> RevolverBulletsBonus = new(0);                             // how many extra bullets in the chamber the revolver gets
    public Modifier<float, Multiplicative> RevolverLastBulletDamageMultiplier = new(1f);      // damage multiplier for the last bullet in the revolvers chamber
    // Dash
    public Modifier<bool> DashVerticallyEnabled = new(false);
    
    
    
    
    
    /*-----------------------------  HAND ABILITIES  ------------------------------ */
    // Brass Knuckles
    public Modifier<int, Additive> BrassKnucklesComboStagesBonus = new(0);                       // how many extra combo stages do we get (each stage gives us an extra 25% * BrassKnucklesComboDamageMultiplier damage)
    public Modifier<float, Multiplicative> BrassKnucklesBaseDamageMultiplier = new(1f);          // simply just the base damage mutliplier for brass knuckles. this gets compounded multiplicatively with BrassKnucklesComboIncreaseDamageMultiplier
    public Modifier<float, Multiplicative> BrassKnucklesComboIncreaseDamageMultiplier = new(1f); // scaling of damage increase per combo level. if base increase is 25% for example (1x -> 1.25x -> 1.5x), setting this value to two would make it 50% (1x -> 1.5x -> 2x)
    // Whirlwind
    public Modifier<float, Additive> WhirlwindRadiusBonus = new(0f);                             // additive radius for the base whirlwind damage dcircle
    // Ground Pound
    public Modifier<float, Additive> GroundPoundRadiusBonus = new(0f);                           // additive radius for the base ground pound damage circle
    public Modifier<float, Multiplicative> GroundPoundDamageScalingMultiplier = new(1f);         // multiplier for the base height-damage scaling function. value of 2 scales twice as fast
    
    
    /* ---------------------------------  END  -------------------------------------*/
    
    
    
    // Collection of all Modifiers!!
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
    
    public interface IModifier
    {
        // contract for reset value so we can store the modifiers in our list using IModifier, and call ResetValue() on them
        public void ResetValue();
    }
    
    // modifier class (how we can define them)
    public class Modifier<T> : IModifier
    {
        readonly T _baseValue;
        T _currentValue;

        public event System.Action<T> OnValueChanged;

        public T BaseValue => _baseValue;

        public Modifier(T value)
        {
            _baseValue = value;
            _currentValue = value;
            Modifiers.Add(this);
        }

        public T GetValue() => _currentValue;
        
        /// sets the current modifier value (we can still access and use BaseValue if we want, that doesn't get modified. only currentvalue does)
        public void SetValue(T value)
        {
            // https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.equalitycomparer-1?view=net-10.0
            // Jake - Compares generic types safely without causing compile errors or allocating garbage memory. Prevents weird situations too.
            if (!EqualityComparer<T>.Default.Equals(_currentValue, value))
            {
                _currentValue = value;
                OnValueChanged?.Invoke(_currentValue);
            }
        }

        /// reset the modifier value to it's base value, gets called on inventory recalculations before re-applying all modifiers again
        public void ResetValue() { _currentValue = _baseValue; }
    }
    
    // https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/generics/constraints-on-type-parameters
    // just enforces TModificationType to be a subclass of IModificationType (defined below)
    public class Modifier<T, TModificationType> : Modifier<T> where TModificationType : struct, IModificationType 
    {
        public Modifier(T value) : base(value) { } // just calls base modifier constructor
    }
}

public interface IModificationType { }
public readonly struct Additive : IModificationType { };
public readonly struct Multiplicative : IModificationType { };

public static class ModifierExtensions
{
    public static void AddValue(this PlayerModifiers.Modifier<int, Additive> modifier, int value) { modifier.SetValue(modifier.GetValue() + value); }
    public static void AddValue(this PlayerModifiers.Modifier<float, Additive> modifier, float value) { modifier.SetValue(modifier.GetValue() + value); }
    public static void MultiplyValue(this PlayerModifiers.Modifier<float, Multiplicative> modifier, float factor) { modifier.SetValue(modifier.GetValue() * factor); }
}