using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale
{
    public class UserControlUI : MonoBehaviour
    {
        [SerializeField] ThirdPersonCharacter m_Character;

        float hDelta = 0.0f;
        float vDelta = 0.0f;

        private Transform m_Cam;                  // A reference to the main camera in the scenes transform
        private Vector3 m_CamForward;             // The current forward direction of the camera
        private Vector3 m_Move;
        private bool m_Jump;                      // the world-relative desired move direction, calculated from the camForward and user input.
        private bool m_Crouch;
        // Start is called before the first frame update
        void Start()
        {
            // get the transform of the main camera
            if (Camera.main != null)
            {
                m_Cam = Camera.main.transform;
            }
            else
            {
                Debug.LogWarning(
                    "Warning: no main camera found. Third person character needs a Camera tagged \"MainCamera\", for camera-relative controls.", gameObject);
                // we use self-relative controls in this case, which probably isn't what the user wants, but hey, we warned them!
            }

            // get the third person character ( this should never be null due to require component )
            //m_Character = GetComponent<ThirdPersonCharacter>();

            m_Move = Vector3.zero;

        }

        // Update is called once per frame
        void Update()
        {
            if(Input.GetKey(KeyCode.W))
            {
                vDelta = 1.0f;
            }
            else if (Input.GetKey(KeyCode.S))
            {
                vDelta = -1.0f;
            }

            if (Input.GetKey(KeyCode.D))
            {
                hDelta = 1.0f;
            }
            else if (Input.GetKey(KeyCode.A))
            {
                hDelta = -1.0f;
            }

            m_Crouch = Input.GetKey(KeyCode.C);
            m_Jump = Input.GetKey(KeyCode.Space);
        }

        private void FixedUpdate()
        {
            // read inputs
            //float h = CrossPlatformInputManager.GetAxis("Horizontal");
            //float v = CrossPlatformInputManager.GetAxis("Vertical");
            //bool crouch = Input.GetKey(KeyCode.C);

            // calculate move direction to pass to character
            if (m_Cam != null)
            {
                // calculate camera relative direction to move:
                m_CamForward = Vector3.Scale(m_Cam.forward, new Vector3(1, 0, 1)).normalized;
                m_Move = vDelta * m_CamForward + hDelta * m_Cam.right;
            }
            else
            {
                // we use world-relative directions in the case of no main camera
                m_Move = vDelta * Vector3.forward + hDelta * Vector3.right;
            }
#if !MOBILE_INPUT
			// walk speed multiplier
	        if (Input.GetKey(KeyCode.LeftShift)) m_Move *= 0.5f;
#endif
            //Debug.Log(hDelta + " " + vDelta);
            //Debug.Log(m_Move);

            // pass all parameters to the character control script
            m_Character.Move(m_Move, m_Crouch, m_Jump);
            m_Jump = false;

            hDelta = 0.0f;
            vDelta = 0.0f;
        }

        public void OnPadHorizontalPressed(float _value)
        {
            hDelta = _value;
        }

        public void OnPadVerticalPressed(float _value)
        {
            vDelta = _value;
        }

        public void OnJumpPressed()
        {
            m_Jump = true;
        }

        public void OnCrouchPressed()
        {
            m_Crouch = true;
        }
    }
}
