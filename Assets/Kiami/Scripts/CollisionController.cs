using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class CollisionController : MonoBehaviour
{
    public CollisionInfo collisions;
    public RaycastOrigins raycastOrigins;
    public Collider2D myCollider;
    Rigidbody2D rb2D;

    protected void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        rb2D = GetComponent<Rigidbody2D>();
    }

    // Checks if there is a collision on top of the actor in a given layer (specially good to check if your are on top of a oneway/fallthrough platform or going through it)
    public bool CollisionSelf(LayerMask mask)
    {
        Vector2 leftcorner = new Vector2(myCollider.bounds.center.x - myCollider.bounds.extents.x + .1f, myCollider.bounds.center.y + myCollider.bounds.extents.y - .1f);
        Vector2 rightcorner = new Vector2(myCollider.bounds.center.x + myCollider.bounds.extents.x - .1f, myCollider.bounds.center.y - myCollider.bounds.extents.y + .1f);
        return Physics2D.OverlapArea(leftcorner, rightcorner, mask);
    }

    public bool CheckColInDir(Vector2 dir, LayerMask mask)
    {
        Bounds bounds = myCollider.bounds;

        if (dir.x > 0)
        {
            raycastOrigins.topLeft = new Vector2(bounds.max.x, bounds.max.y - .1f);
            raycastOrigins.bottomRight = new Vector2(bounds.max.x + .5f, bounds.min.y + .1f);
        }
        else if (dir.x < 0)
        {
            raycastOrigins.topLeft = new Vector2(bounds.min.x - .5f, bounds.max.y - .1f);
            raycastOrigins.bottomRight = new Vector2(bounds.min.x, bounds.min.y + .1f);
        }
        else if (dir.y > 0)
        {
            raycastOrigins.topLeft = new Vector2(bounds.min.x + .1f, bounds.max.y + .5f);
            raycastOrigins.bottomRight = new Vector2(bounds.max.x - .1f, bounds.max.y);
        }
        else if (dir.y < 0)
        {
            raycastOrigins.topLeft = new Vector2(bounds.min.x + .1f, bounds.min.y);
            raycastOrigins.bottomRight = new Vector2(bounds.max.x - .1f, bounds.min.y - .5f);
        }

        return Physics2D.OverlapArea(raycastOrigins.topLeft, raycastOrigins.bottomRight, mask);
    }

    public Collider2D[] CheckColInDirAll(Vector2 dir, LayerMask mask)
    {
        Bounds bounds = myCollider.bounds;

        if (dir.x > 0)
        {
            raycastOrigins.topLeft = new Vector2(bounds.max.x, bounds.max.y - .1f);
            raycastOrigins.bottomRight = new Vector2(bounds.max.x + .5f, bounds.min.y + .1f);
        }
        else if (dir.x < 0)
        {
            raycastOrigins.topLeft = new Vector2(bounds.min.x - .5f, bounds.max.y - .1f);
            raycastOrigins.bottomRight = new Vector2(bounds.min.x, bounds.min.y + .1f);
        }
        else if (dir.y > 0)
        {
            raycastOrigins.topLeft = new Vector2(bounds.min.x + .1f, bounds.max.y + .5f);
            raycastOrigins.bottomRight = new Vector2(bounds.max.x - .1f, bounds.max.y);
        }
        else if (dir.y < 0)
        {
            raycastOrigins.topLeft = new Vector2(bounds.min.x + .1f, bounds.min.y);
            raycastOrigins.bottomRight = new Vector2(bounds.max.x - .1f, bounds.min.y - .5f);
        }

        return Physics2D.OverlapAreaAll(raycastOrigins.topLeft, raycastOrigins.bottomRight, mask);
    }

    public bool WallJumpCheck(int dir,int checkDst,LayerMask mask)
    {
        Bounds bounds = myCollider.bounds;

        if (dir == -1)
        {
            raycastOrigins.topLeft = new Vector2(bounds.min.x - .5f-checkDst, bounds.max.y - .1f);
            raycastOrigins.bottomRight = new Vector2(bounds.min.x, bounds.min.y + .1f);
        }
        else if (dir == 1)
        {
            raycastOrigins.topLeft = new Vector2(bounds.max.x, bounds.max.y - .1f);
            raycastOrigins.bottomRight = new Vector2(bounds.max.x + .5f+checkDst, bounds.min.y + .1f);
        }

        return Physics2D.OverlapArea(raycastOrigins.topLeft, raycastOrigins.bottomRight, mask);
    }

    public bool CheckColAtPlaceOld(Vector2 offset, LayerMask mask)
    {
        Bounds bounds = myCollider.bounds;

        raycastOrigins.topLeft = new Vector2(bounds.min.x + .1f, bounds.max.y - .1f) + offset;
        raycastOrigins.bottomRight = new Vector2(bounds.max.x - .1f, bounds.min.y + .1f) + offset;

        return Physics2D.OverlapArea(raycastOrigins.topLeft, raycastOrigins.bottomRight,mask);
    }

    //Check if there's any collision within a given layer in a set direction(Like a hitbox)
    public bool CheckColAtPlace(Vector2 offset, LayerMask mask)
    {
        bool result = false;
        Bounds bounds = myCollider.bounds;

        raycastOrigins.topLeft = new Vector2(bounds.min.x + .1f, bounds.max.y - .1f) + offset;
        raycastOrigins.bottomRight = new Vector2(bounds.max.x - .1f, bounds.min.y + .1f) + offset;

        if (Physics2D.OverlapArea(raycastOrigins.topLeft, raycastOrigins.bottomRight, mask))
        {
            Collider2D[] collisions = Physics2D.OverlapAreaAll(raycastOrigins.topLeft, raycastOrigins.bottomRight, mask);

            if (collisions.Length > 0)
            {
                foreach (Collider2D coll in collisions)
                {
                    if (coll != myCollider)
                    {
                        result = true;
                        break;
                    }
                }
            }
        }

        return result;
    }

    //Checks all there's any collision within a given layer in a set direction(Like a hitbox)
    public Collider2D[] CheckColAtPlaceAll(Vector2 offset, LayerMask mask)
    {
        Bounds bounds = myCollider.bounds;

        raycastOrigins.topLeft = new Vector2(bounds.min.x + .1f, bounds.max.y - .1f) + offset;
        raycastOrigins.bottomRight = new Vector2(bounds.max.x - .1f, bounds.min.y + .1f) + offset;

        return Physics2D.OverlapAreaAll(raycastOrigins.topLeft, raycastOrigins.bottomRight,mask);
    }

    //Checks if there's a collision on top of the actor in a given layer (specially good to check if you are on top a oneway/fallthrough platform or going through it)
    public bool CollisionAtPlace(Vector2 pos, LayerMask mask)
    {
        Vector2 topLeftCorner = new Vector2(pos.x - 1, pos.y + 1);
        Vector2 bottomRightCorner = new Vector2(pos.x + 1, pos.y - 1);

        return Physics2D.OverlapArea(topLeftCorner, bottomRightCorner,mask);
    }

    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;
    }

    public struct RaycastOrigins
    {
        public Vector2 topLeft, topRight;
        public Vector2 bottomLeft, bottomRight;
    }
}