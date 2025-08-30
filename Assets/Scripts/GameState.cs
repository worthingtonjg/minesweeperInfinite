using UnityEngine;
using System;

public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }

    public int startingLives = 1;
    public int Lives;
    public bool IsDead;

    public event Action<int> OnLivesChanged;
    public event Action OnDeath;

    // Ensure only one permanent GameState exists
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);   // kill duplicates
            return;
        }

        Instance = this;
        Lives = startingLives;

        DontDestroyOnLoad(gameObject); // <- once in DontDestroyOnLoad scene, it will never be destroyed
    }

    public void KillPlayer()
    {
        if (IsDead) return;
        GameObject.Find("GameUIController").GetComponent<GameUI>().ShowRestartGame();
        Lives = 0;
        IsDead = true;
        OnLivesChanged?.Invoke(Lives);
        OnDeath?.Invoke();
        Debug.Log("💥 Player died.");
    }

    public void ResetRun(int? newStartingLives = null)
    {
        GameObject.Find("GameUIController").GetComponent<GameUI>().HideRestartGame();

        Lives = newStartingLives ?? startingLives;
        IsDead = false;
        OnLivesChanged?.Invoke(Lives);
    }
}
