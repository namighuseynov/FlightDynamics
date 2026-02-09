using UnityEngine;

namespace FlightDynamics.Vers2
{
    /// <summary>
    /// Realistic aircraft engine model that calculates thrust based on throttle input, airspeed, and altitude.
    /// </summary>

    public class AircraftEngine : MonoBehaviour
    {
        public float maxThrust = 5000f; // Maximum thrust at sea level
        [Range(0f, 1f)] public float throttle = 0f; // Throttle input (0 to 1)

        private float thrustMag;

        private Rigidbody _rb;

        private void Start()
        {
            if (!_rb) _rb = GetComponentInParent<Rigidbody>();
        }

        void FixedUpdate()
        {
            if (!_rb)
            {
                Debug.LogWarning("Rigidbody is null!");
                return;
            }

            thrustMag = throttle * maxThrust;

            _rb.AddForceAtPosition(thrustMag * transform.forward, transform.position);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + (transform.forward * thrustMag*0.001f));
        }
    }
}
