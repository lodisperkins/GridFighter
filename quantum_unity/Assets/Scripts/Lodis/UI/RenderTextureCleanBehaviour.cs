using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class RenderTextureCleanBehaviour : MonoBehaviour
{
    [SerializeField]
    private VideoPlayer _player;

    private void OnDisable()
    {
        Destroy(_player.targetTexture);
        GL.Clear(true, true, Color.black);
    }
}
