﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class WaterShaderApplicator : MonoBehaviour {

    public Material effectMaterial;

    private Camera cam;
    private Light[] lights;

    void Awake () {
        cam = GetComponent<Camera> ();
    }

    void OnEnable () {
        if (cam == null)
            cam = GetComponent<Camera> ();

        lights = FindObjectsOfType<Light> ();

        cam.depthTextureMode |= DepthTextureMode.Depth;
    }

    // Postprocess the image
    void OnRenderImage (RenderTexture source, RenderTexture destination) {

        //Set params here
        var p = GL.GetGPUProjectionMatrix (cam.projectionMatrix, false);// Unity flips its 'Y' vector depending on if its in VR, Editor view or game view etc... (facepalm)
        p[2, 3] = p[3, 2] = 0.0f;
        p[3, 3] = 1.0f;
        var clipToWorld = Matrix4x4.Inverse (p * cam.worldToCameraMatrix) * Matrix4x4.TRS (new Vector3 (0, 0, -p[2, 2]), Quaternion.identity, Vector3.one);
        effectMaterial.SetMatrix ("clipToWorld", clipToWorld);

        Vector4[] lightPositions = new Vector4[8];
        Vector4[] lightColors = new Vector4[8];
        Vector4[] lightProps = new Vector4[8];

        for (int i = 0; i < 8; i++) {
            lightColors[i] = lightPositions[i] = lightProps[i] = Vector4.zero;

            if (lights.Length > i) {
                lightColors[i] = lights[i].color;
                if (lights[i].type != LightType.Directional) {
                    lightPositions[i] = lights[i].transform.position;
                    lightPositions[i].w = 1;
                    lightProps[i].x = lights[i].range;
                } else {
                    lightPositions[i] = lights[i].transform.forward;
                    lightPositions[i].w = 0;
                }
            }
        }

        effectMaterial.SetVectorArray ("lightPositions", lightPositions);
        effectMaterial.SetVectorArray ("lightColors", lightColors);
        effectMaterial.SetVectorArray ("lightProps", lightProps);

        if (effectMaterial != null) {
            Graphics.Blit (source, destination, effectMaterial);
        } else {
            Graphics.Blit (source, destination);
        }

    }
}