using UnityEngine;

namespace FlightDynamics.Vers2
{
    [RequireComponent(typeof(Rigidbody))]
    public class AircraftController : MonoBehaviour
    {
        [Header("Status")]
        public bool useAutopilot = false;

        [Header("Balance")]
        public Transform _cg;

        [Header("Manual/External Inputs")]
        [Range(-1f, 1f)] public float pitchInput;
        [Range(-1f, 1f)] public float rollInput;
        [Range(-1f, 1f)] public float yawInput;

        private AerodynamicSurface[] _surfaces;
        private Rigidbody _rb;

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            if (_cg != null)
                _rb.centerOfMass = transform.InverseTransformPoint(_cg.position);

            _surfaces = GetComponentsInChildren<AerodynamicSurface>();
        }

        private void Update()
        {
            if (!useAutopilot)
            {
                pitchInput = Input.GetAxis("Vertical");
                rollInput = Input.GetAxis("Horizontal");
                yawInput = Input.GetAxis("Rudder");
            }

            ApplyInputsToSurfaces();
        }

        private void ApplyInputsToSurfaces()
        {
            foreach (var surface in _surfaces)
            {
                if (surface.type == SurfaceType.Wing) continue;

                switch (surface.type)
                {
                    case SurfaceType.Elevator:
                        surface.SetInput(pitchInput);
                        break;
                    case SurfaceType.Aileron:
                        surface.SetInput(rollInput * surface.inputMultiplier);
                        break;
                    case SurfaceType.Rudder:
                        surface.SetInput(yawInput);
                        break;
                    case SurfaceType.Elevon:
                        float elevonMix = pitchInput + (rollInput * surface.inputMultiplier);
                        surface.SetInput(Mathf.Clamp(elevonMix, -1f, 1f));
                        break;
                }
            }
        }

        public void SetAutopilot(bool state)
        {
            useAutopilot = state;
            Debug.Log(state ? "<color=cyan>Autopilot Engaged</color>" : "<color=orange>Manual Control</color>");
        }

        private void OnDrawGizmos()
        {
            if (_cg == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_cg.position, 0.15f);
        }
    }
}