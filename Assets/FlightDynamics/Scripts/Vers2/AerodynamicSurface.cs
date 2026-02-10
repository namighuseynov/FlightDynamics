using UnityEngine;

namespace FlightDynamics.Vers2
{
    /// <summary>
    /// Surface for each wind section with auto-setup presets
    /// </summary>
    public class AerodynamicSurface : MonoBehaviour
    {
        #region Fields
        [Header("Physics parameters")]
        public float surfaceArea = 1.6f;
        public AnimationCurve liftCurve = new AnimationCurve();
        public AnimationCurve dragCurve = new AnimationCurve();

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

        #region Editor Context Menu

#if UNITY_EDITOR
        [ContextMenu("Setup As Fuselage")]
        public void SetupAsFuselage()
        {
            type = SurfaceType.Wing;
            surfaceArea = 8.0f;

            liftCurve = new AnimationCurve(
                new Keyframe(-90f, 0f),
                new Keyframe(-30f, -0.3f),
                new Keyframe(0f, 0f),
                new Keyframe(30f, 0.3f),
                new Keyframe(90f, 0f)
            );

            dragCurve = new AnimationCurve(
                new Keyframe(-90f, 1.2f),
                new Keyframe(-45f, 0.6f),
                new Keyframe(0f, 0.08f),
                new Keyframe(45f, 0.6f),
                new Keyframe(90f, 1.2f)
            );
        }

        [ContextMenu("Setup As Default Wing")]
        public void SetupAsDefaultWing()
        {
            type = SurfaceType.Wing;
            surfaceArea = 1.6f;

            liftCurve = new AnimationCurve(
                new Keyframe(-90f, 0f),
                new Keyframe(-15f, -0.6f),
                new Keyframe(0f, 0.2f),
                new Keyframe(15f, 1.2f),
                new Keyframe(20f, 0.5f), // Stall
                new Keyframe(90f, 0f)
            );

            dragCurve = new AnimationCurve(
                new Keyframe(-90f, 1.0f),
                new Keyframe(-15f, 0.15f),
                new Keyframe(0f, 0.025f),
                new Keyframe(15f, 0.15f),
                new Keyframe(90f, 1.0f)
            );
        }
#endif
        #endregion
    }
}