using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class WaveManager : NetworkBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("��������")]
    public int currentWave = 0;
    public int enemiesPerWave = 5;
    public float waveMultiplier = 1.2f;
    public float timeBetweenWaves = 10f;

    [Header("״̬")]
    [SyncVar] public WaveState currentState = WaveState.Preparing;
    [SyncVar] public int aliveEnemies = 0;
    [SyncVar] public float waveTimer = 0f;

    // ���׼��״̬������������
    private Dictionary<NetworkConnection, bool> playerReadyStates = new Dictionary<NetworkConnection, bool>();
    private List<PlayerExperience> playersWithUpgrades = new List<PlayerExperience>();

    // �¼�
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
        
        // ��ʼ���ɵ���
        SpawnEnemies();
        currentState = WaveState.Active;
    }

    private IEnumerator HandleActiveState()
    {
        // �ȴ����е��˱�����
        while (aliveEnemies > 0)
        {
            yield return null;
        }
        
        currentState = WaveState.Complete;
    }

    private IEnumerator HandleCompleteState()
    {
        RpcOnWaveComplete(currentWave);
        
        // ����Ƿ����������
        CheckForPlayerUpgrades();
        
        if (playersWithUpgrades.Count > 0 && NetworkClient.active)
        {
            // ����ģʽ���ȴ��������ѡ������
            currentState = WaveState.WaitingForPlayers;
        }
        else
        {
            // ����ģʽ����������ֱ�ӽ�����һ��
            currentState = WaveState.Preparing;
        }
        
        yield return null;
    }

    private IEnumerator HandleWaitingState()
    {
        // �ȴ��������׼����
        while (!AreAllPlayersReady())
        {
            yield return null;
        }
        
        // ����׼��״̬
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
        // ����Ļ��Ե�������λ��
        float x = UnityEngine.Random.Range(-10f, 10f);
        float y = UnityEngine.Random.Range(-10f, 10f);
        return new Vector3(x, y, 0);
    }

    [Server]
    public void OnEnemyDeath(Vector3 deathPosition)
    {
        aliveEnemies--;
        
        // ���ɾ�����
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
        // ��ʾ����ѡ��UI
        // ������Դ���UI��ʾ
    }
}

public enum WaveState
{
    Preparing,
    Active,
    Complete,
    WaitingForPlayers
}