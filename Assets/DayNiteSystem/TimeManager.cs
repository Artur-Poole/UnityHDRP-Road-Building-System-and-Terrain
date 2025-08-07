using System;
using UnityEngine;


public enum DayPhase
{
    Day = 0, // 9 - 21
    Sunset, // 21
    Night, // 22-7
    Sunrise, // 7
    EarlyMorning // 7-9
}

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    private static int startYear = 2000;
    private static int startMonth = 6;
    private static int startDay = 1;
    private static int startHour = 15; // 6 is sunrise start
    private static int startMinute = 0;

    

    //                              Start Date is December 1st 2000
    //                              Start Date is June 1st 2000 (shortest day southern hemisphere)
    public DateTime gameStart;
    double totalGameSeconds = 0;

    [Header("Cycle Settings")]
    public float realSecondsPerGameDay = 30 * 60f; // 30 min
    float gameSecondsPerRealSecond;

    [Header("Speed Controls")]
    public float speedMultiplier = 1f; // 1 = normal; >1 = fast
    public float sleepSpeedMultiplier = 20f;

    public event Action<DateTime> OnMinuteChanged; // e.g. Day/Night
    public event Action<DateTime> OnHourChanged;
    public event Action<DayPhase> OnPhaseChanged; // e.g. Day/Night



    private bool hasEnabledSunMangerListeners = false;

    DateTime lastNotifiedMin;
    DateTime lastNotifiedHour;
    DayPhase lastPhase;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);

        gameStart = new DateTime(startYear, startMonth, startDay, startHour, startMinute, 0);

        gameSecondsPerRealSecond = 86400f / realSecondsPerGameDay;
        lastNotifiedHour = gameStart;
        lastPhase = GetPhase(gameStart);
    }

    void Update()
    {

        if (hasEnabledSunMangerListeners == false && SunManager.Instance != null)
        {
            OnEnableSunListener();
            hasEnabledSunMangerListeners = true;
        }


        totalGameSeconds += Time.deltaTime * gameSecondsPerRealSecond * speedMultiplier;
        var now = gameStart.AddSeconds(totalGameSeconds);

        // Hour change?
        if (now.Hour != lastNotifiedHour.Hour)
        {
            lastNotifiedHour = new DateTime(
                now.Year, now.Month, now.Day, now.Hour, 0, 0
            );
            OnHourChanged?.Invoke(now);
        }
        else if (now.Minute != lastNotifiedMin.Minute)
        {
            lastNotifiedHour = new DateTime(
                now.Year, now.Month, now.Day, now.Hour, 0, 0
            );
            OnMinuteChanged?.Invoke(now);
        }



        // Day/Night switch? --- OLD 
        var phase = GetPhase(now);
        if (phase != lastPhase)
        {
            lastPhase = phase;
            OnPhaseChanged?.Invoke(phase);
        }


        // Instead of constantly checking... we will determine phase from the SUN...
        // So OnPhaseChanged will stay the same, phase remains part of the Time Day Night whatever...
        // Add two Events to SunManager... SunSet and Sunrise and with both... invoke in the UpdateSunFunction
        // this TimeManager will then inherit that...

    }

    private void OnEnableSunListener()
    {
        SunManager.Instance.OnSunriseAngleReached += HandleSunriseVisuals;
        SunManager.Instance.OnSunsetAngleReached += HandleSunsetVisuals;
    }

    void HandleSunriseVisuals()
    {
        // Start light warmup, bloom transition, ambient audio fade-in
    }

    void HandleSunsetVisuals()
    {
        // Start cooling lights, fog changes, skybox fade
    }

    public DateTime Now => gameStart.AddSeconds(totalGameSeconds);

    public void SkipSeconds(double seconds) => totalGameSeconds += seconds;
    public void Sleep() => speedMultiplier = sleepSpeedMultiplier;
    public void Wake() => speedMultiplier = 1f;

    DayPhase GetPhase(DateTime dt)
    {

        // 24 we do reset stuff,,, 0 we do spawn stuff 

        //Day = 0, // 9 - 21
        //Sunset, // 21
        //Night, // 22-7
        //Sunrise, // 7
        //EarlyMorning // 7-9


        DayPhase phase = DayPhase.Day;
        int hr = dt.Hour;
        //Debug.Log("New Hour Received: " + hr);
        if (hr < 6)
        {
            phase = DayPhase.Night;
        }
        else if (hr >= 6 && hr < 8)
        {
            phase = DayPhase.Sunrise;
        }
        else if (hr < 9)
        {
            phase = DayPhase.EarlyMorning;
        }
        else if (hr < 19)
        {
            phase = DayPhase.Day;
        }
        else if (hr >= 19 && hr <= 20)
        {
            phase = DayPhase.Sunset;
        }
        else if (hr > 20)
        {
            phase = DayPhase.Night;
        }
        else
        {
            Debug.Log("Err: with hr" + hr);
        }

        return phase;
    }
}

