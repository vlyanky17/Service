using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class EventService : MonoBehaviour
{

    [SerializeField] private string _serverUrl;
    [SerializeField] private int _cooldownBeforeSend;

    private DateTime _cooldownTime;
    private EventsToSend _events = new EventsToSend();

    private void Awake()
    {
        _cooldownTime = DateTime.Now.AddSeconds(_cooldownBeforeSend);
        var needPost = PlayerPrefs.GetInt("needPostAtStart");
        var json = PlayerPrefs.GetString("jsonToPost");
        if (needPost==1 && json!="")
        {
            _events = JsonConvert.DeserializeObject<EventsToSend>(json);
            Post(json);
        }
    }

    public void TrackEvent(string type, string data)
    {
        _events.AddEvent(type, data);
        if (DateTime.Now > _cooldownTime)
        {
            _cooldownTime = _cooldownTime.AddSeconds(_cooldownBeforeSend);
            var jsonString = JsonConvert.SerializeObject(_events);
            PlayerPrefs.SetInt("needPostAtStart", 1);
            PlayerPrefs.SetString("jsonToPost", jsonString);
            StartCoroutine(Post( jsonString));
        }
    }



    private IEnumerator Post( string bodyJsonString)
    {
        var request = new UnityWebRequest(_serverUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        if (request.responseCode == 200)
        {
            _events.events.Clear();
            PlayerPrefs.SetInt("needPostAtStart", 0);
            PlayerPrefs.SetString("jsonToPost", "");
        }
    }

    private class EventsToSend
    {
        public List<Dictionary<string, string>> events = new List<Dictionary<string, string>>();
        public void AddEvent(string type, string data)
        {
            events.Add(new Dictionary<string, string> { { "type", type }, { "data", data } });
        }
    }
}
