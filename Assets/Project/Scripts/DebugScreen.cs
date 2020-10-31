using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class DebugScreen : MonoBehaviour
{
    private World world;
    private Text text;

    private float frameRate;
    private float timer;

    

    private void Awake()
    {
        world = GameObject.Find("World").GetComponent<World>();
        text = GetComponent<Text>();

        
    }

    private void Update()
    {
        string debugText = "MineCraft like game \n";
        debugText += "FPS: " + frameRate + "\n";
        debugText += "Position: " + world.player.transform.position + "\n";
        debugText += "Chunk: " + world.playerChunkCoordinate + "\n";

        debugText += int.MaxValue / VoxelData.chunkWidth + "\n";
        debugText += int.MinValue / VoxelData.chunkWidth + "\n";
        debugText += uint.MaxValue + "\n";

        text.text = debugText;

        if (timer > 1f)
        {
            frameRate = (int)(1f / Time.unscaledDeltaTime);
            timer = 0;
        }
        else
        {
            timer += Time.unscaledDeltaTime;
        }
    }

}
