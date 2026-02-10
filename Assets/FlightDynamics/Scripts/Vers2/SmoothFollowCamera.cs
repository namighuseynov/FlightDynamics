using UnityEngine;

namespace FlightDynamics.Vers2
{
    public class SmoothFollowCamera : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;       
        public Transform aircraft;      

        [Header("Settings")]
        public float positionSmoothness = 0.125f; 
        public float rotationSmoothness = 5f;    
        public Vector3 offset;                 

        void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + target.TransformDirection(offset);
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, positionSmoothness);
            transform.position = smoothedPosition;

            Quaternion desiredRotation = target.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmoothness * Time.deltaTime);
        }
    }
}


