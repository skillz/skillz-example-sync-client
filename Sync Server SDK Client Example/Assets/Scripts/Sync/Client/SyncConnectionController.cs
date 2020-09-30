using System;
using System.Collections;
using Servers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class SyncConnectionController : MonoBehaviour
{
    [SerializeField]
    private ConnectArgs connectionArgs;

    [SerializeField]
    private SyncClient client;

    private void Awake()
    {
        Debug.Log("SyncConnectionController: Awake");
    }

    private void OnDestroy()
    {
    }

    private void Start()
    {
        Debug.Log("SyncConnectionController: Start");
        Time.timeScale = 1f;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        if (UserData.Instance.IsGameOver)
        {
            Debug.Log("Game is over at start of ConnectionController...");
            return;
        }

        Debug.Log("Showing connecting dialog...");
        Debug.Log("Starting connection coroutine...");
        StartCoroutine(ConnectRoutine());
    }

    private IEnumerator ConnectRoutine()
    {
        Debug.Log("Waiting 1 second then initiating connection...");
        yield return new WaitForSecondsRealtime(1.0f);
        Debug.Log("Initiating connection...");
        client.Connect(GetConnectionInfo());
        yield break;
    }

    private void OnInvalidServer(object sender, EventArgs args)
    {
        if (UserData.Instance.IsGameOver)
        {
            return;
        }
    }

    private void OnServerHandshakeFailed(object sender, EventArgs args)
    {
        if (UserData.Instance.IsGameOver || client.IsConnected)
        {
            return;
        }
    }

    private void OnCouldNotOpenSocket(object sender, EventArgs args)
    {
        if (UserData.Instance.IsGameOver || client.IsConnected)
        {
            return;
        }
    }

    private void OnCouldNotOpenSslStream(object sender, EventArgs args)
    {
        if (UserData.Instance.IsGameOver || client.IsConnected)
        {
            return;
        }
    }

    private void OnUnknownError(object sender, EventArgs args)
    {
        if (UserData.Instance.IsGameOver)
        {
            return;
        }
    }
    public ServerConnectionInfo GetConnectionInfo() {
        long playerId = Application.isEditor ? 111111 : UserData.Instance.CurrentPlayerId;
        playerId = playerId == 0 ? 222222 : playerId;

        UserData.Instance.SyncUrl = connectionArgs.Url;
        UserData.Instance.SyncPort = connectionArgs.Port;
        UserData.Instance.SyncMatchId = connectionArgs.MatchId;
        UserData.Instance.SyncMatchToken = connectionArgs.MatchToken;
        UserData.Instance.CurrentPlayerId = playerId;

        return new ServerConnectionInfo(
            UserData.Instance.SyncUrl,
            UserData.Instance.SyncPort,
            UserData.Instance.SyncMatchId,
            UserData.Instance.SyncMatchToken,
            UserData.Instance.CurrentPlayerId
        );
    }
}

