using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    public Vector2 MoveInput;
    public Vector2 ViewInput;
    public bool JumpInput;
    public bool JumpInputPress = false;
    public bool JumpInputRelease = false;
    public bool SprintInput;
    public bool CrouchInput;

    void Awake() {
        if(Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    void LateUpdate() {
        JumpInputPress = false;
        JumpInputRelease = false;
    }

    public void SetMove(InputAction.CallbackContext CTX) {
        MoveInput = CTX.ReadValue<Vector2>();
    }

    public void SetView(InputAction.CallbackContext CTX) {
        ViewInput = CTX.ReadValue<Vector2>();
    }

    public void SetJump(InputAction.CallbackContext CTX) {
        // When using ReadValue for buttons, 1 = pressed, 0 = not pressed
        JumpInput = CTX.ReadValue<float>() > 0f;
        
        // For Press detection - use the Performed phase
        if (CTX.phase == InputActionPhase.Performed) {
            JumpInputPress = true;  // True for 1 frame
        }
        // For Release detection - use the Canceled phase
        else if (CTX.phase == InputActionPhase.Canceled) {
            JumpInputRelease = true;  // True for 1 frame
        }
    }

    public void SetSprint(InputAction.CallbackContext CTX) {
        SprintInput = CTX.ReadValue<float>() > 0f;
    }

    public void SetCrouch(InputAction.CallbackContext CTX) {
        CrouchInput = CTX.ReadValue<float>() > 0f;
    }
}
