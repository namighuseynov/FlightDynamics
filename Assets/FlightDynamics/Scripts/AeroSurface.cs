using UnityEngine;

namespace FlightDynamics
{
    public class AeroSurface : MonoBehaviour
    {
        [Header("Links")]
        public Rigidbody rb;
        public AirfoilProfile profile;

        [Header("Geometry")]
        public float chord = 0.3f;
        public float span = 0.5f;
        public Vector3 chordAxisLocal = Vector3.forward;
        public Vector3 spanAxisLocal = Vector3.right;
        public Vector3 aerodynamicCenterOffsetLocal = Vector3.zero;

        [Header("Control (optional)")]
        public bool isControlSurface = false;
        [Range(-1f, 1f)] public float controlInput = 0f;
        public float maxDeflectionDeg = 20f;

        [Header("Tuning")]
        public float liftMultiplier = 1f;
        public float dragMultiplier = 1f;
        public float minAirspeed = 0.5f;

        public float Area => Mathf.Max(0.0001f, chord * span);

        [Header("Gizmos")]
        public float forceGizmoScale = 0.01f;
        public float maxArrowLength = 5f;
        public bool drawAxes = true;

        private Vector3 _lastWorldPos;
        private Vector3 _lastForce;
        private bool _hasLastForce;

        void Reset()
        {
            if (!rb) rb = GetComponentInParent<Rigidbody>();
        }

        void FixedUpdate()
        {
            if (!rb || profile == null) { _hasLastForce = false; return; }

            Vector3 worldPos = transform.TransformPoint(aerodynamicCenterOffsetLocal);

            // 1) Скорость точки в мировом пространстве
            Vector3 vPoint = rb.GetPointVelocity(worldPos);

            // 2) Учет ветра (если AeroEnvironment реализован)
            float rho = 1.225f; // Значение по умолчанию (плотность воздуха)
            Vector3 wind = Vector3.zero;

            if (AeroEnvironment.Instance != null)
            {
                var air = AeroEnvironment.Instance.Sample(worldPos, Time.time);
                rho = air.density;
                wind = air.wind;
            }

            // 3) Относительная скорость воздуха
            Vector3 vAir = vPoint - wind;
            float speed = vAir.magnitude;
            if (speed < minAirspeed) { _hasLastForce = false; return; }

            // 4) Определение осей крыла в мировом пространстве
            Vector3 chordW = transform.TransformDirection(chordAxisLocal).normalized;
            Vector3 spanW = transform.TransformDirection(spanAxisLocal).normalized;
            Vector3 normalW = Vector3.Cross(spanW, chordW).normalized;

            // Направление набегающего потока (откуда дует ветер относительно крыла)
            Vector3 relWind = -vAir.normalized;
            // Проекция потока на плоскость профиля (перпендикулярно размаху крыла)
            Vector3 relWindInPlane = Vector3.ProjectOnPlane(relWind, spanW).normalized;

            // 5) Расчет Угла Атаки (AoA)
            float aoaDeg = Vector3.SignedAngle(chordW, relWindInPlane, spanW);

            if (isControlSurface)
                aoaDeg += controlInput * maxDeflectionDeg;

            // 6) Получение коэффициентов из профиля крыла
            float CL = profile.liftCurve.Evaluate(aoaDeg);
            float CD = profile.dragCurve.Evaluate(aoaDeg);

            // 7) Силы (Скоростной напор q = 0.5 * rho * v^2)
            float q = 0.5f * rho * speed * speed;
            float liftMag = q * Area * CL * liftMultiplier;
            float dragMag = q * Area * CD * dragMultiplier;

            // 8) Направления сил
            Vector3 dragDir = -vAir.normalized; // Сопротивление всегда против движения

            // Подъемная сила перпендикулярна потоку воздуха
            Vector3 liftDir = Vector3.Cross(dragDir, spanW).normalized;

            // КОРРЕКЦИЯ: проверяем, чтобы Lift смотрел в сторону «верха» крыла
            if (Vector3.Dot(liftDir, normalW) < 0) liftDir = -liftDir;

            // 9) Приложение итоговой силы
            Vector3 force = (liftDir * liftMag) + (dragDir * dragMag);
            rb.AddForceAtPosition(force, worldPos, ForceMode.Force);

            _lastWorldPos = worldPos;
            _lastForce = force;
            _hasLastForce = true;
        }

        // --- Визуализация (Gizmos) ---
        void OnDrawGizmosSelected()
        {
            Vector3 center = transform.TransformPoint(aerodynamicCenterOffsetLocal);
            Vector3 chordW = transform.TransformDirection(chordAxisLocal).normalized;
            Vector3 spanW = transform.TransformDirection(spanAxisLocal).normalized;

            // Отрисовка "виртуальной" плоскости крыла
            Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
            Vector3 p1 = center + (chordW * chord * 0.5f) + (spanW * span * 0.5f);
            Vector3 p2 = center + (chordW * chord * 0.5f) - (spanW * span * 0.5f);
            Vector3 p3 = center - (chordW * chord * 0.5f) - (spanW * span * 0.5f);
            Vector3 p4 = center - (chordW * chord * 0.5f) + (spanW * span * 0.5f);
            Gizmos.DrawLine(p1, p2); Gizmos.DrawLine(p2, p3); Gizmos.DrawLine(p3, p4); Gizmos.DrawLine(p4, p1);

            if (Application.isPlaying && _hasLastForce)
            {
                Vector3 arrow = _lastForce * forceGizmoScale;
                if (arrow.magnitude > maxArrowLength) arrow = arrow.normalized * maxArrowLength;
                DrawArrow(_lastWorldPos, arrow, Color.red);
            }
        }

        private void DrawArrow(Vector3 start, Vector3 vec, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawRay(start, vec);
        }
    }
}