using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameplaySettings", menuName = "GameplaySettings")]
public class GameplaySettings : ScriptableObject {
    public int GameID { get; set; }

    // Sync game settings, set at runtime before launching into a sync match. Types from ConnectArgs
    public string syncUrl { get; set; }
    public uint syncPort { get; set; }
    public string syncMatchId { get; set; }
    public string syncMatchToken { get; set; }
    public long currentPlayerId { get; set; }
    public int tickRateMS { get; set;}
    public string currentPlayerAvatarUrl { get; set; }
    public string opponentAvatarUrl { get; set; }
    public string currentPlayerName { get; set; }
    public string opponentName { get; set; }
    public bool syncGameOver { get; set; }
}