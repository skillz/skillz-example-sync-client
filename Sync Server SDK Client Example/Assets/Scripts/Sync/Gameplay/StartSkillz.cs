using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartSkillz : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        SkillzCrossPlatform.LaunchSkillz(new GameController());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
