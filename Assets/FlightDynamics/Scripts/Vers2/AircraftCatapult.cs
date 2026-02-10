using UnityEngine;
using System.Collections;

namespace FlightDynamics.Vers2
{
    public class AircraftCatapult : MonoBehaviour
    {
        [Header("Settings")]
        public float launchForce = 400f;     
        public float launchDuration = 1.0f;  
        public KeyCode launchKey = KeyCode.Space;

        [Header("State")]
        public bool isLaunching = false;
        private Rigidbody _rb;
        private AircraftEngine _engine;

        void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _engine = GetComponentInChildren<AircraftEngine>();

            if (_rb) _rb.isKinematic = true;
        }

        void Update()
        {
            if (Input.GetKeyDown(launchKey) && !isLaunching)
            {
                StartCoroutine(LaunchRoutine());
            }
        }

        private IEnumerator LaunchRoutine()
        {
            isLaunching = true;
            _rb.isKinematic = false; 

            if (_engine) _engine.throttle = 1f;

            float timer = 0f;
            while (timer < launchDuration)
            {
                _rb.AddForce(transform.forward * launchForce, ForceMode.Acceleration);

                _rb.angularVelocity = Vector3.zero;

                timer += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            isLaunching = false;
            Debug.Log("<color=green>[Catapult]</color> Launch Complete!");
        }
    }
}