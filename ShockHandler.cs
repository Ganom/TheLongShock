using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace TheLongShockProper
{
    public class ShockHandler : MonoBehaviour
    {
        private const string APIUrl = "https://do.pishock.com/api/apioperate";

        private static IEnumerator SendPostRequestCoroutine(ShockPostData data)
        {
            var json = JsonUtility.ToJson(data);
            var jsonToSend = new UTF8Encoding().GetBytes(json);
            var unityWebRequest = new UnityWebRequest(APIUrl, "POST")
            {
                uploadHandler = new UploadHandlerRaw(jsonToSend),
                downloadHandler = new DownloadHandlerBuffer()
            };
            unityWebRequest.SetRequestHeader("Content-Type", "application/json");

            yield return unityWebRequest.SendWebRequest();
        }

        public void SendShock(int percent, ConfigData data)
        {
            var postData = new ShockPostData
            {
                username = data.username,
                name = "TheLongShock",
                code = data.shockerCode,
                intensity = percent.ToString(),
                duration = "1",
                apiKey = data.apiKey,
                op = data.testMode ? "1" : "0",
            };
            StartCoroutine(SendPostRequestCoroutine(postData));
        }
        
        public void SendVibrate(int percent, ConfigData data)
        {
            var postData = new ShockPostData
            {
                username = data.username,
                name = "TheLongShock",
                code = data.shockerCode,
                intensity = percent.ToString(),
                duration = "1",
                apiKey = data.apiKey,
                op = "1"
            };
            StartCoroutine(SendPostRequestCoroutine(postData));
        }
    }
}