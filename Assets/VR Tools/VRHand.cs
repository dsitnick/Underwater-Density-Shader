using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRTools {
    [RequireComponent(typeof(Rigidbody))]
    public class VRHand : MonoBehaviour {

        public VRInput.Button GrabButton = VRInput.Button.Trigger, UseButton = VRInput.Button.Touchpad;
        public VRInput.Hand Hand = VRInput.Hand.Right;

        public int BufferLength = 8;
        
        [System.Serializable]
        public class ObjEvent : UnityEvent<GameObject> { }
        public ObjEvent OnGrab, OnRelease, OnUse;

        public VRObject currentObject { get; private set; }

        public Vector3 position { get { return rb.position; } }
        public Quaternion rotation { get { return rb.rotation; } }
        
        public Vector3 Velocity { get; private set; }
        public Vector3 Spin { get; private set; }
        public Vector3 SmoothVelocity {
            get {
                Vector3 result = Vector3.zero;
                for (int i = 0; i < BufferLength; i++) {
                    result += velocityBuffer[i];
                }
                return result / BufferLength;
            }
        }
        public Vector3 SmoothSpin {
            get {
                Vector3 result = Vector3.zero;
                for (int i = 0; i < BufferLength; i++) {
                    result += spinBuffer[i];
                }
                return result / BufferLength;
            }
        }

        private Rigidbody rb;

        private HashSet<Collider> intersecting;
        private Vector3[] velocityBuffer, spinBuffer;
        private Vector3 lastPos;
        private Quaternion lastRot;

        void Awake () {
            rb = GetComponent<Rigidbody> ();
            intersecting = new HashSet<Collider> ();
            Debug.Assert (rb.isKinematic);

            velocityBuffer = new Vector3[BufferLength];
            spinBuffer = new Vector3[BufferLength];
            lastPos = transform.localPosition;
            lastRot = transform.localRotation;
        }

        void Update () {
            if (VRInput.GetButtonDown(GrabButton, Hand) && currentObject == null) {
                Grab ();
            }
            if (VRInput.GetButtonUp(GrabButton, Hand) && currentObject != null) {
                Release ();
            }
            if (VRInput.GetButtonDown(UseButton, Hand) && currentObject != null) {
                Use ();
            }
        }

        void FixedUpdate () {
            Vector3 pos = transform.localPosition;
            Quaternion rot = transform.localRotation;

            Velocity = (pos - lastPos) / Time.fixedDeltaTime;
            Spin = GetSpin (rot, lastRot, Time.fixedDeltaTime);

            for (int i = BufferLength - 1; i > 0; i--) {
                velocityBuffer[i] = velocityBuffer[i - 1];
                spinBuffer[i] = spinBuffer[i - 1];
            }
            velocityBuffer[0] = Velocity;
            spinBuffer[0] = Spin;

            lastPos = pos;
            lastRot = rot;
        }

        public virtual void Grab () {
            if (currentObject != null)
                return; 

            currentObject = FindObject (rb.position);

            if (currentObject) {
                OnGrab.Invoke (currentObject.gameObject);
                currentObject.Grab (this, rb.position, rb.rotation);
            }
        }

        public virtual void Release () {
            if (currentObject == null)
                return;

            OnRelease.Invoke (currentObject.gameObject);
            currentObject.Release (this);
            currentObject = null;
        }

        public virtual void Use () {
            if (currentObject == null)
                return;

            OnUse.Invoke (currentObject.gameObject);
            currentObject.Use (this);
        }

        public VRObject FindObject (Vector3 pos) {
            Collider closest = null;
            float dist = 0;

            foreach (Collider c in intersecting) {
                if (c && c.enabled && c.gameObject.activeInHierarchy) {
                    float d = Vector3.Distance (pos, c.ClosestPoint (pos));
                    if (closest == null || d < dist) {
                        dist = d;
                        closest = c;
                    }
                }
            }

            return closest ? VRObject.GetOwner (closest) : null;
        }

        void OnTriggerEnter (Collider other) {
            VRObject obj = other.GetComponent<VRObject> ();
            if (obj == null)
                obj = other.GetComponentInParent<VRObject> ();

            if (obj != null) {
                intersecting.Add (other);
            }
        }

        void OnTriggerExit (Collider other) {
            intersecting.Remove (other);
        }

        public static Vector3 GetSpin (Quaternion to, Quaternion from, float dt) {
            if (Vector3.Distance (to.eulerAngles, from.eulerAngles) < 0.001f) {
                return Vector3.zero;
            }
            float angle = 0f;
            Vector3 axis = Vector3.zero;
            Quaternion q = to * Quaternion.Inverse (from);
            q.ToAngleAxis (out angle, out axis);
            if (System.Single.IsInfinity (axis.x) || System.Single.IsNaN (axis.x)) {
                return Vector3.zero;
            }
            return axis * angle * Mathf.Deg2Rad / dt;
        }

    }
}