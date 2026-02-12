using System.Collections.Generic;
using UnityEngine;

namespace FlightDynamics.Vers2
{
    public class AircraftAutopilot : MonoBehaviour
    {
        [Header("Mission Control")]
        public List<Transform> waypoints = new List<Transform>();
        public int currentWaypointIndex = 0;
        public float waypointArrivalDistance = 50f;
        public bool loopMission = true;

        [Header("Flight Limits")]
        public float maxBankAngle = 45f;
        public float maxPitchAngle = 20f;

        [Header("PID Controllers")]
        public PIDController rollPID;
        public PIDController pitchPID;

        private AircraftController _controller;
        private Rigidbody _rb;

        private void Start()
        {
            _controller = GetComponent<AircraftController>();
            _rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (!_controller || !_controller.useAutopilot) return;

            if (waypoints.Count == 0 || currentWaypointIndex >= waypoints.Count)
            {
                StabilizeLevelFlight();
                return;
            }

            NavigateToWaypoint();
        }

        private void NavigateToWaypoint()
        {
            Vector3 targetPos = waypoints[currentWaypointIndex].position;
            float distanceToTarget = Vector3.Distance(transform.position, targetPos);

            if (distanceToTarget < waypointArrivalDistance)
            {
                currentWaypointIndex++;
                if (currentWaypointIndex >= waypoints.Count)
                {
                    if (loopMission) currentWaypointIndex = 0;
                    else return;
                }
            }

            Vector3 localTarget = transform.InverseTransformPoint(targetPos);

            float targetRoll = Mathf.Clamp(localTarget.x * 0.1f, -1f, 1f) * maxBankAngle;
            float currentRoll = CalculateCurrentRoll();
            float rollError = targetRoll - currentRoll;
            _controller.rollInput = rollPID.Update(rollError, Time.fixedDeltaTime);

            float targetPitch = Mathf.Clamp(localTarget.y * 0.1f, -1f, 1f) * maxPitchAngle;
            float currentPitch = CalculateCurrentPitch();
            float pitchError = targetPitch - currentPitch;
            _controller.pitchInput = -pitchPID.Update(pitchError, Time.fixedDeltaTime);

            _controller.yawInput = 0;
        }

        private void StabilizeLevelFlight()
        {
            _controller.rollInput = rollPID.Update(0 - CalculateCurrentRoll(), Time.fixedDeltaTime);
            _controller.pitchInput = pitchPID.Update(0 - CalculateCurrentPitch(), Time.fixedDeltaTime);
        }

        private float CalculateCurrentRoll()
        {
            return transform.localEulerAngles.z > 180 ? transform.localEulerAngles.z - 360 : transform.localEulerAngles.z;
        }

        private float CalculateCurrentPitch()
        {
            float angle = transform.localEulerAngles.x;
            float pitch = angle > 180 ? angle - 360 : angle;
            return -pitch;
        }

        private void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Count == 0) return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] == null) continue;

                Gizmos.DrawSphere(waypoints[i].position, 5f);
                if (i < waypoints.Count - 1 && waypoints[i + 1] != null)
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }

            if (Application.isPlaying && currentWaypointIndex < waypoints.Count)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, waypoints[currentWaypointIndex].position);
            }
        }
    }
}

