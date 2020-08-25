using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public static class VRInput {

    public enum Device { HMD, Left, Right }
    public enum Hand { Both, Left, Right }
    public enum Button { Touchpad, Trigger, Grip, Menu, Back }
    public enum Axis { Horizontal, Vertical, Trigger, Grip }

    public static Vector3 GetPosition (Device device) {
        return InputTracking.GetLocalPosition (getNode (device));
    }

    public static Quaternion GetRotation (Device device) {
        return InputTracking.GetLocalRotation (getNode (device));
    }

    public static void HapticPulse(Hand hand, float amplitude) {
        switch (hand) {
        case Hand.Both:
            pulse (XRNode.LeftHand, amplitude);
            pulse (XRNode.RightHand, amplitude);
            break;
        case Hand.Left:
            pulse (XRNode.LeftHand, amplitude);
            break;
        case Hand.Right:
            pulse (XRNode.RightHand, amplitude);
            break;
        }
    }

    public static bool GetButton (Button button, Hand hand) {
        switch (hand) {
        case Hand.Left:
            return Input.GetKey (buttonKey (button, false));
        case Hand.Right:
            return Input.GetKey (buttonKey (button, true));
        case Hand.Both:
            return Input.GetKey (buttonKey (button, true)) || Input.GetKey (buttonKey (button, false));
        }
        return false;
    }
    public static bool GetButtonDown (Button button, Hand hand) {
        switch (hand) {
        case Hand.Left:
            return Input.GetKeyDown (buttonKey (button, false));
        case Hand.Right:
            return Input.GetKeyDown (buttonKey (button, true));
        case Hand.Both:
            return Input.GetKeyDown (buttonKey (button, true)) || Input.GetKeyDown (buttonKey (button, false));
        }
        return false;
    }
    public static bool GetButtonUp (Button button, Hand hand) {
        switch (hand) {
        case Hand.Left:
            return Input.GetKeyUp (buttonKey (button, false));
        case Hand.Right:
            return Input.GetKeyUp (buttonKey (button, true));
        case Hand.Both:
            return Input.GetKeyUp (buttonKey (button, true)) || Input.GetKeyUp (buttonKey (button, false));
        }
        return false;
    }

    public static float GetAxis(Axis axis, Hand hand) {
        switch (hand) {
        case Hand.Left:
            return Input.GetAxis (axisName (axis, false));
        case Hand.Right:
            return Input.GetAxis (axisName (axis, true));
        case Hand.Both:
            return Input.GetAxis (axisName (axis, false)) + Input.GetAxis (axisName (axis, true));
        }
        return 0;
    }

    public static Vector2 GetTouchpadValue(Hand hand) {
        return new Vector2 (GetAxis (Axis.Horizontal, hand), GetAxis (Axis.Vertical, hand));
    }

    #region Utility

    private static XRNode getNode(Device device) {
        switch (device) {
        case Device.HMD:
            return XRNode.Head;
        case Device.Left:
            return XRNode.LeftHand;
        case Device.Right:
            return XRNode.RightHand;
        default:
            return XRNode.Head;
        }
    }

    private static void pulse(XRNode node, float amplitude) {
        InputDevices.GetDeviceAtXRNode (node).SendHapticImpulse (0, amplitude, 1);
    }

    private static KeyCode buttonKey(Button button, bool right) {
        int id = 0;
        switch (button) {
        case Button.Touchpad:
            id = right ? 9 : 8;
            break;
        case Button.Menu:
            id = right ? 0 : 2;
            break;
        case Button.Trigger:
            id = right ? 15 : 14;
            break;
        case Button.Grip:
            id = right ? 5 : 4;
            break;
        case Button.Back:
            id = right ? 1 : 3;
            break;
        }
        return KeyCode.JoystickButton0 + id;
    }

    private static string axisName(Axis axis, bool right) {
        return "Joystick " + axisName (axis, right);
    }

    private static int axisIndex(Axis axis, bool right) {
        switch (axis) {
        case Axis.Horizontal:
            return !right ? 1 : 2;
        case Axis.Vertical:
            return !right ? 4 : 5;
        case Axis.Trigger:
            return !right ? 9 : 10;
        case Axis.Grip:
            return !right ? 11 : 12;
        }
        Debug.LogError ("Invalid Axis ID " + ((int)axis));
        return 0;
    }

    #endregion
}
