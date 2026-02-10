using UnityEngine;

namespace FlightDynamics.Vers2
{
    [RequireComponent(typeof(Rigidbody))]
    public class AircraftController : MonoBehaviour
    {
        [Header("Balance")]
        public Transform _cg; // Center of gravity

        [Header("References")]
        public AerodynamicSurface[] _surfaces;
        private Rigidbody _rb;

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.centerOfMass = transform.InverseTransformPoint(_cg.position); // Set center of mass to CG
            _surfaces = GetComponentsInChildren<AerodynamicSurface>();
        }

        private void Update()
        {
            float pitch = Input.GetAxis("Vertical");
            float roll = Input.GetAxis("Horizontal");
            float yaw = Input.GetAxis("Rudder");

            foreach (var surface in _surfaces)
            {
                if (surface.type == SurfaceType.Wing) continue;

                switch (surface.type)
                {
                    case SurfaceType.Elevator:
                        surface.SetInput(pitch);
                        break;
                    case SurfaceType.Aileron:
                        surface.SetInput(roll * surface.inputMultiplier);
                        break;
                    case SurfaceType.Rudder:
                        surface.SetInput(yaw);
                        break;
                    case SurfaceType.Elevon:
                        float elevonMix = pitch + (roll * surface.inputMultiplier);
                        surface.SetInput(elevonMix);
                        break;
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (_cg == null) return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_cg.position, 0.15f);
            Gizmos.DrawLine(_cg.position, _cg.position + Vector3.down * 10f);

        }
    }
}


