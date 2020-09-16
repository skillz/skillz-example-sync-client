using UnityEngine;
using System.Collections.Generic;
using TileId = System.Int32;

public class UserData : MonoBehaviour {
    public static UserData Instance { private set; get; }
    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(this);
	}
	private int LastTick = -1;
    public int Level;
	public int FinalScore;
    public bool InPause = false;
	public bool IsGameOver = false;
	public bool IsGameStarted = false;
	public bool IsUnityAppPaused = false;

    public string SyncUrl;
    public uint SyncPort;
    public string SyncMatchId;
    public string SyncMatchToken;
    public long CurrentPlayerId;
    public string CurrentPlayerName;
    public string CurrentPlayerAvatarUrl;
    public string OpponentName;
    public string OpponentAvatarUrl;


	public void OnStartGame() {
		
	}
}