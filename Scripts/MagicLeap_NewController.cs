using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR.MagicLeap;

[RequireComponent(typeof(ControllerConnectionHandler))]
public class MagicLeap_NewController : MonoBehaviour {

    private ControllerConnectionHandler _controllerConnectionHandler;
    private int _lastLEDindex = -1;

    private const float TRIGGER_DOWN_MIN_VALUE = 0.2f;
    private const float HALF_HOUR_IN_DEGREES = 15.0f;
    private const float DEGREES_PER_HOUR = 12.0f / 360.0f;

    private const int MIN_LED_INDEX = (int)(MLInputControllerFeedbackPatternLED.Clock12);
    private const int MAX_LED_INDEX = (int)(MLInputControllerFeedbackPatternLED.Clock6And12);
    private const int LED_INDEX_DELTA = MAX_LED_INDEX - MIN_LED_INDEX;

    public bool triggerPressed = false;
    public bool padPressed = false;
    public bool gripped = false;
    public bool menuPressed = false;
    public bool triggerDown = false;
    public bool padDown = false;
    public bool gripDown = false;
    public bool menuDown = false;
    public bool triggerUp = false;
    public bool padUp = false;
    public bool gripUp = false;
    public bool menuUp = false;

    public bool padDirUp = false;
    public bool padDirDown = false;
    public bool padDirLeft = false;
    public bool padDirRight = false;
    public bool padDirCenter = false;

    public Vector3 touchpad = new Vector2(0f, 0f);

    [HideInInspector] public Vector3 startPos = Vector3.zero;
    [HideInInspector] public Vector3 endPos = Vector3.zero;
    [HideInInspector] public float triggerVal;

    private float touchPadLimit = 0.6f; // 0.7f;

    private void Awake() {
        _controllerConnectionHandler = GetComponent<ControllerConnectionHandler>();
    }

    private void Start() {
        MLInput.OnControllerButtonUp += HandleOnButtonUp;
        MLInput.OnControllerButtonDown += HandleOnButtonDown;

        MLInput.OnTriggerDown += HandleOnTriggerDown;
        MLInput.OnTriggerUp += HandleOnTriggerUp;
    }

    private void Update() {
        resetButtons();

        checkInputs();
    }

    private void resetButtons() {
        triggerDown = false;
        padDown = false;
        gripDown = false;
        menuDown = false;

        triggerUp = false;
        padUp = false;
        gripUp = false;
        menuUp = false;

        padDirUp = false;
        padDirDown = false;
        padDirLeft = false;
        padDirRight = false;
        padDirCenter = true;
    }

    private void checkInputs() {
        if (!_controllerConnectionHandler.IsControllerValid()) {
            return;
        }

        MLInputController controller = _controllerConnectionHandler.ConnectedController;

        checkPadDir(ref controller);
        checkTriggerVal(ref controller);
        checkLeds(ref controller);

        // The ML1 touchpad also recognizes a double press which can serve as a separate button
        if (controller.Touch2Active) {
            if (!gripDown) gripDown = true;
            gripped = true;
        } else {
            if (gripped) gripUp = true;
            gripped = false;
        }
    }

    private void checkTriggerVal(ref MLInputController controller) {
        triggerVal = controller.TriggerValue; //device.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger).x;
    }

    private void checkPadDir(ref MLInputController controller) {
        // The ML1 touchpad doesn't have a discrete button, so we fake one here
        touchpad = controller.Touch1PosAndForce; //ctl.ControllerInputDevice.TouchPos; //device.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0);

        if (touchpad.z > touchPadLimit) {
            if (!padDown) padDown = true;
            padPressed = true;
        } else {
            if (padPressed) padUp = true;
            padPressed = false;
        }

        if (touchpad.y > touchPadLimit) {
            padDirUp = true;
            padDirDown = false;
            padDirCenter = false;
        } else if (touchpad.y < -touchPadLimit) {
            padDirUp = false;
            padDirDown = true;
            padDirCenter = false;
        }

        if (touchpad.x > touchPadLimit) {
            padDirLeft = true;
            padDirRight = false;
            padDirCenter = false;
        } else if (touchpad.x < -touchPadLimit) {
            padDirLeft = false;
            padDirRight = true;
            padDirCenter = false;
        }
    }

