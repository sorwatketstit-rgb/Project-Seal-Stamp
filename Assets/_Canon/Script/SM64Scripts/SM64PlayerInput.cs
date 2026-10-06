using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SM64
{
    public class SM64PlayerInput : MonoBehaviour
    {
        [Header("Controls Sensitivity")]
        public float lookSensitivity = 1.5f;

        // Vector Input values
        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }

        // Action Trigger Signals
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool CrouchPressed { get; private set; }
        public bool CrouchHeld { get; private set; }
        public bool ActionPressed { get; private set; }

        private void Update()
        {
            ReadInput();
        }

        private void ReadInput()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;

            float moveX = 0f;
            float moveY = 0f;

            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveY += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveY -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveX -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveX += 1f;
            }

            if (gamepad != null)
            {
                Vector2 stick = gamepad.leftStick.ReadValue();
                if (stick.magnitude > 0.1f)
                {
                    moveX = stick.x;
                    moveY = stick.y;
                }
            }

            MoveInput = Vector2.ClampMagnitude(new Vector2(moveX, moveY), 1f);

            // Look input
            float lookX = 0f;
            float lookY = 0f;

            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                Vector2 delta = Mouse.current.delta.ReadValue();
                // Normalize pixel delta to smooth degree units
                lookX = delta.x * 0.005f * lookSensitivity;
                lookY = delta.y * 0.005f * lookSensitivity;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (gamepad != null)
            {
                Vector2 rStick = gamepad.rightStick.ReadValue();
                if (rStick.magnitude > 0.1f)
                {
                    lookX = rStick.x * lookSensitivity * Time.deltaTime;
                    lookY = rStick.y * lookSensitivity * Time.deltaTime;
                }
            }

            LookInput = new Vector2(lookX, lookY);

            // Action inputs
            JumpPressed = (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) ||
                          (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);
            JumpHeld = (keyboard != null && keyboard.spaceKey.isPressed) ||
                       (gamepad != null && gamepad.buttonSouth.isPressed);

            CrouchPressed = (keyboard != null && (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.cKey.wasPressedThisFrame)) ||
                            (gamepad != null && gamepad.buttonEast.wasPressedThisFrame);
            CrouchHeld = (keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.cKey.isPressed)) ||
                         (gamepad != null && gamepad.buttonEast.isPressed);

            ActionPressed = (keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.leftCtrlKey.wasPressedThisFrame)) ||
                            (gamepad != null && gamepad.buttonWest.wasPressedThisFrame);
#else
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");
            MoveInput = Vector2.ClampMagnitude(new Vector2(moveX, moveY), 1f);

            float lookX = 0f;
            float lookY = 0f;

            if (Input.GetMouseButton(1))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                lookX = Input.GetAxis("Mouse X") * lookSensitivity * 0.05f;
                lookY = Input.GetAxis("Mouse Y") * lookSensitivity * 0.05f;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            LookInput = new Vector2(lookX, lookY);

            JumpPressed = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space);
            JumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.Space);

            CrouchPressed = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.C);
            CrouchHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.C);

            ActionPressed = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.LeftControl);
#endif
        }
    }
}
