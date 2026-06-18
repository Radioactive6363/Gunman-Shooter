using System;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Networking;

using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class WorldTimeManager : MonoBehaviourPunCallbacks
{
    [Serializable]
    private class TimeResponse
    {
        public string datetime;
        public string timezone;
    }

    public static WorldTimeManager Instance;

    public string CurrentCityName { get; private set; }
    public DateTime CurrentTime { get; private set; }

    private readonly string[] timezones =
    {
        "America/New_York",
        "Europe/London",
        "Europe/Madrid",
        "Asia/Tokyo",
        "Australia/Sydney",
        "America/Argentina/Buenos_Aires",
        "Europe/Paris",
        "Asia/Seoul",
        "America/Los_Angeles",
        "Africa/Cairo"
    };

    private const string CITY_KEY = "CITY";
    private const string TIME_KEY = "TIME";

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
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
        string timezone =
            timezones[UnityEngine.Random.Range(0, timezones.Length)];

        string url =
            $"https://worldtimeapi.org/api/timezone/{timezone}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error API: {request.error}");
                yield break;
            }

            TimeResponse response =
                JsonUtility.FromJson<TimeResponse>(
                    request.downloadHandler.text);

            DateTime dateTime =
                DateTime.Parse(response.datetime);

            string city =
                timezone.Split('/')[timezone.Split('/').Length - 1]
                .Replace("_", " ");

            PhotonHashtable roomProps = new PhotonHashtable
            {
                { CITY_KEY, city },
                { TIME_KEY, dateTime.ToString("O") }
            };

            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

            ApplyData(city, dateTime);
        }
    }

    void ApplyData(string city, DateTime time)
    {
        CurrentCityName = city;
        CurrentTime = time;

        Debug.Log($"Ciudad: {CurrentCityName}");
        Debug.Log($"Hora: {CurrentTime:HH:mm:ss}");

        UpdateSun();
    }

    void UpdateSun()
    {
        float hour =
            CurrentTime.Hour +
            CurrentTime.Minute / 60f +
            CurrentTime.Second / 3600f;

        float sunAngle =
            (hour / 24f) * 360f - 90f;

        Debug.Log($"Ángulo del Sol: {sunAngle}");

        // Ejemplo:
        // sunLight.transform.rotation =
        //     Quaternion.Euler(sunAngle, 170f, 0f);
    }

    void LoadExistingProperties()
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

    public override void OnRoomPropertiesUpdate(PhotonHashtable propertiesThatChanged)
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
