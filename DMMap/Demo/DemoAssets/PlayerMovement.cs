using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour {

    public float speed = 10f;
    public float rotationSpeed = 10f;
    public bool sideScroller = false;

	void Start () {
	
	}
	
	void Update () {
        if (!sideScroller) {
            //Vector3 move = new Vector3();
            this.GetComponent<CharacterController>().transform.Rotate(new Vector3(0f, Input.GetAxis("Horizontal")  * Time.deltaTime * rotationSpeed, 0f));
            //this.GetComponent<CharacterController>().transform.Translate(Vector3.forward * Input.GetAxis("Vertical") * Time.deltaTime * speed);

            Vector3 fwd = this.gameObject.transform.forward * Input.GetAxis("Vertical") * speed;
            //Vector3 rgt = this.gameObject.transform.right * Input.GetAxis("Horizontal") * speed;
            //Vector3 move = (fwd + rgt);
            this.GetComponent<CharacterController>().Move(fwd);

            if (Input.GetKey(KeyCode.Space)) {
                //Debug.Log("Why are we not moving!");
                this.GetComponent<CharacterController>().transform.Translate(Vector3.up * Time.deltaTime * 10);
            }

            if (Input.GetKey(KeyCode.LeftShift)) {
                this.GetComponent<CharacterController>().transform.Translate(Vector3.up * -1 * Time.deltaTime * 10);
            }
        } else {
            //this.GetComponent<CharacterController>().transform.Rotate(new Vector3(0f, Input.GetAxis("Horizontal") * 10 * Time.deltaTime * speed, 0f));
            //this.GetComponent<CharacterController>().transform.Translate(Vector3.right *-1* Input.GetAxis("Horizontal") * Time.deltaTime * speed);

            Vector3 fwd = this.gameObject.transform.forward * Input.GetAxis("Vertical") * speed;
            Vector3 rgt = this.gameObject.transform.right * Input.GetAxis("Horizontal") * -speed;
            Vector3 move = (fwd + rgt);
            this.GetComponent<CharacterController>().Move(move);

            if (Input.GetKey(KeyCode.Space)) {
                this.GetComponent<CharacterController>().transform.Translate(Vector3.up * Time.deltaTime * 10);
            }

            if (Input.GetKey(KeyCode.LeftShift)) {
                this.GetComponent<CharacterController>().transform.Translate(Vector3.up * -1 * Time.deltaTime * 10);
            }
        }
	}
}
