using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class WaveManager : NetworkBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("波次设置")]
    public int currentWave = 0;
    public int enemiesPerWave = 5;
    public float waveMultiplier = 1.2f;
    public float timeBetweenWaves = 10f;

    [Header("状态")]
    [SyncVar] public WaveState currentState = WaveState.Preparing;
    [SyncVar] public int aliveEnemies = 0;
    [SyncVar] public float waveTimer = 0f;

    // 玩家准备状态（用于联机）
    private Dictionary<NetworkConnection, bool> playerReadyStates = new Dictionary<NetworkConnection, bool>();
    private List<PlayerExperience> playersWithUpgrades = new List<PlayerExperience>();

    // 事件
    public static event Action<int> OnWaveStart;
    public static event Action<int> OnWaveComplete;
    public static event Action OnAllPlayersReady;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (isServer)
        {
            StartCoroutine(WaveManagerCoroutine());
        }
    }

    private IEnumerator WaveManagerCoroutine()
    {
        while (true)
        {
            switch (currentState)
            {
                case WaveState.Preparing:
                    yield return StartCoroutine(HandlePreparingState());
                    break;
                    
                case WaveState.Active:
                    yield return StartCoroutine(HandleActiveState());
                    break;
                    
                case WaveState.Complete:
                    yield return StartCoroutine(HandleCompleteState());
                    break;
                    
                case WaveState.WaitingForPlayers:
                    yield return StartCoroutine(HandleWaitingState());
                    break;
            }
            
            yield return null;
        }
    }

    private IEnumerator HandlePreparingState()
    {
        currentWave++;
        waveTimer = timeBetweenWaves;
        
        RpcOnWaveStart(currentWave);
        
        while (waveTimer > 0)
        {
            waveTimer -= Time.deltaTime;
            yield return null;
        }
        
        // 开始生成敌人
        SpawnEnemies();
        currentState = WaveState.Active;
    }

    private IEnumerator HandleActiveState()
    {
        // 等待所有敌人被击败
        while (aliveEnemies > 0)
        {
            yield return null;
        }
        
        currentState = WaveState.Complete;
    }

    private IEnumerator HandleCompleteState()
    {
        RpcOnWaveComplete(currentWave);
        
        // 检查是否有玩家升级
        CheckForPlayerUpgrades();
        
        if (playersWithUpgrades.Count > 0 && NetworkClient.active)
        {
            // 联机模式：等待所有玩家选择升级
            currentState = WaveState.WaitingForPlayers;
        }
        else
        {
            // 离线模式或无升级：直接进入下一波
            currentState = WaveState.Preparing;
        }
        
        yield return null;
    }

    private IEnumerator HandleWaitingState()
    {
        // 等待所有玩家准备好
        while (!AreAllPlayersReady())
        {
            yield return null;
        }
        
        // 清理准备状态
        ClearPlayerReadyStates();
        playersWithUpgrades.Clear();
        
        currentState = WaveState.Preparing;
    }

    [Server]
    private void SpawnEnemies()
    {
        int enemiesToSpawn = Mathf.RoundToInt(enemiesPerWave * Mathf.Pow(waveMultiplier, currentWave - 1));
        aliveEnemies = enemiesToSpawn;
        
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            StartCoroutine(SpawnEnemyWithDelay(i * 0.2f));
        }
    }

    private IEnumerator SpawnEnemyWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        Vector3 spawnPosition = GetRandomSpawnPosition();
        GameObject enemy = ObjectPoolManager.Instance.Spawn("EnemyOnline", spawnPosition, Quaternion.identity);
    }

    private Vector3 GetRandomSpawnPosition()
    {
        // 在屏幕边缘随机生成位置
        float x = UnityEngine.Random.Range(-10f, 10f);
        float y = UnityEngine.Random.Range(-10f, 10f);
        return new Vector3(x, y, 0);
    }

    [Server]
    public void OnEnemyDeath(Vector3 deathPosition)
    {
        aliveEnemies--;
        
        // 生成经验球
        SpawnExpBall(deathPosition);
    }

    [Server]
    private void SpawnExpBall(Vector3 position)
    {
        GameObject expBall = ObjectPoolManager.Instance.Spawn("ExpBallOnline", position, Quaternion.identity);
    }

    private void CheckForPlayerUpgrades()
    {
        playersWithUpgrades.Clear();
        
        foreach (var player in FindObjectsOfType<PlayerExperience>())
        {
            if (player.HasPendingUpgrade())
            {
                playersWithUpgrades.Add(player);
            }
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdPlayerReady(NetworkConnectionToClient sender = null)
    {
        playerReadyStates[sender] = true;
        
        if (AreAllPlayersReady())
        {
            RpcAllPlayersReady();
        }
    }

    private bool AreAllPlayersReady()
    {
        var activePlayers = NetworkServer.connections.Values;
        
        foreach (var conn in activePlayers)
        {
            if (!playerReadyStates.ContainsKey(conn) || !playerReadyStates[conn])
            {
                return false;
            }
        }
        
        return true;
    }

    private void ClearPlayerReadyStates()
    {
        playerReadyStates.Clear();
    }

    [ClientRpc]
    private void RpcOnWaveStart(int wave)
    {
        OnWaveStart?.Invoke(wave);
    }

    [ClientRpc]
    private void RpcOnWaveComplete(int wave)
    {
        OnWaveComplete?.Invoke(wave);
    }

    [ClientRpc]
    private void RpcAllPlayersReady()
    {
        OnAllPlayersReady?.Invoke();
    }

    public void OnPlayerLevelUp()
    {
        // 显示升级选择UI
        // 这里可以触发UI显示
    }
}

public enum WaveState
{
    Preparing,
    Active,
    Complete,
    WaitingForPlayers
}