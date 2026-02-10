using UnityEngine;

namespace FlightDynamics.Vers2
{
    /// <summary>
    /// Surface for each wind section with auto-setup presets
    /// </summary>
    public class AerodynamicSurface : MonoBehaviour
    {
        #region Fields
        [Header("Profile Data")]
        public AerodynamicProfile profile;

        [Header("Physics parameters")]
        public float surfaceArea = 1.6f;

        [Header("Control parameters")]
        public float maxDeflectionDeg = 20f;

        [SerializeField] private Rigidbody _rb;
        private float _currentInput;

        private Vector3 liftGizmo;

        public SurfaceType type = SurfaceType.Wing;
        public float inputMultiplier = 1f;
        public float configAngle = 0f;

        #endregion

        #region System 

        private void Start()
        {
            if (!_rb) _rb = GetComponentInParent<Rigidbody>();
            if (!_rb) _rb = FindAnyObjectByType<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (!_rb) return;

            Vector3 worldVelocity = _rb.GetPointVelocity(transform.position);
            Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);

            if (localVelocity.magnitude < 0.1f) return;

            float angleOfAttack = Mathf.Atan2(-localVelocity.y, localVelocity.z) * Mathf.Rad2Deg;

            float effectiveAoA = angleOfAttack + configAngle;

            if (type != SurfaceType.Wing)
            {
                effectiveAoA += _currentInput * maxDeflectionDeg;
            }

            float Cl = profile.liftCurve.Evaluate(effectiveAoA);
            float Cd = profile.dragCurve.Evaluate(effectiveAoA);

            float dynamicPressure = 0.5f * 1.225f * localVelocity.sqrMagnitude;

            float liftMag = Cl * dynamicPressure * surfaceArea;
            float dragMag = Cd * dynamicPressure * surfaceArea;

            Vector3 liftDir = Vector3.Cross(worldVelocity, transform.right).normalized;
            Vector3 dragDir = -worldVelocity.normalized;

            liftGizmo = liftDir * liftMag;

            _rb.AddForceAtPosition(liftDir * liftMag, transform.position);
            _rb.AddForceAtPosition(dragDir * dragMag, transform.position);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, liftGizmo * 0.01f);
        }

        public void SetInput(float input)
        {
            _currentInput = input;
        }

        #endregion
    }
}