using System.Collections;
using UnityEngine;

public class FallingPlatform : CollisionController
{
    public bool isTriggered = false;
    public float timeBeforeFalling = 0.5f;
    public float destroyAfterTime = 0.5f;

    public Animator animator;
   
   new void Awake()
    {
        base.Awake();
    }

    void Update()
    {
        if (!isTriggered)
        {
            //Use use col at place old because the method fits with what we need
            if (CheckColAtPlaceOld(Vector2.up,1))
            {
                Collider2D[] collisions = CheckColInDirAll(Vector2.up, 1);

                foreach (Collider2D collision in collisions)
                {
                    if (collision.GetComponent<Actor>()!=null && collision.GetComponent<Actor>().grounded)
                    {
                        isTriggered = true;
                        StartFalling();
                        break;
                    }
                }
            }
        }   
    }

    [ContextMenu("Start falling")]
    void StartFalling()
    {
        StartCoroutine(FallRoutine());
    }

    IEnumerator FallRoutine()
    {
        yield return new WaitForSeconds(timeBeforeFalling);

        if (animator != null)
            animator.Play("Fall");
        if (myCollider.enabled)
            myCollider.enabled = false;

        yield return new WaitForSeconds(destroyAfterTime);
    }

    [ContextMenu("Reset platform")]
    public void ResetPlatform()
    {
        isTriggered = false;

        if (animator != null)
            animator.Play("Idle");
        if (!myCollider.enabled)
            myCollider.enabled = true;
    }
}