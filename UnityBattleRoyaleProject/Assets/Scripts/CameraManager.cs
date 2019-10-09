using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale
{
    public class CameraManager : MonoBehaviour
    {
        [SerializeField] Transform m_cameraTarget;
        [SerializeField] float m_cameraFollowDistance;
        [SerializeField] Vector3 m_cameraOffset;

        Transform cameraTransform;

        private void Awake()
        {
            cameraTransform = transform;

           // Cursor.lockState = CursorLockMode.Locked;
            
        }

        // Start is called before the first frame update
        void Start()
        {
           
           
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if(m_cameraTarget)
            {
                cameraTransform.position = m_cameraTarget.position +( m_cameraTarget.forward * -1 * m_cameraFollowDistance);

                cameraTransform.LookAt(m_cameraTarget.position + m_cameraOffset);
            }
        }

        public void AssignCameraTarget(Transform _target)
        {
            m_cameraTarget = _target;
        }
    }
}