    /*
    float defaultVibrationVal = 2f;

    public void vibrateController() {
        int ms = (int)defaultVibrationVal * 1000;
        device.TriggerHapticPulse((ushort)ms, Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad);
    }

    public void vibrateController(float val) {
        int ms = (int)val * 1000;
        device.TriggerHapticPulse((ushort)ms, Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad);
    }
    */

    private void checkLeds(ref MLInputController controller) {
        if (controller.Touch1Active) {
            // Get angle of touchpad position.
            float angle = -Vector2.SignedAngle(Vector2.up, controller.Touch1PosAndForce);
            if (angle < 0.0f) {
                angle += 360.0f;
            }

            // Get the correct hour and map it to [0,6]
            int index = (int)((angle + HALF_HOUR_IN_DEGREES) * DEGREES_PER_HOUR) % LED_INDEX_DELTA;

            // Pass from hour to MLInputControllerFeedbackPatternLED index  [0,6] -> [MAX_LED_INDEX, MIN_LED_INDEX + 1, ..., MAX_LED_INDEX - 1]
            index = (MAX_LED_INDEX + index > MAX_LED_INDEX) ? MIN_LED_INDEX + index : MAX_LED_INDEX;

            if (_lastLEDindex != index) {
                // a duration of 0 means leave it on indefinitely
                controller.StartFeedbackPatternLED((MLInputControllerFeedbackPatternLED)index, MLInputControllerFeedbackColorLED.BrightCosmicPurple, 0);
                _lastLEDindex = index;
            }
        } else if (_lastLEDindex != -1) {
            controller.StopFeedbackPatternLED();
            _lastLEDindex = -1;
        }
    }

    private void OnDestroy() {
        if (MLInput.IsStarted) {
            MLInput.OnTriggerDown -= HandleOnTriggerDown;
            MLInput.OnTriggerUp -= HandleOnTriggerUp;
            MLInput.OnControllerButtonDown -= HandleOnButtonDown;
            MLInput.OnControllerButtonUp -= HandleOnButtonUp;
        }
    }

    private void HandleOnButtonDown(byte controllerId, MLInputControllerButton button) {
        MLInputController controller = _controllerConnectionHandler.ConnectedController;
        if (controller != null && controller.Id == controllerId && button == MLInputControllerButton.Bumper) {
            // Demonstrate haptics using callbacks.
            controller.StartFeedbackPatternVibe(MLInputControllerFeedbackPatternVibe.ForceDown, MLInputControllerFeedbackIntensity.Medium);
            // Toggle UseCFUIDTransforms
            //controller.UseCFUIDTransforms = !controller.UseCFUIDTransforms;

            menuPressed = true;
            menuDown = true;
        }
    }

    private void HandleOnButtonUp(byte controllerId, MLInputControllerButton button) {
        MLInputController controller = _controllerConnectionHandler.ConnectedController;
        if (controller != null && controller.Id == controllerId && button == MLInputControllerButton.Bumper) {
            // Demonstrate haptics using callbacks.
            controller.StartFeedbackPatternVibe(MLInputControllerFeedbackPatternVibe.ForceUp, MLInputControllerFeedbackIntensity.Medium);

            menuPressed = false;
            menuUp = true;
        }
    }

    private void HandleOnTriggerDown(byte controllerId, float value) {
        MLInputController controller = _controllerConnectionHandler.ConnectedController;
        if (controller != null && controller.Id == controllerId) {
            MLInputControllerFeedbackIntensity intensity = (MLInputControllerFeedbackIntensity)((int)(value * 2.0f));
            controller.StartFeedbackPatternVibe(MLInputControllerFeedbackPatternVibe.Buzz, intensity);

            triggerPressed = true;
            triggerDown = true;
            startPos = transform.position;
        }
    }

    private void HandleOnTriggerUp(byte controllerId, float value) {
        MLInputController controller = _controllerConnectionHandler.ConnectedController;
        if (controller != null && controller.Id == controllerId) {
            MLInputControllerFeedbackIntensity intensity = (MLInputControllerFeedbackIntensity)((int)(value * 2.0f));
            controller.StartFeedbackPatternVibe(MLInputControllerFeedbackPatternVibe.Buzz, intensity);

            triggerPressed = false;
            triggerUp = true;
            endPos = transform.position;
        }
    }

}
