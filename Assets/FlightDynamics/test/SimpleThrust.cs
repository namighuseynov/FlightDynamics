using UnityEngine;

namespace test
{
    public class SimpleThrust : MonoBehaviour
    {
        public Rigidbody rb;
        public float thrustN = 8000f; 
        public bool constantThrust = true;

        void Reset()
        {
            rb = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            if (!rb) return;
            if (constantThrust)
                rb.AddForce(transform.forward * thrustN, ForceMode.Force);
        }
    }
}
