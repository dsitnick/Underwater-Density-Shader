using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRTools {
    [RequireComponent(typeof(Rigidbody))]
    public class VRObject : MonoBehaviour {

        public bool CanGrab = true, CanMove = true;

        public Transform GrabRoot;

        [Range(0, 100)]
        public int BufferLength = 8;

        [System.Serializable]
        public class ObjEvent : UnityEvent<GameObject> { }
        public ObjEvent OnGrab, OnRelease, OnUse;

        public VRHand Holder { get; private set; }
        public Vector3 Velocity { get; private set; }
        public Vector3 Spin { get; private set; }
        public Vector3 SmoothVelocity { get {
                Vector3 result = Vector3.zero;
                for (int i = 0; i < BufferLength; i++) {
                    result += velocityBuffer[i];
                }
                return result / BufferLength;
            } }
        public Vector3 SmoothSpin { get {
                Vector3 result = Vector3.zero;
                for (int i = 0; i < BufferLength; i++) {
                    result += spinBuffer[i];
                }
                return result / BufferLength;
            } }

        private Rigidbody rb;

        private Vector3 holdPos;
        private Quaternion holdRot;

        private Vector3[] velocityBuffer, spinBuffer;
        private Vector3 lastPos;
        private Quaternion lastRot;

        void Awake () {
            rb = GetComponent<Rigidbody> ();
            rb.maxAngularVelocity = Mathf.Infinity;

            LocalColliders = new HashSet<Collider> ();
            foreach (Collider c in GetComponents<Collider> ()) {
                LocalColliders.Add (c);
            }
            foreach (Collider c in GetComponentsInChildren<Collider> (true)) {
                LocalColliders.Add (c);
            }

            velocityBuffer = new Vector3[BufferLength];
            spinBuffer = new Vector3[BufferLength];
            lastPos = rb.position;
            lastRot = rb.rotation;
        }

        public void Grab (VRHand hand, Vector3 position, Quaternion rotation) {
            if (Holder != null || !CanGrab)
                return;
            
            Holder = hand;
            holdPos = RelativePos (GrabRoot ? GrabRoot.position : position);
            holdRot = RelativeRot (GrabRoot ? GrabRoot.rotation : rotation);
            OnGrab.Invoke (hand.gameObject);
        }

        public void Release (VRHand hand) {
            if (Holder != hand)
                return;

            TrackHolder (Holder.position, Holder.rotation);
            rb.velocity = SmoothVelocity;
            rb.angularVelocity = SmoothSpin;

            OnRelease.Invoke (hand.gameObject);

            Holder = null;
        }

        public void Use(VRHand hand) {
            OnUse.Invoke (hand.gameObject);
        }

        protected virtual void TrackHolder (Vector3 position, Quaternion rotation) {
            if (Holder == null)
                return;

            rotation *= holdRot;
            position -= rotation * holdPos;

            rb.rotation = rotation;
            rb.position = position;
            rb.velocity = rb.angularVelocity = Vector3.zero;
        }

        void FixedUpdate () {
            if (CanMove && Holder)
                TrackHolder (Holder.position, Holder.rotation);

            Velocity = (rb.position - lastPos) / Time.fixedDeltaTime;
            Spin = GetSpin (rb.rotation, lastRot, Time.fixedDeltaTime);

            for (int i = BufferLength - 1; i > 0; i--) {
                velocityBuffer[i] = velocityBuffer[i - 1];
                spinBuffer[i] = spinBuffer[i - 1];
            }
            velocityBuffer[0] = Velocity;
            spinBuffer[0] = Spin;

            lastPos = rb.position;
            lastRot = rb.rotation;
        }

        public Vector3 RelativePos (Vector3 pos) {
            return Quaternion.Inverse (transform.rotation) * (pos - transform.position);
        }
        public Quaternion RelativeRot (Quaternion rot) {
            return Quaternion.Inverse (rot) * transform.rotation;
        }

        #region Colliders

        public static Dictionary<Collider, VRObject> ColliderOwners = new Dictionary<Collider, VRObject> ();

        public static VRObject GetOwner (Collider c) {
            return ColliderOwners.ContainsKey (c) ? ColliderOwners[c] : null;
        }

        private HashSet<Collider> LocalColliders;
        
        void OnEnable () {
            foreach (Collider c in LocalColliders) {
                ColliderOwners.Add (c, this);
            }
        }

        void OnDisable () {
            foreach (Collider c in LocalColliders) {
                ColliderOwners.Remove (c);
            }
        }

        #endregion

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