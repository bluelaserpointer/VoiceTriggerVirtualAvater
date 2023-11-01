using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IzumiTools
{
    /// <summary>
    /// Rigidbody based FPS/TPS human movement controller.<br/>
    /// - Accepts physical interaction.<br/>
    /// - Requires curved surface on bottom collider (like capsule) to climb up stairs. 
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class RBBasedHumanMovementController : MonoBehaviour
    {
        //inspector
        [Header("Movement")]
        [Min(0)]
        public float speed = 5f;
        public bool controllableInAir = true;
        [Min(0)]
        public float modelRotateLerpFactor = 20F;

        [Header("SelfReference")]
        [SerializeField]
        CollisionChecker _groundChecker;
        [SerializeField]
        Transform _modelYAxis;
        [SerializeField]
        protected Transform _cameraYAxis;
        //--

        //control
        public bool IsGrounded => _groundChecker.CollidingAny;
        public bool IsMoving { get; private set; }
        public Rigidbody Rigidbody { get; private set; }
        private Vector3 _inputs = Vector3.zero;
        public Quaternion ModelTargetRotation { get; private set; }

        void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            //_actionPose = ActionPoseType.Idle;
        }
        void Update()
        {
            IsMoving = false;
            if (IsGrounded || controllableInAir)
            {
                _inputs = Vector3.zero;
                _inputs.x = Input.GetAxis("Horizontal");
                _inputs.z = Input.GetAxis("Vertical");
                if (_inputs != Vector3.zero)
                {
                    IsMoving = true;
                    _inputs = Quaternion.Euler(0, _cameraYAxis.eulerAngles.y, 0) * _inputs;
                    ModelTargetRotation = Quaternion.Euler(0, _inputs.YAngle(), 0);
                }
            }
        }
        void FixedUpdate()
        {
            if (_inputs != Vector3.zero)
            {
                Rigidbody.MovePosition(Rigidbody.position + _inputs * speed * Time.fixedDeltaTime);
            }
            _modelYAxis.rotation = Quaternion.LerpUnclamped(_modelYAxis.rotation, ModelTargetRotation, modelRotateLerpFactor * Time.fixedDeltaTime);
        }
    }
}
