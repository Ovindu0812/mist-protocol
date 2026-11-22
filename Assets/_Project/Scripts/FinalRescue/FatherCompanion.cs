using MistProtocol.Core;
using MistProtocol.Player;
using UnityEngine;
using UnityEngine.AI;

namespace MistProtocol.FinalRescue
{
    /// <summary>
    /// Makes Father (Appa) intelligently follow the player towards the escape extraction point.
    /// Resilient hybrid navigation: leverages NavMeshAgent when available,
    /// with smooth direct kinematic pathing fallback to guarantee Father never gets stuck.
    /// </summary>
    public class FatherCompanion : MonoBehaviour
    {
        [SerializeField] private float followDistance = 2.8f;
        [SerializeField] private float runSpeed = 5.2f;
        [SerializeField] private float turnSpeed = 10f;

        private Transform _playerTransform;
        private NavMeshAgent _navAgent;
        private CharacterController _characterController;
        private bool _isFollowing;
        private Vector3 _lastPos;

        public bool IsFollowing => _isFollowing;

        public void StartFollowing()
        {
            _isFollowing = true;
            ResolvePlayer();

            // Try using NavMeshAgent if on baked NavMesh
            _navAgent = GetComponent<NavMeshAgent>();
            if (_navAgent == null)
            {
                // Test if ground has NavMesh before adding
                if (NavMesh.SamplePosition(transform.position, out var hit, 2.5f, NavMesh.AllAreas))
                {
                    transform.position = hit.position;
                    _navAgent = gameObject.AddComponent<NavMeshAgent>();
                    _navAgent.speed = runSpeed;
                    _navAgent.stoppingDistance = followDistance;
                    _navAgent.acceleration = 12f;
                    _navAgent.angularSpeed = 360f;
                    _navAgent.radius = 0.4f;
                    _navAgent.height = 1.85f;
                }
            }

            _characterController = GetComponent<CharacterController>();
            _lastPos = transform.position;
        }

        private void ResolvePlayer()
        {
            if (_playerTransform != null) return;
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) _playerTransform = pc.transform;
            else if (PlayerLocator.State != null && PlayerLocator.State.Root != null)
                _playerTransform = PlayerLocator.State.Root;
        }

        private void Update()
        {
            if (!_isFollowing) return;

            if (_playerTransform == null)
            {
                ResolvePlayer();
                if (_playerTransform == null) return;
            }

            Vector3 targetPos = _playerTransform.position;
            float dist = Vector3.Distance(transform.position, targetPos);

            // 1. Primary: NavMeshAgent
            if (_navAgent != null && _navAgent.isOnNavMesh)
            {
                _navAgent.isStopped = dist <= followDistance;
                if (!_navAgent.isStopped)
                {
                    _navAgent.SetDestination(targetPos);
                }
            }
            // 2. Resilient Kinematic Fallback
            else
            {
                if (dist > followDistance)
                {
                    Vector3 dir = (targetPos - transform.position);
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.01f)
                    {
                        dir.Normalize();
                        // Face travel direction
                        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);

                        Vector3 move = dir * (runSpeed * Time.deltaTime);

                        // Raycast to snap height to floor
                        if (Physics.Raycast(transform.position + Vector3.up * 1f, Vector3.down, out var groundHit, 3f))
                        {
                            move.y = (groundHit.point.y - transform.position.y);
                        }

                        if (_characterController != null && _characterController.enabled)
                        {
                            _characterController.Move(move);
                        }
                        else
                        {
                            transform.position += move;
                        }
                    }
                }
            }

            // Running bob animation
            float moved = Vector3.Distance(_lastPos, transform.position);
            _lastPos = transform.position;
            if (moved > 0.005f)
            {
                float bob = Mathf.Sin(Time.time * 12f) * 0.02f;
                var child = transform.Find("Appa_Mesh");
                if (child != null)
                {
                    child.localPosition = new Vector3(0f, bob, 0f);
                }
            }
        }
    }
}
