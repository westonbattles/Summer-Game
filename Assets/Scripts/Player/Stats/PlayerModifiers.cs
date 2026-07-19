// rough example for now - these are base stats, don't change these in file. we'll use Player.Instance.Modifiers to change them

// bonus        -> addition modifiers
// multiplier   -> multiplicative modifiers
public class PlayerModifiers
{

    public class Modifier<T>
    {
        readonly T _baseValue;
        T _currentValue;
        
        public T BaseValue => _baseValue;


        public Modifier(T value)
        {
            _baseValue = value;
            _currentValue = value;
        }
        
        public T GetCurrentValue() => _currentValue;
        /// sets the current modifier value (we can still access and use BaseValue if we want, that doesn't get modified. only CurrentValue does)
        public void SetValue(T value) { _currentValue = value; }
        public void ResetValue() { _currentValue = _baseValue; }
    }

    
    // Base Player Stats
    public Modifier<float> DamageMultiplier = new(1f);
    public float MaxHealthBonus = 0f;
    public float BhopMomentumCarryPercent = 0.33f; // how much horizontal velocity you keep on perfect bhops
    
    
    
    
    // Eye Abilities
    
    // Revolver
    public int RevolverBulletsBonus = 0;
    public Modifier<bool> DashVerticallyEnabled = new(false);
    
    
    
    
    
    // Hand Abilities
    
    // Brass Knuckles
    public int BrassKnucklesComboStagesBonus = 0;                   // how many extra combo stages do we get (each stage gives us an extra 25% * BrassKnucklesComboDamageMultiplier damage)
    public float BrassKnucklesComboIncreaseDamageMultiplier = 1f;   // scaling of damage increase per combo level. if base increase is 25% for example (1x -> 1.25x -> 1.5x), setting this value to two would make it 50% (1x -> 1.5x -> 2x)
    // Whirlwind
    public float WhirlwindRadiusBonus = 0f;                         // additive radius for the base whirlwind damage dcircle
    // Ground Pound
    public float GroundPoundRadiusBonus = 0f;                       // additive radius for the base ground pound damage circle
    public float GroundPoundDamageScalingMultiplier = 1f;           // multiplier for the base height-damage scaling function. value of 2 scales twice as fast

}