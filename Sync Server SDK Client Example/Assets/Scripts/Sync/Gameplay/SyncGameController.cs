using UnityEngine;
using TMPro;
using Servers;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using FlatBuffers;
using System;
using System.Collections;
using System.Threading;

public sealed class SyncGameController : MonoBehaviour
{
    public static SyncGameController Instance { private set; get; }

    [SerializeField]
    private SyncClient client;

    [SerializeField]
    private MatchInfoDisplay matchInfoDisplay;
    public int tickCount = 0;

    private bool doAbort = false;
    private bool didConnectSuccessfully = false;
    private bool doAttemptConnect = false;
    
    private void Awake()
    {
        Debug.Log("SyncGameController: Awake");
        tickCount = 0;
        Time.timeScale = 1f;
        if (Instance != null)
        {
            Destroy(Instance);
        }
        Instance = this;
        if (UserData.Instance != null)
        {
            UserData.Instance.IsGameOver = false;
            UserData.Instance.IsGameStarted = false;
            UserData.Instance.IsUnityAppPaused = false;
        }

        if (!Application.isEditor)
        {
            Debug.Log("Disabling Debug Logging in non-editor build...");
            Debug.unityLogger.logEnabled = false;
        }
    }

    private void Start()
    {
        Debug.Log("SyncGameController: Start");
		Time.timeScale = 1f;
        tickCount = 0;
        InvokeRepeating("KeepAlive", 0.0f, 1.0f); // send keep alive every second
        UserData.Instance.IsGameOver = false;

        matchInfoDisplay.GameState = "Connecting";
    }

    private void KeepAlive()
    {
        if (client.IsConnected)
        {
            client.SendKeepAlive();
        }
    }

    void OnApplicationFocus(bool focused)
    {
        // This check ensures that we don't send a PauseMessage when running in editor or on a desktop build for development
        if (!Application.isEditor && Application.platform != RuntimePlatform.WindowsPlayer && Application.platform != RuntimePlatform.OSXPlayer)
        {
            UserData.Instance.IsUnityAppPaused = !focused;

            if (!focused)
            {
                client.StopReadTimer();
                client.SendAppPaused();
            }
            else
            {
                client.ResetReadTimer();
                client.SendAppResumed();
            }
            Debug.Log("OnApplicationFocus, focused: " + focused);
        }
    }

    private void Update()
    {
        if (doAbort)
        {
            doAbort = false;
            AbortGame();
        }
        if (didConnectSuccessfully)
        {
            didConnectSuccessfully = false;
            OnConnected();
        }
        if (doAttemptConnect)
        {
            doAttemptConnect = false;
            OnAttemptingReconnect();
        }
            
        if (!client.IsConnected && tickCount > 0 && !UserData.Instance.IsGameOver)
        {
            matchInfoDisplay.GameState = "Disconnected";
        }

        // Process all packets that have been received by the server thus far
        byte[] data;
        while (client.GetNextPacket(out data))
        {
            if (UserData.Instance.IsGameOver)
            {
                return;
            }

            var packet = PacketFactory.BytesToPacket(data);
            var byteBuffer = new ByteBuffer(data);

            // Uncomment for logs containing which packet type you receive
            // Debug.Log("SyncGameController: Received packet: " + (Opcode)packet.Opcode);
            switch ((Opcode)packet.Opcode)
            {
                case Opcode.MatchSuccess:
                    client.ResetReadTimer();
                    on(MatchSuccess.GetRootAsMatchSuccess(byteBuffer));

                    client.SetReadTimeout(2000);
                    break;

                case Opcode.GameState:
                    on(GameState.GetRootAsGameState(byteBuffer));
                    break;

                case Opcode.MatchOver:
                    on(MatchOver.GetRootAsMatchOver(byteBuffer));
                    break;

                case Opcode.OpponentConnectionStatus:
                    on(OpponentConnectionStatus.GetRootAsOpponentConnectionStatus(byteBuffer));
                    break;

                case Opcode.PlayerReconnected:
                    on(PlayerReconnected.GetRootAsPlayerReconnected(byteBuffer));
                    break;

                case Opcode.OpponentPaused:
                    on(OpponentPaused.GetRootAsOpponentPaused(byteBuffer));
                    break;

                case Opcode.OpponentResumed:
                    on(OpponentResumed.GetRootAsOpponentResumed(byteBuffer));
                    break;

                case Opcode.Chat:
                    Debug.Log("Chat receigedf");
                    on(Chat.GetRootAsChat(byteBuffer));
                    break;

                default:
                    Debug.Log("SyncGameController: Received packet with unimplemented/unsupported authcode: " + packet.Opcode);
                    break;
            }
        }
    }

