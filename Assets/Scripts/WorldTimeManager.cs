using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

using Photon.Pun;
using Photon.Realtime;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class WorldTimeManager : MonoBehaviourPunCallbacks
{
    [Serializable]
    public class CurrentData
    {
        public string time;
    }

    [Serializable]
    public class OpenMeteoResponse
    {
        public CurrentData current;
    }

    [Serializable]
    public class CityData
    {
        public string cityName;
        public float latitude;
        public float longitude;

        public CityData(string city, float lat, float lon)
        {
            cityName = city;
            latitude = lat;
            longitude = lon;
        }
    }

    public static WorldTimeManager Instance;

    public string CurrentCityName { get; private set; }
    public DateTime CurrentTime { get; private set; }

    private const string CITY_KEY = "CITY";
    private const string TIME_KEY = "TIME";

    private CityData[] cities =
    {
        new CityData("Buenos Aires", -34.6037f, -58.3816f),
        new CityData("New York", 40.7128f, -74.0060f),
        new CityData("London", 51.5072f, -0.1276f),
        new CityData("Madrid", 40.4168f, -3.7038f),
        new CityData("Tokyo", 35.6762f, 139.6503f),
        new CityData("Sydney", -33.8688f, 151.2093f),
        new CityData("Seoul", 37.5665f, 126.9780f),
        new CityData("Paris", 48.8566f, 2.3522f),
        new CityData("Cairo", 30.0444f, 31.2357f)
    };

    private void Awake()
    {
        Instance = this;
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(GetRandomCityTime());
        }
        else
        {
            LoadExistingProperties();
        }
    }

    IEnumerator GetRandomCityTime()
    {
        CityData city =
            cities[UnityEngine.Random.Range(0, cities.Length)];

        string url = string.Format(CultureInfo.InvariantCulture,
            "https://api.open-meteo.com/v1/forecast?latitude={0}&longitude={1}&current=temperature_2m&timezone=auto",
            city.latitude, 
            city.longitude);

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Open-Meteo Error: {request.error}");
                yield break;
            }

            OpenMeteoResponse response =
                JsonUtility.FromJson<OpenMeteoResponse>(
                    request.downloadHandler.text);

            DateTime cityTime =
                DateTime.Parse(response.current.time);

            PhotonHashtable props = new PhotonHashtable
            {
                { CITY_KEY, city.cityName },
                { TIME_KEY, cityTime.ToString("O") }
            };

            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            ApplyData(city.cityName, cityTime);
        }
    }

    private void ApplyData(string cityName, DateTime cityTime)
    {
        CurrentCityName = cityName;
        CurrentTime = cityTime;

        Debug.Log($"Ciudad: {CurrentCityName}");
        Debug.Log($"Hora: {CurrentTime:HH:mm:ss}");

        UpdateSun();
    }

    private void UpdateSun()
    {
        float hour =
            CurrentTime.Hour +
            CurrentTime.Minute / 60f +
            CurrentTime.Second / 3600f;

        float t = hour / 24f;

        float sunAngle = (t * 360f) - 90f;

        Light sun = FindObjectOfType<Light>();

        if (sun != null)
        {
            sun.transform.rotation =
                Quaternion.Euler(sunAngle, 170f, 0f);

            float sunHeight = Mathf.Sin(t * Mathf.PI * 2f);

            float sunIntensity = Mathf.SmoothStep(-0.2f, 1f, sunHeight);

            float minLight = 0.12f;

            sun.intensity = Mathf.Max(sunIntensity, minLight);
        }

        UpdateAmbientLighting(t);
    }

    private void UpdateAmbientLighting(float t)
    {
        float dayFactor = Mathf.Sin(t * Mathf.PI * 2f);

        dayFactor = Mathf.SmoothStep(-0.2f, 1f, dayFactor);

        dayFactor = Mathf.Clamp01(dayFactor);

        float nightFactor = 1f - dayFactor;

        Color dayColor = Color.white;
        Color nightColor = new Color(0.08f, 0.08f, 0.2f);

        RenderSettings.ambientLight =
            Color.Lerp(nightColor, dayColor, dayFactor);

        RenderSettings.ambientIntensity =
            Mathf.Lerp(0.25f, 1f, dayFactor);
    }

    private void LoadExistingProperties()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return;

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(CITY_KEY))
            return;

        string city =
            (string)PhotonNetwork.CurrentRoom.CustomProperties[CITY_KEY];

        string timeString =
            (string)PhotonNetwork.CurrentRoom.CustomProperties[TIME_KEY];

        DateTime time =
            DateTime.Parse(timeString);

        ApplyData(city, time);
    }

    public override void OnRoomPropertiesUpdate(
        PhotonHashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(CITY_KEY))
        {
            string city =
                (string)PhotonNetwork.CurrentRoom.CustomProperties[CITY_KEY];

            string timeString =
                (string)PhotonNetwork.CurrentRoom.CustomProperties[TIME_KEY];

            DateTime time =
                DateTime.Parse(timeString);

            ApplyData(city, time);
        }
    }
}
