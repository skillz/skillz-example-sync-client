using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : SkillzMatchDelegate {

    public void OnMatchWillBegin(SkillzSDK.Match matchInfo) {
        if (matchInfo.IsCustomSynchronousMatch) {
            Debug.Log("Sync Game Mode Starting...");
            SetSyncMatchSettings(matchInfo);
            Debug.Log("SetSyncMatchSettings complete...");
            if (UserData.Instance != null)
            {
                Debug.Log("IsGameOver = false complete...");
                UserData.Instance.IsGameOver = false;
            }
            Debug.Log("Loading SyncScene synchronously...");
            SceneManager.LoadScene("SyncScene", LoadSceneMode.Single);
        } else {
            ClearSyncSettings(); // Clear any settings from a previous match for good measure
            SceneManager.LoadScene("MainScene", LoadSceneMode.Single);
        }
    }

    public void OnSkillzWillExit() {
        // Do nothing. Loading the initial scene again causes problems with concurrency as the SDK handles relaunching the activity for us
    }

    private void ClearSyncSettings() {
        UserData.Instance.SyncUrl = null;
        UserData.Instance.SyncPort = 0;
        UserData.Instance.SyncMatchToken = null;
        UserData.Instance.SyncMatchId = null;
        UserData.Instance.CurrentPlayerId = 0;
    }

    private void SetSyncMatchSettings(SkillzSDK.Match matchInfo) {
        SkillzSDK.CustomServerConnectionInfo info = matchInfo.CustomServerConnectionInfo;
        UserData.Instance.SyncUrl = info.ServerIp;
        UserData.Instance.SyncPort = (uint)int.Parse(info.ServerPort);
        UserData.Instance.SyncMatchToken = info.MatchToken;
        UserData.Instance.SyncMatchId = info.MatchId;
        UserData.Instance.IsGameOver = false;
        foreach (SkillzSDK.Player player in matchInfo.Players) {
            if (player.IsCurrentPlayer) {
                UserData.Instance.CurrentPlayerId = (long)player.ID;
                UserData.Instance.CurrentPlayerAvatarUrl = player.AvatarURL;
                UserData.Instance.CurrentPlayerName = player.DisplayName;
            } else {
                UserData.Instance.OpponentAvatarUrl = player.AvatarURL;
                UserData.Instance.OpponentName = player.DisplayName;
            }
        }
    }
}