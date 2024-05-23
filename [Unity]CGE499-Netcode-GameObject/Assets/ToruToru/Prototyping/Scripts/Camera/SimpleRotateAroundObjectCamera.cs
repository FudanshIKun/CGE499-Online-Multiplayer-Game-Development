using UnityEngine;

namespace ToruToru{
    internal class SimpleRotateAroundObjectCamera : MonoBehaviour {
        public float ZoomSpeed = 5.0f;
        public float RotateSpeed = 10.0f;
        public GameObject Target;
        private Vector3 point;
 
        //---------------------//
        // BEHAVIOUR INTERFACE //
        //---------------------//
        private void Start(){
            point = Target.transform.position;
            transform.LookAt(point);
        }
 
        private void Update () {
            transform.RotateAround(point, new Vector3(0.0f, -1.0f, 0.0f), 20 * Time.deltaTime * RotateSpeed);
            if (Input.GetKey(KeyCode.RightArrow))
                transform.position += transform.right * ZoomSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.LeftArrow))
                transform.position -= transform.right * ZoomSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.UpArrow))
                transform.position += transform.forward * ZoomSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.DownArrow))
                transform.position -= transform.forward * ZoomSpeed * Time.deltaTime;
        }
    }
}