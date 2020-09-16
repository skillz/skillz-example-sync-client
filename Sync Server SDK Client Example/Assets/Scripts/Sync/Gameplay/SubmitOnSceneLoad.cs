using System.Collections;
using System.Collections.Generic;
using Servers;
using UnityEngine;

public class SubmitOnSceneLoad : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    private SyncClient client;
    private bool didSubmit = false;
    
    void LateUpdate()
    {
        if (didSubmit)
        {
            return;
        }
        didSubmit = true;

        client.Reset();

         if (UserData.Instance != null)
        {
            UserData.Instance.IsGameOver = false;
            UserData.Instance.IsGameStarted = false;
            UserData.Instance.IsUnityAppPaused = false;
        }
        Debug.Log("Submitting score: " + client.Score);
        SkillzCrossPlatform.ReportFinalScore(client.Score);
    }
}
