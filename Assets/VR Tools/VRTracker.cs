using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRTools { 
    public class VRTracker : MonoBehaviour {

        public enum UpdateMode { Update, UpdateAndPostRender, FixedUpdate }
        public enum TrackingMode { Position, Rotation, PositionAndRotation }

        public VRInput.Device device;
        public bool local = true;
        public UpdateMode updateMode = UpdateMode.Update;
        public TrackingMode trackingMode = TrackingMode.PositionAndRotation;

        private Rigidbody rb;

        void Awake () {
            rb = GetComponent<Rigidbody> ();
        }

        void Update () {
            if (updateMode == UpdateMode.Update || updateMode == UpdateMode.UpdateAndPostRender)
                updateDevice ();
        }

        void FixedUpdate () {
            if (updateMode == UpdateMode.FixedUpdate)
                updateDevice ();
        }

        void OnPostRender () {
            if (updateMode == UpdateMode.UpdateAndPostRender)
                updateDevice ();
        }

        private void updateDevice () {
            if (trackingMode != TrackingMode.Rotation) {
                Vector3 position = VRInput.GetPosition (device);

                if (local && transform.parent != null)
                    position = transform.parent.TransformPoint (position);

                if (rb) {
                    rb.position = position;
                } else {
                    transform.position = position;
                }
            }
            if (trackingMode != TrackingMode.Position) {
                Quaternion rotation = VRInput.GetRotation (device);

                if (local && transform.parent != null)
                    rotation = transform.parent.rotation * rotation;

                if (rb) {
                    rb.rotation = rotation;
                } else {
                    transform.rotation = rotation;
                }
            }
        }

    }
}