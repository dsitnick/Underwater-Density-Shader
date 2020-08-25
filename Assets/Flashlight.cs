using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flashlight : MonoBehaviour {

    public Color lightColor;
    public Material flashlightMaterial, normalMaterial;

    public VRInput.Hand hand;
    public VRInput.Button button = VRInput.Button.Touchpad;

    private Light light;
    private MeshRenderer rend;
    private bool enabled = false;

    void Awake () {
        light = GetComponentInChildren<Light> ();
        rend = GetComponentInChildren<MeshRenderer> ();
        SetEnabled (false);
    }

    public void SetEnabled(bool enabled) {
        this.enabled = enabled;
        rend.material = enabled ? flashlightMaterial : normalMaterial;
        light.color = enabled ? lightColor : Color.black;
    }

    void Update () {
        if (VRInput.GetButtonDown(button, hand)) {
            SetEnabled (!enabled);
        }
    }
}
