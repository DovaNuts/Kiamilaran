using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Com.LuisPedroFonseca.ProCamera2D;

public class Health : MonoBehaviour
{
    public UnityEvent OnTakeDamageEvent;
    public UnityEvent OnTakeHealEvent;
    public UnityEvent OnDeathEvent;
    public float dieEventsAfterTime = 1f;

    [Header("Max/Starting health")]
    public int maxHealth;
    [Header("Current health")]
    public int health;

    [Header("Is death")]
    public bool dead = false;

    [Header("Other")]
    public bool invincible = false;
    public bool becomeInvincibleOnHit = false;
    public float invincibleTimeOnHit = .5f;
    float invincibleTime;

    void Start()
    {
        health = maxHealth;
    }

    void Update()
    {
        if (Time.time >= invincibleTime)
        {
            if (invincible)
                invincible = false;
        }
    }

    public bool TakeDamage(int amt)
    {
        if (dead || invincible)
            return false;

        health = Mathf.Max(0, health - amt);

        OnTakeDamageEvent?.Invoke();

        if (health <= 0)
            Die();
        else
        {
            if (becomeInvincibleOnHit)
            {
                invincible = true;
                invincibleTime = Time.time + invincibleTimeOnHit;
            }

            //Add shake
            ProCamera2DShake.Instance.Shake("PlayerHit");
        }

        //SFX
        //AudioManager.instance.PlaySound2D("Impact"); Update to FMOD

        return true;
    }

    public bool TakeHeal(int amt)
    {
        if (dead || health == maxHealth)
            return false;

        health = Mathf.Min(maxHealth, health + amt);

        OnTakeHealEvent?.Invoke();
        //Add shake

        //SFX
        //AudioManager.instance.PlaySound2D("Heal"); Update to FMOD
        return true;
    }

    public void Die()
    {
        dead = true;
        //Add shaking
        StartCoroutine(DeathEventsRoutine(dieEventsAfterTime));
    }

    IEnumerator DeathEventsRoutine(float time)
    {
        yield return new WaitForSeconds(time);
        OnDeathEvent?.Invoke();
    }
}