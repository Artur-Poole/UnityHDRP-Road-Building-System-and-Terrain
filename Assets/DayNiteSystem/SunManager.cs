using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class SunManager : MonoBehaviour
{
    public static SunManager Instance { get; private set; }


    [SerializeField] private Light sunLight;
    private HDAdditionalLightData sunLightData;
    [SerializeField] Transform sunContainer;

    [Tooltip("How fast to blend into the new rotation (1 = ~1 second).")]
    [SerializeField] private float smoothSpeed = 1f;
    [SerializeField, Range(-90f, 90f)] private float latitude = 45f;



    public event Action OnSunsetStarted; //say 150f? 30f from 0. 
    public event Action OnSunsetAngleReached; // at this point sun has reached horizon... night theme should be setup  // So Time Manager will enter and exit Sunset base don these and these alone...

    public event Action OnSunriseStarted; // e.g. Day/NightOnSunriseAngleReached // at this point sun has reached horizon... day theme should be setup
    public event Action OnSunriseAngleReached; // e.g. Day/Night   // So Time Manager will enter and exit sunrise based don these and these alone... 


    // Time Manger does not know what day or night time is... this will remain dynamic arranged by the SUN... the HOURCHANGE of timemanager will happen regardles ie mainting WorkSchedule and hour dependent stuff
    // But this will delink The GetPhase from the hour change and instead just change 5 times a day...
    // Technically, althjough not wanted, further sub phases can be added if we continue to be angle dependent... 

    // OnSunsetStarted -- Phase Day -> Sunset
    // OnSunsetReached -- Phase Sunset -> Night
    // OnSunriseStarted - Phase Night -> Sunrise
    // OnSunriseEnded --- Phase Sunrise -> Day


    public int ViewableHourTime;
    public int ViewableMinuteTime;
    public float ViewableHourAngle;

    private DayPhase currentPhase;
    private Quaternion _lightTargetRot;
    private Quaternion _containerTargetRot;

    private float sunDesiredIntensity = 85000f;
    //private float enviroLightingIntensityTarget = 1f;
    //private float enviroReflectionsIntensityTarget = 1f;

    private bool attached = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; } 
        Instance = this;

        //if (1 == 0)
        //{
        //    tempRidOfWarning();
        //}

    }

    void tempRidOfWarning()
    {
        OnSunriseStarted.Invoke();
        OnSunsetAngleReached.Invoke();
        OnSunsetStarted.Invoke();
        OnSunriseAngleReached.Invoke();
        DayPhase throwaway = currentPhase;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (sunLight != null)
        {
            sunLightData = sunLight.GetComponent<HDAdditionalLightData>();
            
        }
    }

    // Update is called once per frame
    void Update()
    {

        if ( attached == false) 
        {
            OnEnable();
            attached = true;
        }

        // blending rotations... 
        sunLight.transform.localRotation = Quaternion.Lerp(
                                                sunLight.transform.localRotation,
                                                _lightTargetRot,
                                                smoothSpeed * Time.deltaTime
                                            );
        sunContainer.transform.localRotation = Quaternion.Lerp(
                                                sunContainer.transform.localRotation,
                                                _containerTargetRot,
                                                smoothSpeed * Time.deltaTime
                                            );

        sunLight.intensity = Mathf.Lerp(sunLight.intensity, sunDesiredIntensity, smoothSpeed * Time.deltaTime);
        

        //if (currentPhase == DayPhase.Sunset || currentPhase == DayPhase.Sunrise)
        //{
        //    RenderSettings.ambientIntensity = Mathf.Lerp(
        //        RenderSettings.ambientIntensity,
        //        enviroLightingIntensityTarget,
        //        smoothSpeed * Time.deltaTime
        //    );

        //    RenderSettings.reflectionIntensity = Mathf.Lerp(
        //        RenderSettings.reflectionIntensity,
        //        enviroReflectionsIntensityTarget,
        //        smoothSpeed * Time.deltaTime
        //    );
        //}
    }

    private void OnEnable()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnHourChanged += HandleHourChanged;
            TimeManager.Instance.OnPhaseChanged += HandlePhaseChanged;
            TimeManager.Instance.OnMinuteChanged += HandleMinuteChanged;
            currentPhase = DayPhase.Day;
            UpdateSunAngles(TimeManager.Instance.gameStart);
            sunContainer.transform.rotation = _containerTargetRot;
            sunLight.transform.rotation = _lightTargetRot;
            sunLight.intensity = sunDesiredIntensity;

        }
    }

    private void OnDisable()
    {
        
    }


    /// <summary>
    /// Based on the current Time in Hr & Minutes will calculate what angle the sun should be at...
    /// Based on the current date will calculate the declination
    /// 
    /// TODO::
    /// - Add so Day time gets longer and shorter depending on date and lattide...
    /// </summary>
    /// <param name="gameTime"></param>
    private void UpdateSunAngles(DateTime gameTime)
    {
        float hours = gameTime.Hour + gameTime.Minute / 60f;
        ViewableHourTime = gameTime.Hour;
        ViewableMinuteTime = gameTime.Minute;


        float hourAngle = (hours / 24f) * 360f - 90f;
        ViewableHourAngle = hourAngle;

        int N = gameTime.DayOfYear;
        float declination = 23.45f * Mathf.Sin(Mathf.Deg2Rad * (360f * (284 + N) / 365f));

        float elevationAtNoon = 90f - latitude + declination;

        _lightTargetRot = Quaternion.Euler(hourAngle, 0f, 0f);
        _containerTargetRot = Quaternion.Euler(0f, 0f, -1 * declination);




        //Debug.Log("Set the points");

        if (hourAngle < 0f || hourAngle > 180f) {
            sunDesiredIntensity = 0f;
        }
        else
        {
            float normalizedAngle = hourAngle * Mathf.Deg2Rad;
            

            float baseIntensity = 85000f;
            float floor         = 15000f;

            float scaledIntensity = baseIntensity * Mathf.Sin(normalizedAngle);

            if (scaledIntensity > floor) scaledIntensity = floor;

            sunDesiredIntensity = scaledIntensity;
        }

    }

    private void HandleMinuteChanged(DateTime dt)
    {
        //Debug.Log("HandleMinuteChange");
        UpdateSunAngles(dt);
    }

    private void HandleHourChanged(DateTime dt)
    {

    }

    private void HandlePhaseChanged(DayPhase dp)
    {

    }

}
