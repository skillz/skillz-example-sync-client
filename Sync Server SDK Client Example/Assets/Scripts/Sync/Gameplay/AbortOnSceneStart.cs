using System.Collections;
using System.Collections.Generic;
using Servers;
using UnityEngine;

public class AbortOnSceneStart : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    private SyncClient client;
    private bool didAbort = false;
    
    void LateUpdate()
    {
        if (didAbort)
        {
            return;
        }
        didAbort = true;
        client.Reset();
        SkillzCrossPlatform.AbortMatch();
    }
}
