using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
   

    //control movement with a statemachine
    public enum MovementStates
	{
        walking,
        running,
        jumping,
        crouch,
        inAir,
        sliding //soon
    }
    public MovementStates movementState = MovementStates.walking;


    [Header("Objects to refrence")]
    [SerializeField] private CapsuleCollider movementCollider;
    [SerializeField] private Transform Cam;
    [SerializeField] private Transform Head;

    [Space]
    [Header("SpeedVariables")]
    public float walkSpeed = 10;
    public float crouchSpeed = 5;
    public float runSpeed = 20;
    public float jumpForce = 1000;
    [Space]

    [Tooltip("Controls how quickly the player reaches max speed")]
    public float acceleration = 10;

    [Tooltip("Controls how quickly the player slows down without input")]
    public float counterForce = 60;
    public float minSlideVel = 5;
    public float slidingCounterForce = 1;
    public float gravForce  = 49;
    public float jumpGravScale = 0.5f;
    [Space]
    [Header("Ground finding variables")]
    [SerializeField] private float gcRadius = 0.49f;
    [SerializeField] private float gcOffset = 0.52f;
    [SerializeField] private float maxGroundAngle = 45;
    

    [SerializeField] private LayerMask groundLayer;

    [SerializeField] private bool grounded = true;
   
    

    Rigidbody rb;
    Vector2 inputDir;
    Vector3 groundNormal;
    
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();

    }

    //returns inputed dirction X represents left and right, Y represents forward / backward
    Vector2 GetDir()
    {
        Vector2 dir = new Vector2(
            Input.GetAxisRaw("Vertical"),
            Input.GetAxisRaw("Horizontal")
            ).normalized;
        return dir;
    }

    // Update is called once per frame
    void Update()
    {
        grounded = Physics.CheckSphere(transform.position - Vector3.up * gcOffset,gcRadius, groundLayer);
        RaycastHit hit;
		if (grounded)
		{
            if (Physics.Raycast(transform.position, Vector3.down, out hit, groundLayer))
            {
                groundNormal = hit.normal;
                if (Vector3.Angle(Vector3.up, groundNormal) > maxGroundAngle)
                {
                    grounded = false;
                }
            }
		}
		else
		{
            groundNormal = Vector3.down;
		}
        
        inputDir = GetDir();
        transform.rotation = Quaternion.Euler(0, Cam.rotation.eulerAngles.y, 0);
        //handle input and state changing actual movement is done in fixed update
		switch (movementState)
		{

            default: //walking state
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    Jump();
                    movementState = MovementStates.jumping;
                    break;
                }
				if (!grounded)
                {
                    movementState = MovementStates.inAir;
                    break;
                }
                if (Input.GetKey(KeyCode.LeftShift) && !(Mathf.Abs(inputDir.y) > 0 || inputDir.x < 0))
                {
                    movementState = MovementStates.running;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.LeftControl))
                {
                    movementState = MovementStates.crouch;
                    CrouchCollider();
                    break;
                }
                break;


            case MovementStates.running:
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    Jump();
                    movementState = MovementStates.jumping;
                    break;
                }
                if (!grounded)
                {
                    movementState = MovementStates.inAir;
                    break;
                }
                if (Input.GetKeyUp(KeyCode.LeftShift) ||  Mathf.Abs(inputDir.y) > 0 || inputDir.x <0)
                {
                    movementState = MovementStates.walking;
                }

                if (Input.GetKeyDown(KeyCode.LeftControl))
                {
                    movementState = MovementStates.sliding;
                    CrouchCollider();
                    break;
                }
                break;

            case MovementStates.crouch:
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    UnCrouchCollider();
                    Jump();
                    movementState = MovementStates.jumping;
                    break;
                }
                if (!grounded)
                {
                    UnCrouchCollider();
                    movementState = MovementStates.inAir;
                    break;
                }
                if (Input.GetKeyUp(KeyCode.LeftControl))
				{
                    UnCrouchCollider();
                    movementState = MovementStates.walking;
				}
                break;
            case MovementStates.jumping:
                if(rb.velocity.y < 0 || Input.GetKeyUp(KeyCode.Space))
				{
                    movementState |= MovementStates.inAir;
				}
                break;
            case MovementStates.inAir:
                if (grounded)
                {
                    if (Input.GetKey(KeyCode.LeftControl))
                    {
                        movementState = MovementStates.sliding;
                        CrouchCollider();
                    }
                    else { movementState = MovementStates.walking; }
                }
                break;
            case MovementStates.sliding:
				if (Input.GetKeyUp(KeyCode.LeftControl))
				{
                    movementState = MovementStates.walking;
                    UnCrouchCollider();
				}
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    UnCrouchCollider();
                    Jump();
                    movementState = MovementStates.jumping;
                    break;
                }
                if(rb.velocity.magnitude < minSlideVel)
				{
                    movementState = MovementStates.crouch;
				}
                break;
        }

    }
    private void Jump()
	{
        Vector3 vel = rb.velocity;
        vel.y = 0;
        rb.velocity = vel;
        rb.AddForce(Vector3.up * jumpForce);
	}
	private void FixedUpdate()
	{
        Vector3 forceDir;
        
        switch (movementState)
        {
            default: //walking State

                //add a force in the inputed direction
                forceDir = transform.forward * walkSpeed * inputDir.x * acceleration + transform.right * walkSpeed * inputDir.y * acceleration;
                rb.AddForce(forceDir);

                //clamp the velocity
                ClampVel(walkSpeed);

               
                //add a slowing force if player gives no input
                if (forceDir.sqrMagnitude == 0)
                {
                    ApplyCounterForce();
                }
                break;
            case MovementStates.crouch:
                //add a force in the inputed direction
                forceDir = transform.forward * crouchSpeed * inputDir.x * acceleration + transform.right * crouchSpeed * inputDir.y * acceleration;
                rb.AddForce(forceDir);

                //clamp the velocity
                ClampVel(crouchSpeed);

                //add a slowing force if player gives no input
                if (forceDir.sqrMagnitude == 0)
                {
                    ApplyCounterForce();
                }
                break;

            case MovementStates.running:

                //add a force in the inputed direction
                forceDir = transform.forward * runSpeed * inputDir.x * acceleration + transform.right * runSpeed * inputDir.y * acceleration;
                rb.AddForce(forceDir);

                //clamp the velocity
                ClampVel(runSpeed);

                //add a slowing force if player gives no input
                if (forceDir.sqrMagnitude == 0)
                {
                    ApplyCounterForce();
                }
                break;
            case MovementStates.jumping:
                //TODO: Add Air Control
                ApplyGravity(jumpGravScale);
                break;
            case MovementStates.inAir:
                //TODO: Add Air Control
                ApplyGravity(1);
                break;

            case MovementStates.sliding:
                ApplyGravity(2);
                ApplySlidingCounterForce();
                
                break;
        }

        //UpdateGrav();
    }

    //Called whenever the player
    void ApplyGravity(float scale)
	{
        rb.AddForce(Vector3.down * gravForce * scale);
	}

    void ClampVel(float amount)
	{
        Vector3 clampedVel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        clampedVel = Vector3.ClampMagnitude(clampedVel, amount);
        rb.velocity = clampedVel + new Vector3(0, rb.velocity.y, 0);
    }
    void ApplyCounterForce()
	{
        Vector3 counter = -rb.velocity * counterForce;
        //counter.y = 0;
        rb.AddForce(counter);
    }
    void ApplySlidingCounterForce()
    {
        Vector3 counter = -rb.velocity * slidingCounterForce;
        counter.y = 0;
        rb.AddForce(counter);
    }



    void CrouchCollider()
	{
        movementCollider.height = 1;
        movementCollider.center = new Vector3(0, -.5f, 0);
        Head.transform.localPosition = new Vector3(0, 0, 0);
    }
    void UnCrouchCollider()
    {
        movementCollider.height = 2;
        movementCollider.center = new Vector3(0, 0, 0);
        Head.transform.localPosition = new Vector3(0, 0.8f, 0);
    }

    bool CanJump()
	{
        return false;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position - Vector3.up * gcOffset, gcRadius);
	}

}
