using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Actor : CollisionController
{
    public bool grounded = true;
    [Tooltip("Same as grounded but stores the frame before the current one")]
    public bool wasGrounded = true;

    public Vector2 speed;
    public Vector2 movementCounter = Vector2.zero; //Counter to store non int movement values

    public LayerMask solidMask;
    public LayerMask oneWayMask;
    public LayerMask ladderMask;

    //Rounds the movement and moves the player
    public bool MoveH(float moveH)
    {
        movementCounter.x += moveH;
        //Rounds x.5 down and x.6 up
        int roundedMov = (int)Mathf.Round(movementCounter.x);

        //If roundedMovement is not 0 then substract roundedMov and send it
        //Ex = 5.5 | rounded = 5 | movementCounter = 5.5 - 5
        if (roundedMov != 0)
        {
            movementCounter.x  -= roundedMov;
            return MoveHExact(roundedMov);
        }

        return false;
    }

    public bool MoveV(float moveV)
    {
        movementCounter.y += moveV;
        //Rounds x.5 down and x.6 up
        int roundedMov = (int)Mathf.Round(movementCounter.y);

        //If roundedMovement is not 0 then substract roundedMov and send it
        //Ex = 5.5 | rounded = 5 | movementCounter = 5.5 - 5
        if (roundedMov != 0)
        {
            movementCounter.y -= roundedMov;
            return MoveVExact(roundedMov);
        }

        return false;
    }

    public virtual bool MoveHExact(int moveH)
    {
        int direction = (int)Mathf.Sign(moveH);

        //Moves the actor until he collides with something or move amount reaches 0
        while (moveH != 0)
        {
            //Common sidesways collision   
            bool solid = CheckColAtPlace(Vector2.right * direction, solidMask);

            //If colliding with something
            if (solid)
            {
                movementCounter.x = 0;
                return true;
            }
            moveH -= direction;
            transform.position = new Vector2(transform.position.x + direction, transform.position.y);
        }
        return false;
    }

    public virtual bool MoveVExact(int moveV)
    {
        int direction = (int)Mathf.Sign(moveV);

        //Moves the actor until he collides with something or move amount reaches 0
        while (moveV != 0)
        {
            bool solid = direction > 0 ? CheckColAtPlace(Vector2.up * direction, solidMask) : IsGrounded();
            //If colliding with something
            if (solid)
            {
                movementCounter.y = 0;
                return true;
            }
            moveV -= direction;
            transform.position = new Vector2(transform.position.x, transform.position.y + direction);
        }
        return false;
    }

    public virtual bool IsGrounded()
    {
        // checking if there is a collision on the bottom with the solid layer or if there is a collision with the oneway layer and you are not already collisioning with it
        return CheckColAtPlace(Vector2.down, solidMask) || (CheckColAtPlace(Vector2.down, oneWayMask) && !CollisionSelf(oneWayMask));
    }
}