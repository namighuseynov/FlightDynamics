using UnityEngine;

namespace FlightDynamics.Vers2
{
    /// <summary>
    /// Surface for each wind section
    /// </summary>

    public class AerodynamicSurface : MonoBehaviour
    {
        #region Fields
        [Header("Physics parameters")]
        public float surfaceArea = 1.6f;
        public AnimationCurve liftCurve = new AnimationCurve(
            new Keyframe(-15f, -0.6f),
            new Keyframe(-10f, -0.4f),
            new Keyframe(-5f, -0.2f),
            new Keyframe(0f, 0.2f),
            new Keyframe(5f, 0.7f),
            new Keyframe(10f, 1.1f),
            new Keyframe(12f, 1.3f),
            new Keyframe(15f, 1.0f),
            new Keyframe(20f, 0.5f)
        );
        public AnimationCurve dragCurve = new AnimationCurve(
            new Keyframe(-15f, 0.12f),
            new Keyframe(-10f, 0.08f),
            new Keyframe(-5f, 0.04f),
            new Keyframe(0f, 0.025f),
            new Keyframe(5f, 0.035f),
            new Keyframe(10f, 0.06f),
            new Keyframe(15f, 0.12f),
            new Keyframe(20f, 0.25f)
        );

        [Header("Control parameters")]
        public float maxDeflectionDeg = 20f;

        [SerializeField] private Rigidbody _rb;
        private float _currentInput;

        private Vector3 liftGizmo;

        public SurfaceType type = SurfaceType.Wing;
        public float inputMultiplier = 1f;

        #endregion

        #region System 

        private void Start()
        {
            if (!_rb) _rb = GetComponent<Rigidbody>(); 
            if (!_rb) _rb = FindAnyObjectByType<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (!_rb) return;

            Vector3 worldVelocity = _rb.GetPointVelocity(transform.position);
            Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);

            if (localVelocity.magnitude < 0.1f) return; // Avoid calculations at very low speeds

            float angleOfAttack = Mathf.Atan2(-localVelocity.y, localVelocity.z) * Mathf.Rad2Deg;

            float effectiveAoA = angleOfAttack;

            if (type != SurfaceType.Wing)
            {
                effectiveAoA += _currentInput * maxDeflectionDeg;
            }

            float Cl = liftCurve.Evaluate(effectiveAoA);
            float Cd = dragCurve.Evaluate(effectiveAoA);

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
