using UnityEngine;

namespace FlightDynamics.Vers2
{
    [CreateAssetMenu(fileName = "NewAerodynamicProfile", menuName = "Flight Dynamics/Aerodynamic Profile")]

    public class AerodynamicProfile : ScriptableObject
    {
        [Header("Lift Settings")]
        public AnimationCurve liftCurve;

        [Header("Drag Settings")]
        public AnimationCurve dragCurve;

    }
}