    private void on(MatchSuccess message)
    {
        Debug.Log("MatchSuccess received, OpponentId " + message.OpponentUserId);

        matchInfoDisplay.TickRate = message.TickRate;
        matchInfoDisplay.UserId = UserData.Instance.CurrentPlayerId.ToString();
        matchInfoDisplay.OpponentId = message.OpponentUserId.ToString();
        matchInfoDisplay.GameState = "Playing";
    }

    private void on(GameState message)
    {
        tickCount = (int)message.TickCount;
        
        matchInfoDisplay.PlayerScore = message.PlayerScore;
        matchInfoDisplay.OpponentScore = message.OpponentScore;
        matchInfoDisplay.CurrentGameTick = message.GameTickCount;
        matchInfoDisplay.CurrentTick = message.TickCount;
    }

    private void on(Chat message)
    {
        Debug.Log("Chat message received!");
        ChatManager.Instance.ShowPlayerChat(message.ChatId, false);
    }

    private void on(OpponentConnectionStatus message)
    {
        Debug.Log("Opponent Disconnected, status: " + message.Status + " time remaining: " + message.TimeRemaining);

        matchInfoDisplay.GameState = "Paused by " + matchInfoDisplay.OpponentId + ", " + message.TimeRemaining + "s left...";
        matchInfoDisplay.SetInputAllowed(false);
    }

    private void on(PlayerReconnected message)
    {
        Debug.Log("Player reconnected: " + message.PlayerReconnectedUserId);

        matchInfoDisplay.GameState = "Playing";
        matchInfoDisplay.SetInputAllowed(true);

    }

    private void on(OpponentPaused message)
    { 
        Debug.Log("Opponent Paused, time remaining: " + message.TimeRemaining);

        matchInfoDisplay.GameState = "Paused by " + matchInfoDisplay.OpponentId + ", " + message.TimeRemaining + "s left...";
        matchInfoDisplay.SetInputAllowed(false);
    }

    private void on(OpponentResumed message)
    {
        Debug.Log("Opponent Resumed");

        matchInfoDisplay.GameState = "Playing";
        matchInfoDisplay.SetInputAllowed(true);
    }

    private void on(MatchOver message)
    {
        Debug.Log("SyncGameController: On MatchOver");

        UserData.Instance.InPause = true;
        UserData.Instance.IsGameOver = true;

        ChatManager.Instance.SetChatEnabled(false);

        matchInfoDisplay.PlayerScore = message.PlayerScore;

        string winner = matchInfoDisplay.PlayerScore > matchInfoDisplay.OpponentScore ?
        "You Won!" :
        "Opponent Won!";

        matchInfoDisplay.GameState = "Game Over! " + winner;
        matchInfoDisplay.SetInputAllowed(false);

        client.Disconnect(true);
    }

    public void ForfeitMatch()
    {
        Debug.Log("SyncGameController: ForfeitMatch");
    }

    public void SendScoreAdjust(int scoreDifference) 
    {
        client.SendPlayerInput(scoreDifference);
    }

    private void OnAttemptingReconnect()
    {
        if (UserData.Instance.IsGameOver || client.IsConnected)
        {
            return;
        }
        matchInfoDisplay.GameState = "Attempting Reconnect";
    }
    
    public void OnConnected()
    {
        Debug.Log("SyncGameController: Connected");
        matchInfoDisplay.GameState = "Connected, waiting for player";
    }
    public void ForfeitGame()
    {
        Debug.Log("Forfeiting game...");
        client.SendForfeitMatch();
    }

    public void AbortGame()
    {
        Debug.Log("Aborting game...");
        tickCount = 0;
        client.Disconnect(true);
        SceneManager.LoadScene("AbortScene", LoadSceneMode.Single);
        UserData.Instance.IsGameOver = true;
    }

    void OnApplicationQuit()
    {
        Debug.Log("Disposing SyncClient on app quit...");
        client.Reset();
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    public void AttemptReconnectOnNextUpdate()
    {
        doAttemptConnect = true;
    }

    public void DidConnectSuccessfully()
    {
        didConnectSuccessfully = true;
    }

    public void AbortOnNextUpdate()
    {
        doAbort = true;
    }

    public void SendChatForId(int chatId) {
        client.SendChatMessage(chatId);
    }
}