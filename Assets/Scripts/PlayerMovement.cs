using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    Vector3 moveDir;
    Vector3 inputDir;
    CharacterController charCtrl; 

    public float speed = 6f;
    public float turnSmoothTime = 0.1f;
    private float turnSmoothSpeed;

    // Start is called before the first frame update
    void Start()
    {
        charCtrl = GetComponent<CharacterController>(); 
    }

    // Update is called once per frame
    void Update()
    {
        if (inputDir.magnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + Camera.main.transform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothSpeed, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            //charCtrl.Move(moveDir.normalized * speed * Time.deltaTime); 
        }
        else
        {
            moveDir = Vector3.zero;
        }
    }

    private void FixedUpdate()
    {
        if (moveDir.magnitude > 0.1f)
        {
            charCtrl.Move(moveDir * speed * Time.deltaTime);
        }
    }

    void OnMove(InputValue value)
    { 
        var _moveDir =value.Get<Vector2>();
        inputDir = new Vector3(_moveDir.x, 0, _moveDir.y).normalized; 

    }
}
