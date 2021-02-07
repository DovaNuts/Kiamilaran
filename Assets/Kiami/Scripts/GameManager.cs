using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    [Header("Target frame rate")]
    public bool vsync;
    public int fpsLimit = 60;

    [Header("Tile size")]
    public Vector2 tileSize;

    [Header("Temporary spawn point")]
    public Transform spawnPoint;

    [Header("Events")]
    public UnityEvent OnPlayerDeadEvents;
    public UnityEvent OnPlayerRespawnEvents; //Called when exiting Respawn state
    public UnityEvent NextLevelEvents;

    public static GameManager instance;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);

        if (!vsync)
        {
            QualitySettings.vSyncCount = 0;
            //Application.targetFrameRate = fpsLimit;
        }
        else
            QualitySettings.vSyncCount = 1;
    }

    public void PlayerDead()
    {
        Debug.Log("Player dead");
        OnPlayerDeadEvents?.Invoke();
    }

    public void PlayerRespawn()
    {
        Debug.Log("Player respawn");
        OnPlayerRespawnEvents?.Invoke();
    }

    public void NextLevel()
    {
        Debug.Log("Next level");
        NextLevelEvents?.Invoke();
    }

    public void EmitParticles(GameObject particlePrefab,int amount,Vector2 pos,Vector2 posRange)
    {
        for (int i = 0; i < amount; i++)
            EmitParticles(particlePrefab, new Vector2(Random.Range(pos.x - posRange.x, pos.x + posRange.x),
                Random.Range(pos.y - posRange.y, pos.y + posRange.y)));

    }

    public void EmitParticles(GameObject particlePrefab, Vector2 pos) => Instantiate(particlePrefab, pos, Quaternion.identity);

    public void ResetFallingPlatforms()
    {
        FallingPlatform[] platforms = FindObjectsOfType<FallingPlatform>();

        foreach (FallingPlatform platform in platforms)
        {
            if (platform.enabled)
                platform.ResetPlatform();
        }
    }
}