using UnityEngine;

namespace FlightDynamics
{
    [CreateAssetMenu(menuName = "Flight/Airfoil Profile")]
    public class AirfoilProfile : ScriptableObject
    {
        public AnimationCurve liftCurve;
        public AnimationCurve dragCurve;

        private void OnEnable()
        {
            if (liftCurve == null || liftCurve.length == 0)
            {
                liftCurve = new AnimationCurve(
                    new Keyframe(-20f, -0.8f),
                    new Keyframe(-10f, -0.4f),
                    new Keyframe(0f, 0.0f),
                    new Keyframe(10f, 0.8f),
                    new Keyframe(15f, 1.1f),
                    new Keyframe(20f, 0.6f),
                    new Keyframe(30f, 0.2f)
                );
            }

            if (dragCurve == null || dragCurve.length == 0)
            {
                dragCurve = new AnimationCurve(
                    new Keyframe(-30f, 0.25f),
                    new Keyframe(-15f, 0.08f),
                    new Keyframe(0f, 0.02f),
                    new Keyframe(15f, 0.08f),
                    new Keyframe(30f, 0.25f)
                );
            }
        }
    }

}
