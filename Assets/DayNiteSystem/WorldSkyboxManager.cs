using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class WorldSkyboxManager : MonoBehaviour
{
    private static WorldSkyboxManager _instance;

    public static WorldSkyboxManager Instance { get { return _instance; } }

    [SerializeField] public List<Skybox> skybox = new List<Skybox>();



    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }
}
