using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MonsterLove.StateMachine;
using Rewired;
using Com.LuisPedroFonseca.ProCamera2D;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : Actor
{
    [Header("Gravity & fall speed")] //Gravity,fall speed
    public float gravity = 900f;
    [Tooltip("Maximun speed at which you can fall")]
    public float maxFall = -160f;
    [Tooltip("Maximun fall speed when you're fast falling")]
    public float fastFall = -240f;

    [Header("Run speed & acceleration")] //Run speed & accel
    [Tooltip("Maximun horizontal run speed")]
    public float maxRun = 90f;
    [Tooltip("Horizontal Acceleration Speed")]
    public float runAccel = 1000f;
    [Tooltip("Horizontal Acceleration when your horizontal speed is higher or equal to the maximun")]
    public float runReduce = 400f;
    [Tooltip("Multiplier for the air horizontal movement (friction) the higher the more air control you'll have")]
    public float AirAccelerationMult = .8f;

    [Header("Jump variables")] //Jump variables
    [Tooltip("Vertical jump speed/force")]
    public float jumpSpeed = 135f;
    [Tooltip("Extra horizontal speed boost when jumping")]
    public float jumpHBoost = 40f;
    [Tooltip("Time after performing a jump on which you can hold the jump key to jump higher")]
    public float variableJumpTime = .2f;
    [Tooltip("Vertical jump speed/force for spring jumps")]
    public float springJumpSpeed = 275f;
    [Tooltip("Time after performing a jump(jumping off a spring) on which you can hold the jump key to jump higher")]
    public float springJumpVariableTime = .05f;

    [Header("Wall jump variables")] //Wall jump variables
    [Tooltip("Time on which the horizontal movement is restricted/forced after a wall jump(if too low the player migh be able to climb up a wall)")]
    public float wallJumpForceTime = .16f;
    [Tooltip("Horizontal speed boost when performing a wall jump")]
    public float wallJumpHSpeed = 130f;
    [Tooltip("Distance at we check for walls before performing a wall jump(2-4 recommended)")]
    public int wallJumpCheckDst = 2;

    [Header("Wall slide variables")] //Wall slide variables
    [Tooltip("Starting vertical speed when you wall slide")]
    public float wallSlideStartMax = -20f;
    [Tooltip("Maximun time you can wall slide before gaining the full fall speed again")]
    public float wallSlideTime = 1.2f;

    [Header("Dash variables")] //Dash variables
    public float dashSpeed = 240f;
    [Tooltip("Extra speed boost when the dash ends (2/3 of the dash speed recommended)")]
    public float endDashSpeed = 160f;
    [Tooltip("Multiplier applied to the speed after a dash ends if the direction you dashed at was up")]
    public float endDashUpMult = .75f;
    [Tooltip("The total time which a dash lasts for")]
    public float dashTime = .15f;
    public float dashCooldown = .4f;

    [Header("Other")] // Other variables used for responsive mov
    [Tooltip("Wall cling/stick time after touching a wall where you can't leave the wall(to avoid unintentionally leaving the wall when trying to perform a wall jump)")]
    public float clingTime = .125f;
    [Tooltip("Jump grace time after leaving the ground non-jump on which you can still make a jump")]
    public float jumpGraceTime = .1f;
    [Tooltip("If the player hits the ground within this time after pressing the jump button, the jump will be executed as soon as they touch the ground")]
    public float jumpBufferTime = .1f;

    [Header("Wall slide direction")] // Wall slide
    public int wallSlideDir;

    [Header("Respawn[Temporary]")] // Respawn
    [Tooltip("Total time it takes the player to respawn")]
    public float respawnTime;

    [Header("Squash & strech")]
    public Transform spriteHolder; // Reference to the transform of the child object which holds the sprite renderer
    public Vector2 spriteScale = Vector2.one; // The current X and Y scale of the sprite

    [Header("Animator")]
    public Animator animator;

    [Header("Particles")]
    public GameObject dustParticles;
    public GameObject dashDustParticle;

    [Header("Sound FX")]
    public AudioClip jumpClip;
    public AudioClip dashClip;
    public AudioClip footstepsClip;

    [HideInInspector]
    public Vector2 playerInput; // Store horizontal and vertical input each frame
    int oldMoveY; // Variable to store the vertical input for the last frame
    int forceMoveX; // Used to store the forced horizontal mov input
    float forceMoveXTimer = 0f; // Used to store the time left on the force horizontal mov
    int facingDir = 1;
    float varJumpSpeed; // Speed to apply each frame of the variable jump
    float varJumpTimer = 0f; // Store the time left on the variable jump
    float maxFallSpeed; // Store the current maximun fall speed
    float jumpGraceTimer = 0f; // Timer to store the time left to perform a jump after leaving a platform/solid
    float jumpBufferTimer = 0f; // Timer to store the time left in the JumpBuffer timer
    bool jumpIsInsideBuffer = false;
    float wallSlideTimer = 0f; // Used to store the time left on the wallSlide
    Vector2 dashDir;
    float dashCooldownTimer = 0f;
    bool canStick = false; // Helper variable for the wall sticking functionality
    bool sticking = false; // Variable to store if the player is currently sticking to a wall
    float stickTimer = 0f; // Timer to store the time left sticking to a wall
    float ledgeClimbTime = 1f; // Total time it takes to climb a wall
    float ledgeClimbTimer = 0f; // Timer to store the current time passed in the ledgeClimb state
    float respawnTimer = 0f;
    float moveToSpAfterTimer;
    float moveToSpAfterTime = .5f;
    Vector2 respawnPos;
    Vector2 extraPosOnClimb = new Vector2(10, 22); // Extra pos to add to the current one
    float footStepClipTimer;

    Player inputActions;
    public bool CanDash => inputActions.GetButtonDown("Dash")  && dashCooldownTimer <= 0f;

    public StateMachine<States> fsm;
    public enum States
    {
        Normal,
        Dash,
        Respawn,
        LedgeGrab,
        LedgeClimb,
        Attack,
    }

    new void Awake()
    {
        base.Awake();

        fsm = StateMachine<States>.Initialize(this);
    }

    void Start()
    {
        fsm.ChangeState(States.Normal, StateTransition.Overwrite);
    }

    void Update()
    {
        if (fsm.State == States.Respawn)
            return;

        //Update grounded last frame and current frame
        wasGrounded = grounded;
        grounded = IsGrounded();

        //Handle variable jump timer
        if (varJumpTimer > 0f)
            varJumpTimer -= Time.deltaTime;

        //Handle wall slide timer
        if (wallSlideDir != 0)
        {
            //Assign the time left in wall slide and resets the direction
            wallSlideTimer = Mathf.Max(wallSlideTimer - Time.deltaTime, 0f);
            wallSlideDir = 0;
        }

        //Reduce the cooldown of the dash
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        //If grounded at the start of this frame and handles jump grace timer
        if (grounded)
        {
            //Resets the wall slide and jump grace timer when on the ground
            wallSlideTimer = wallSlideTime;
            jumpGraceTimer = jumpGraceTime;
        }
        else if (jumpGraceTimer > 0f)
            jumpGraceTimer -= Time.deltaTime;

        //Reset the wall cling
        //If grounded or no collision from left and right side
        if (grounded || (!CheckColInDir(Vector2.right, solidMask) && !CheckColInDir(Vector2.left, solidMask)))
        {
            sticking = false;
            canStick = true;
        }

        //Set sticking to false when the timer has expired
        if (stickTimer > 0f && sticking)
        {
            stickTimer -= Time.deltaTime;

            if (stickTimer <= 0f)
                sticking = false;
        }

        //Jump buffer timer handling
        if (jumpIsInsideBuffer)
            jumpBufferTimer -= Time.deltaTime;

        //Jump input buffering
        if (inputActions.GetButtonDown("Jump"))
            jumpBufferTimer = jumpBufferTime;

        //Check if the jump buffer timer has ran off so set the jump to false
        if (jumpBufferTimer > 0)
            jumpIsInsideBuffer = true;
        else
            jumpIsInsideBuffer = false;

        //Just in case the jumpBufferTime has been set to 0 or less just use the old jump input
        if (jumpBufferTime <= 0)
            jumpIsInsideBuffer = inputActions.GetButtonDown("Jump");

        //Player movement input
        Vector2 input = new Vector2(inputActions.GetAxis("Move horizontal"), inputActions.GetAxis("Move vertical"));

        //Update the playerInput.x variable depending wether the movement is force or not
        if (forceMoveXTimer > 0f)
        {
            forceMoveXTimer -= Time.deltaTime;
            playerInput.x = forceMoveX;
        }
        else
            playerInput = new Vector2(Mathf.RoundToInt(input.x), playerInput.y);

        oldMoveY = Mathf.RoundToInt(playerInput.y);

        playerInput = new Vector2(playerInput.x, Mathf.RoundToInt(input.y));
    }

    void LateUpdate()
    {
        if (fsm.State == States.Respawn)
            return;

        //Landed squash
        if (grounded && !wasGrounded && speed.y <= 0)
        {
            spriteScale.x = 1.35f;
            spriteScale.y = .65f;
        }

        //Do all the movement on the actor
        //Horizontal
        bool moveH = MoveH(speed.x * Time.deltaTime);
        if (moveH)
            speed.x = 0;

        bool moveV = MoveV(speed.y * Time.deltaTime);
        if (moveV)
            speed.y = 0;

        UpdateSprite();

        // Get Crushed by block if we are collisioning with the solid layer
        if (CollisionSelf(solidMask))
            Die();
    }

    void Normal_Update()
    {
        if (CanDash)
        {
            fsm.ChangeState(States.Dash, StateTransition.Overwrite);
            return;
        }

        //Cling to wall
        //If player is moving right and colliding left(stick to the left) or vice versa
        if (((playerInput.x > 0 && CheckColInDir(Vector2.left, solidMask)) || (playerInput.x < 0 && CheckColInDir(Vector2.right, solidMask))) && canStick && !grounded)
        {
            stickTimer = clingTime;
            sticking = true;
            canStick = false;
        }

        //Horizotal speed update section
        float accelerationMultiplier = grounded ? 1f : AirAccelerationMult;

        if (!sticking)
        {
            if (Mathf.Abs(speed.x) > maxRun && Mathf.Sign(speed.x) == playerInput.x)
                speed.x = Helper.CalculateApproach(speed.x, maxRun * playerInput.x, runReduce * accelerationMultiplier * Time.deltaTime);
            else
                speed.x = Helper.CalculateApproach(speed.x, maxRun * playerInput.x, runAccel * accelerationMultiplier * Time.deltaTime);
        }

        //Vertical speed update & fast fall
        maxFallSpeed = Helper.CalculateApproach(maxFallSpeed, maxFall, 300 * Time.deltaTime);

        //Ledge grab
        if (!grounded && speed.y <= 0 && playerInput.x != 0 && playerInput.y != -1 && CheckColAtPlace(Vector2.right * playerInput.x * 2, solidMask))
        {
            Bounds bounds = myCollider.bounds;
            int extraDstY = 2;

            for (int i = 0; i < bounds.extents.y + extraDstY; i++)
            {
                if (CanGrabLedge((int)bounds.center.y + i, (int)playerInput.x) && CheckColAtPlace(Vector2.right * playerInput.x * 2, solidMask))
                {
                    GrabLedge((int)bounds.center.y + i, (int)playerInput.x);
                    return;
                }
            }
        }

        //Wall slide and gravity
        if (!grounded)
        {
            float targetFall = maxFallSpeed;
            //Wall slide
            if ((playerInput.x == facingDir) && playerInput.y != -1)
            {
                if (speed.y <= 0 && wallSlideTimer > 0 && CheckColInDir(Vector2.right * facingDir, solidMask))
                    wallSlideDir = facingDir;
                if (wallSlideDir != 0)
                    targetFall = Mathf.Lerp(maxFall, wallSlideStartMax, wallSlideTimer / wallSlideTime);
            }

            speed.y = Helper.CalculateApproach(speed.y, targetFall, gravity * Time.deltaTime);
        }

        //Handle facing direction
        if (wallSlideDir == 0)
        {
            if (playerInput.x != 0)
                facingDir = (int)playerInput.x;
        }
        else
        {
            if (Random.value < 0.35f && GameManager.instance != null)
                GameManager.instance.EmitParticles(dashDustParticle, 1, new Vector2(transform.position.x, transform.position.y) + new Vector2(wallSlideDir * 2.5f, 4), Vector2.one);
        }

        //Handle variable jumping
        if (varJumpTimer > 0f)
        {
            if (inputActions.GetButtonDown("Jump"))
                speed.y = Mathf.Max(speed.y, varJumpSpeed);
            else
                varJumpTimer = 0f;
        }

        //Drop down from a one way platform
        if (grounded && playerInput.y < 0 && jumpIsInsideBuffer && CheckColInDir(Vector2.down, oneWayMask) && !CheckColInDir(Vector2.down, solidMask))
        {
            grounded = false;
            jumpGraceTimer = 0f;
            jumpIsInsideBuffer = false;
            jumpBufferTimer = 0f;
            transform.position += new Vector3(0, -1, 0);
        }

        //Jump
        if (jumpIsInsideBuffer)
        {
            if (grounded || jumpGraceTimer > 0f)
                Jump();
            else
            {
                if (WallJumpCheck(-1, wallJumpCheckDst, solidMask))
                    WallJump(1);
                else if (WallJumpCheck(1, wallJumpCheckDst, solidMask))
                    WallJump(-1);
            }
        }

        //Footsetps SFX
        if (grounded && playerInput.x != 0 && Time.time > footStepClipTimer)
        {
            //AudioManager.instance.PlaySound2D("Footstep", 0.5f); Update to FMOD
            footStepClipTimer = Time.time + .4f;
        }
    }

    IEnumerator Dash_Enter()
    {
        //AudioManager.instance.PlaySound2D(dashClip); Update to FMOD
        dashCooldownTimer = dashCooldown;
        speed = Vector2.zero;
        dashDir = Vector2.zero;

        Vector2 target = playerInput;
        if (target == Vector2.zero)
            target = new Vector2(facingDir, 0);
        else if (target.x == 0 && target.y > 0 && grounded)
            target = new Vector2(facingDir, target.y);

        target.Normalize();
        Vector2 targetSpeed = target * dashSpeed;
        speed = targetSpeed;
        dashDir = target;
        if (dashDir.x != 0)
            facingDir = (int)Mathf.Sign(dashDir.x);

        if (dashDir.y < 0 && grounded)
        {
            dashDir.y = 0;
            dashDir.x = Mathf.Sign(dashDir.x);
            speed.y = 0f;
            speed.x *= 2;
        }

        if (dashDir.x != 0 && dashDir.y == 0)
            spriteScale = new Vector2(1.2f, .8f);
        else if (dashDir.x == 0 && dashDir.y != 0)
            spriteScale = new Vector2(.8f, 1.2f);

        ProCamera2DShake.Instance.Shake("Dash");

        yield return new WaitForSeconds(dashTime);

        //wait ont extra frame
        yield return null;

        if (dashDir.y >= 0f)
            speed = dashDir * endDashSpeed;
        if (speed.y > 0)
            speed.y = speed.y * endDashUpMult;

        fsm.ChangeState(States.Normal, StateTransition.Overwrite);
        yield break;
    }

    void Dash_Update()
    {
        if (Random.value < 0.85f && GameManager.instance != null)
            GameManager.instance.EmitParticles(dashDustParticle, Random.Range(1, 3), new Vector2(transform.position.x, transform.position.y) + new Vector2(0, -5), Vector2.one * 3f);
    }

    void Respawn_Enter()
    {
        if (GameManager.instance != null)
            respawnPos = GameManager.instance.spawnPoint.position;
        else
            respawnPos = Vector2.zero;

        GetComponentInChildren<SpriteRenderer>().sortingOrder = 1;
        animator.Play("Fall");
        respawnTimer = 0;
    }

    void Respawn_Exit()
    {
        respawnTimer = 0f;
        transform.position = respawnPos;
        GetComponentInChildren<SpriteRenderer>().sortingOrder = 1;
        spriteScale = new Vector2(1.5f, .5f);

        if (GameManager.instance != null)
            GameManager.instance.PlayerRespawn();

        Health health = GetComponent<Health>();
        health.dead = false;
        health.TakeHeal(health.maxHealth);
    }

    void Respawn_Update()
    {
        if (moveToSpAfterTime > 0)
        {
            moveToSpAfterTimer += Time.deltaTime;

            if (moveToSpAfterTimer < moveToSpAfterTime)
                return;
        }

        respawnTimer += Time.deltaTime;
        float percent = respawnTimer / respawnTime;

        if (percent >= 1f)
        {
            fsm.ChangeState(States.Normal, StateTransition.Overwrite);
            return;
        }

        transform.position = Vector2.Lerp(transform.position, respawnPos, percent);
    }

    void LedgeGrab_Enter()
    {
        speed = Vector2.zero;
        if (wallSlideDir != 0)
            wallSlideDir = 0;
    }

    void LedgeGrab_Update()
    {
        // If pressing down or the other direction which is not the ledgegrab direction
        if (playerInput.y < 0 || playerInput.x != facingDir || !CheckColAtPlace(Vector2.right * playerInput.x * 2, solidMask))
        {
            jumpGraceTimer = jumpGraceTime;
            fsm.ChangeState(States.Normal, StateTransition.Overwrite);
            return;
        }

        if (jumpIsInsideBuffer)
        {
            //Ledge climb
            if (playerInput.x == facingDir || playerInput.y > 0)
            {
                fsm.ChangeState(States.LedgeClimb, StateTransition.Overwrite);
                return;
            }
            else if (playerInput.y != -1)
                Jump();
            else if (WallJumpCheck(facingDir, wallJumpCheckDst, solidMask))
                WallJump(-facingDir);

            fsm.ChangeState(States.Normal, StateTransition.Overwrite);
            return;
        }
    }

    void LedgeClimb_Enter()
    {
        myCollider.enabled = false;
        ledgeClimbTimer = 0f;
        speed = Vector2.zero;
        if (wallSlideDir != 0)
            wallSlideDir = 0;
    }

    void LedgeClimb_Exit()
    {
        transform.position = new Vector2(transform.position.x + (extraPosOnClimb.x * facingDir), transform.position.y + extraPosOnClimb.y);
        myCollider.enabled = true;
        grounded = true;
        wasGrounded = true;

        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
            animator.Play("Idle");
    }

    void LedgeClimb_Update()
    {
        ledgeClimbTimer += Time.deltaTime;

        if (ledgeClimbTimer >= ledgeClimbTime)
        {
            ledgeClimbTimer = 0f;
            fsm.ChangeState(States.Normal, StateTransition.Overwrite);
            return;
        }
    }

    public void Jump()
    {
        wallSlideTimer = wallSlideTime;
        jumpGraceTimer = 0f;
        jumpBufferTimer = 0f;
        varJumpTimer = variableJumpTime;
        speed.x = speed.x + jumpHBoost * playerInput.x;
        speed.y = jumpSpeed;
        varJumpSpeed = speed.y;
        spriteScale = new Vector2(0.6f, 1.4f);

        if (GameManager.instance != null)
            GameManager.instance.EmitParticles(dustParticles, 5, new Vector2(transform.position.x, transform.position.y) + new Vector2(0, -5), Vector2.one * 3f);

        //AudioManager.instance.PlaySound2D(jumpClip); Update to FMOD
    }

    void WallJump(int dir)
    {
        wallSlideTimer = wallSlideTime;
        jumpGraceTimer = 0f;
        jumpBufferTimer = 0f;
        varJumpTimer = variableJumpTime;
        if (playerInput.x != 0)
        {
            forceMoveX = dir;
            forceMoveXTimer = wallJumpForceTime;
        }

        speed.x = wallJumpHSpeed * dir;
        speed.y = jumpSpeed;
        varJumpSpeed = speed.y;
        spriteScale = new Vector2(0.6f, 1.4f);

        //Particles
        if (dir == -1)
            GameManager.instance.EmitParticles(dustParticles, 5, new Vector2(transform.position.x, transform.position.y) + new Vector2(2, -5), Vector2.one * 3f);
        else if (dir == 1)
            GameManager.instance.EmitParticles(dustParticles, 5, new Vector2(transform.position.x, transform.position.y) + new Vector2(-2, -5), Vector2.one * 3f);

        //AudioManager.instance.PlaySound2D(jumpClip); Update to FMOD
    }

    bool CanGrabLedge(int targetY, int dir)
    {
        Vector2 tileSize = GameManager.instance != null ? GameManager.instance.tileSize : Vector2.one * 16;

        return !CollisionAtPlace(new Vector2(transform.position.x + (dir * (tileSize.x / 2)), targetY + 1), solidMask) &&
            CollisionAtPlace(new Vector2(transform.position.x + (dir * (tileSize.x / 2)), targetY), solidMask);
    }

    void GrabLedge(int targetY, int dir)
    {
        facingDir = dir;
        Bounds bounds = myCollider.bounds;

        transform.position = new Vector2(transform.position.x, targetY - bounds.extents.y + 1);
        speed.y = 0f;

        while (!CheckColAtPlace(Vector2.right * dir, solidMask))
        {
            transform.position = new Vector2(transform.position.x + dir, transform.position.y);
        }

        spriteScale = new Vector2(1.3f, 0.7f);
        fsm.ChangeState(States.LedgeGrab, StateTransition.Overwrite);
    }

    public void SpringBounce()
    {
        varJumpTimer = springJumpVariableTime; // Amt of time ppl can hold the jump button to reach higher
        wallSlideTimer = wallSlideTime;
        jumpGraceTimer = 0;
        speed.x = 0;
        speed.y = springJumpSpeed;
        varJumpSpeed = speed.y;

        ProCamera2DShake.Instance.Shake("SpringJump");

        spriteScale = new Vector2(.6f, 1.4f);
    }

    public bool UseRefillDash()
    {
        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer = 0;
            return true;
        }
        return false;

        /* 
        // This version is used when you've a set amount of dashes like in celeste (Only use one version of this function)
        if (Dashes < MaxDashes)
        {
            Dashes = MaxDashes;
            return true;
        }
        return false;
        */
    }

    void UpdateSprite()
    {
        //Returns to normal state(1,1,1) and then assign it
        if (fsm.State != States.Dash)
        {
            spriteScale.x = Helper.CalculateApproach(spriteScale.x, 1f, .04f);
            spriteScale.y = Helper.CalculateApproach(spriteScale.y, 1f, .04f);
        }

        Vector3 fixedScale = new Vector3(spriteScale.x, spriteScale.y, 1);
        if (spriteHolder.localScale != fixedScale)
            spriteHolder.localScale = fixedScale;

        //Set the facing dir
        Vector3 targetDir = new Vector3(facingDir, transform.localScale.y, transform.localScale.z);
        if (transform.localScale != targetDir)
            transform.localScale = targetDir;

        if (fsm.State == States.LedgeClimb)
        {
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("LedgeClimb"))
                animator.Play("LedgeClimb");
        }
        else if (fsm.State == States.LedgeGrab)
        {
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("LedgeHang"))
                animator.Play("LedgeHang");
        }
        else if (fsm.State == States.Attack)
        {

        }
        else if (fsm.State == States.Dash)
        {
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Dash"))
                animator.Play("Dash");
        }
        else if (grounded)
        {
            if (playerInput.x == 0)
            {
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
                    animator.Play("Idle");
            }
            else
            {
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Run"))
                    animator.Play("Run");
            }
        }
        else
        {
            if (wallSlideDir != 0)
            {
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("WallSlide"))
                    animator.Play("WallSlide");
            }
            else if (speed.y > 0)
            {
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Jump"))
                    animator.Play("Jump");
            }
            else if (speed.y <= 0)
            {
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Fall"))
                    animator.Play("Fall");
            }
        }
    }

    public void Die()
    {
        speed = Vector2.zero;
        fsm.ChangeState(States.Respawn, StateTransition.Overwrite);
        if (GameManager.instance != null)
            GameManager.instance.PlayerDead();
    }
}