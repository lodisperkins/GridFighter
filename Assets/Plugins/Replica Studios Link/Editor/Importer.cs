using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Packages.Replica.Bridge.Editor
{
    public class Importer
    {
        public IEnumerator CreateAsset(CreateAssetMessage message)
        {
            Debug.Log("Replica Link importing " + message.filename);
            using (var downloadRequest = UnityWebRequest.Get(message.url))
            {

                downloadRequest.downloadHandler = new DownloadHandlerFile("Assets/Replica/" + message.filename);
                yield return downloadRequest.SendWebRequest();

                while (!downloadRequest.isDone)
                    yield return true;

                #if UNITY_2019
                    if (downloadRequest.isNetworkError || downloadRequest.isHttpError)
                #else
                    if (downloadRequest.result != UnityWebRequest.Result.Success)
                #endif
                {
                    Debug.Log("Replica Link import asset error: " + downloadRequest.error);
                }
                else
                {
                    AssetDatabase.Refresh();
                    var importedAsset = AssetDatabase.LoadAssetAtPath("Assets/Replica/" + message.filename, typeof(Object));
                    Selection.activeObject = importedAsset;
                    EditorGUIUtility.PingObject(importedAsset);
                }
            }

        }
    }
}