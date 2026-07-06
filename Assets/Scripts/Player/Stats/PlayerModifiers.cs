// rough example for now - these are base stats, don't change these in file. we'll use Player.Instance.Modifiers to change them

// bonus        -> addition modifiers
// multiplier   -> multiplicative modifiers
public class PlayerModifiers
{
    public float MaxHealthBonus = 0f;
    public float BhopMomentumCarryPercent = 0.33f; // how much horizontal velocity you keep on perfect bhops
    
    // EYE ABILITIES
    public int RevolverBulletsBonus = 0;
    public bool DashVerticallyEnabled = true;
    
    // HAND ABILITIES
    public int BrassKnucklesComboStagesBonus = 0;                   // how many extra combo stages do we get (each stage gives us an extra 25% * BrassKnucklesComboDamageMultiplier damage)
    public float BrassKnucklesComboIncreaseDamageMultiplier = 1f;   // scaling of damage increase per combo level. if base increase is 25% for example (1x -> 1.25x -> 1.5x), setting this value to two would make it 50% (1x -> 1.5x -> 2x)
}