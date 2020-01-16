using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR.MagicLeap;

[RequireComponent(typeof(ControllerConnectionHandler))]
public class latkInput_MagicLeap : MonoBehaviour {

    public LightningArtist latk;
    public LatkNetwork latkNetwork;	

    private ControllerConnectionHandler _controllerConnectionHandler;
	private int _lastLEDindex = -1;

    private const float TRIGGER_DOWN_MIN_VALUE = 0.2f;
    private const float HALF_HOUR_IN_DEGREES = 15.0f;
    private const float DEGREES_PER_HOUR = 12.0f / 360.0f;

    private const int MIN_LED_INDEX = (int)(MLInputControllerFeedbackPatternLED.Clock12);
    private const int MAX_LED_INDEX = (int)(MLInputControllerFeedbackPatternLED.Clock6And12);
    private const int LED_INDEX_DELTA = MAX_LED_INDEX - MIN_LED_INDEX;

    private void Start() {
        _controllerConnectionHandler = GetComponent<ControllerConnectionHandler>();

        MLInput.OnControllerButtonUp += HandleOnButtonUp;
        MLInput.OnControllerButtonDown += HandleOnButtonDown;
        MLInput.OnTriggerDown += HandleOnTriggerDown;
        MLInput.OnTriggerUp += HandleOnTriggerUp;
    }

    private void Update() {
        UpdateLED();
    }

    private void OnDestroy() {
        if (MLInput.IsStarted) {
            MLInput.OnTriggerDown -= HandleOnTriggerDown;
            MLInput.OnTriggerUp -= HandleOnTriggerUp;
            MLInput.OnControllerButtonDown -= HandleOnButtonDown;
            MLInput.OnControllerButtonUp -= HandleOnButtonUp;
        }
    }

    private void UpdateLED() {
        if (!_controllerConnectionHandler.IsControllerValid()) {
            return;
        }

        MLInputController controller = _controllerConnectionHandler.ConnectedController;
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

    private void HandleOnButtonDown(byte controllerId, MLInputControllerButton button) {
        MLInputController controller = _controllerConnectionHandler.ConnectedController;
        if (controller != null && controller.Id == controllerId &&
            button == MLInputControllerButton.Bumper) {
            // Demonstrate haptics using callbacks.
            controller.StartFeedbackPatternVibe(MLInputControllerFeedbackPatternVibe.ForceDown, MLInputControllerFeedbackIntensity.Medium);
            // Toggle UseCFUIDTransforms
            controller.UseCFUIDTransforms = !controller.UseCFUIDTransforms;
        }
    }

    private void HandleOnButtonUp(byte controllerId, MLInputControllerButton button) {
        MLInputController controller = _controllerConnectionHandler.ConnectedController;
        if (controller != null && controller.Id == controllerId &&
            button == MLInputControllerButton.Bumper) {
            // Demonstrate haptics using callbacks.
            controller.StartFeedbackPatternVibe(MLInputControllerFeedbackPatternVibe.ForceUp, MLInputControllerFeedbackIntensity.Medium);
            //latk.useCollisions = !latk.useCollisions;
        }
    }

    private void HandleOnTriggerDown(byte controllerId, float value) {
        MLInputController controller = _controllerConnectionHandler.ConnectedController;
        if (controller != null && controller.Id == controllerId) {
            MLInputControllerFeedbackIntensity intensity = (MLInputControllerFeedbackIntensity)((int)(value * 2.0f));
            controller.StartFeedbackPatternVibe(MLInputControllerFeedbackPatternVibe.Buzz, intensity);
            latk.clicked = true;
        }
    }

    private void HandleOnTriggerUp(byte controllerId, float value) {
        MLInputController controller = _controllerConnectionHandler.ConnectedController;
        if (controller != null && controller.Id == controllerId) {
            MLInputControllerFeedbackIntensity intensity = (MLInputControllerFeedbackIntensity)((int)(value * 2.0f));
            controller.StartFeedbackPatternVibe(MLInputControllerFeedbackPatternVibe.Buzz, intensity);
            latk.clicked = false;
            List<Vector3> points = latk.layerList[latk.currentLayer].frameList[latk.layerList[latk.currentLayer].currentFrame].brushStrokeList[latk.layerList[latk.currentLayer].frameList[latk.layerList[latk.currentLayer].currentFrame].brushStrokeList.Count - 1].points;

            if (latkNetwork != null) latkNetwork.sendStrokeData(points);
        }
    }

}
