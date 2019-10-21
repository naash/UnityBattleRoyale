using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale
{
    public class UserControlUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] RectTransform handleImage;
        [SerializeField] float distanceLimit = 35.0f;

        [SerializeField] ThirdPersonCharacter m_Character;

        float hDelta = 0.0f;
        float vDelta = 0.0f;

        private Transform m_Cam;                  // A reference to the main camera in the scenes transform
        private Vector3 m_CamForward;             // The current forward direction of the camera
        private Vector3 m_Move;
        private bool m_Jump;                      // the world-relative desired move direction, calculated from the camForward and user input.
        private bool m_Crouch;
        private bool m_DragStarted = false;

        private Vector3 m_initialHandlePosition;
        private float m_currentHandleSnapDuration = 0.0f;
        private float m_handleSnapDuration = 0.25f;
        private Vector3 m_HandleFromPos;


        Vector2 m_mouseCurrentPosition;
        Vector2 m_mousePreviousPosition;

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
            m_mouseCurrentPosition = Vector2.zero;
            m_mousePreviousPosition = m_mouseCurrentPosition;
            if (handleImage)
            {
                m_initialHandlePosition = handleImage.position;
            }


            m_currentHandleSnapDuration = 2.0f;
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
            else
            {
                vDelta = 0.0f;
            }

            if (Input.GetKey(KeyCode.D))
            {
                hDelta = 1.0f;

                //hDelta = Mathf.Min(1.0f, hDelta + 0.01f);
            }
            else if (Input.GetKey(KeyCode.A))
            {
                hDelta = -1.0f;

               // hDelta = Mathf.Max(-1.0f, hDelta - 0.01f);
            }
            else
            {
                hDelta = 0.0f;
            }

            m_mousePreviousPosition = m_mouseCurrentPosition;
            m_mouseCurrentPosition = Input.mousePosition;

            m_Crouch = Input.GetKey(KeyCode.C);
            m_Jump = Input.GetKey(KeyCode.Space);


            if(m_DragStarted)
            {
                //Get touch position
                Vector3 pos = Input.mousePosition;

                handleImage.position = pos;

                if (Vector3.Distance(pos, m_initialHandlePosition) > distanceLimit)
                {
                    handleImage.position = m_initialHandlePosition + (Vector3.Normalize(pos - m_initialHandlePosition) * distanceLimit);
                }

                vDelta = (handleImage.position.y - m_initialHandlePosition.y) / distanceLimit;
                hDelta = (handleImage.position.x - m_initialHandlePosition.x) / distanceLimit;
            }
            else
            {
                if(m_currentHandleSnapDuration <= 1.0f)
                {
                    m_currentHandleSnapDuration += Time.deltaTime / m_handleSnapDuration;

                    handleImage.position = Vector3.Lerp(m_HandleFromPos, m_initialHandlePosition, m_currentHandleSnapDuration);
                }
            }
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
            m_Character.Move(m_Move, (m_mouseCurrentPosition - m_mousePreviousPosition).x, m_Crouch, m_Jump);
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

        public void OnHandleDragStarted()
        {
            m_DragStarted = true;
        }

        public void OnHandleDragEnded()
        {
            m_DragStarted = false;

            m_currentHandleSnapDuration = 0.0f;

            m_HandleFromPos = handleImage.position;
            //Code to snap
        }
    }
}
