using UnityEngine;

namespace test
{
    public class SimpleThrust : MonoBehaviour
    {
        public Rigidbody rb;
        public float maxThrust = 5000f; 
        public Transform thrustPoint;

        void Reset()
        {
            rb = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {

            Vector3 thrust = maxThrust * 1 * transform.forward; // Always full throttle forward
            rb.AddForceAtPosition(thrust, thrustPoint.position);
        }
    }
}
