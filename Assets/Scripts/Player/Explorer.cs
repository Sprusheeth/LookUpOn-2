using LookUpOn.Core;
using UnityEngine;

namespace LookUpOn.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class Explorer : MonoBehaviour
    {
        public Camera View;
        public bool Captured;
        public int Seed;
        public Vector3 Spawn;
        public System.Func<float, float, bool> CanStand;
        CharacterController controller;
        float pitch = 8, vertical;
        void Awake() { controller = GetComponent<CharacterController>(); }
        public void Capture(bool capture)
        {
            Captured = capture; Cursor.lockState = capture ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !capture;
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab)) Capture(!Captured);
            if (Captured)
            {
                transform.Rotate(0, Input.GetAxisRaw("Mouse X") * 2, 0);
                pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * 2, -80, 80);
                View.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            }
            Vector3 move = Vector3.zero;
            if (CanStand != null && !CanStand(transform.position.x, transform.position.z)) { vertical = 0; return; }
            if (Captured)
            {
                float x = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
                float z = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
                move = (transform.right * x + transform.forward * z).normalized * (Input.GetKey(KeyCode.LeftShift) ? 14 : 7);
            }
            Vector3 target = transform.position + move * Mathf.Min(Time.deltaTime, .05f);
            if (!WorldMath.Inside(target.x,target.z) || CanStand != null && !CanStand(target.x,target.z)) move = Vector3.zero;
            if (controller.isGrounded) { vertical = -2; if (Captured && Input.GetKeyDown(KeyCode.Space)) vertical = 8; }
            vertical -= 22 * Time.deltaTime;
            controller.Move((move + Vector3.up * vertical) * Mathf.Min(Time.deltaTime, .05f));
            if (transform.position.y < WorldMath.Height(transform.position.x, transform.position.z, Seed) - 10
                || !WorldMath.Inside(transform.position.x, transform.position.z))
            {
                controller.enabled = false; transform.position = Spawn;
                vertical = 0; controller.enabled = true;
            }
        }
        void OnApplicationFocus(bool focused) { if (!focused) Capture(false); }
        void OnDisable() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
}
