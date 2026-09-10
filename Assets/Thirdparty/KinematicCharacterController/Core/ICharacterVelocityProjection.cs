using UnityEngine;

namespace KinematicCharacterController
{
    /// <summary>Optional velocity policy, invoked inside collision solving before the remaining sweep is updated.</summary>
    public interface ICharacterVelocityProjection
    {
        /// <returns>True when handled; false to use the motor's default projection.</returns>
        bool ProjectVelocity(ref Vector3 velocity, Vector3 obstructionNormal, bool stableOnHit);
    }
}
