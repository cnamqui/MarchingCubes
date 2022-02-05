using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] GameObject Player;
    // Start is called before the first frame update
    void Start()
    {
        var settings = ChunkManager.Instance.settings;
        var sd = SettingsManager.Load();
        var tpc = this.Player.GetComponent<ThirdPersonController>();
        tpc.MoveSpeed = sd.playerSettings.moveSpeed;
        tpc.SprintSpeed = sd.playerSettings.sprintSpeed;
        tpc.JumpHeight = sd.playerSettings.jumpHeight;
        var maxHeight = settings.maxOverworldHeight * settings.chunkScale + settings.overworldStartAt;
        this.Player.transform.position = new Vector3(0, maxHeight, 0);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
