using UnityEngine;

public class CG : MonoBehaviour
{
    private Rigidbody _rb;
    private Vector3 _gravity;
    [SerializeField] private Transform _cg;
    private void Awake()
    {
        if (_rb == null)
        {
            _rb = GetComponent<Rigidbody>();
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _rb.centerOfMass = transform.InverseTransformPoint(_cg.position);
        }
    }

    private void OnDrawGizmos()
    {
        if (_cg == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_cg.position, 0.5f);
        Gizmos.DrawLine(_cg.position, _cg.position + Vector3.down * 10f);

    }
}
