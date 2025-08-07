using UnityEngine;
using UnityEngine.Rendering;

public class GlobalVolumeManager : MonoBehaviour
{

    private static GlobalVolumeManager _instance;

    public static GlobalVolumeManager Instance { get { return _instance; } }

    [SerializeField] public Volume GlobalVolume;
    [SerializeField] public Volume SkyAndFogVolume;
    [SerializeField] public Volume AdaptiveProbeVolume;




    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        } else
        {
            _instance = this;
        }
    }










}
