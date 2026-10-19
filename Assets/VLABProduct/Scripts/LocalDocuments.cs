using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Vlab.Product
{
    public static class LocalDocuments
    {
        public static IEnumerator OpenPdf(string filename,Action<string> failure)
        {
            if(filename!=Path.GetFileName(filename)||!filename.EndsWith(".pdf",StringComparison.OrdinalIgnoreCase)){failure("Invalid local document name.");yield break;}
            string path=Path.Combine(Application.streamingAssetsPath,"Legal",filename);
            if(Application.platform!=RuntimePlatform.Android){Application.OpenURL(new Uri(path).AbsoluteUri);yield break;}
            var request=UnityWebRequest.Get(path);yield return request.SendWebRequest();
            if(request.result!=UnityWebRequest.Result.Success){failure("The bundled PDF could not be opened: "+request.error);request.Dispose();yield break;}
            try
            {
                string folder=Path.Combine(Application.persistentDataPath,"Legal");Directory.CreateDirectory(folder);string local=Path.Combine(folder,filename);File.WriteAllBytes(local,request.downloadHandler.data);
#if UNITY_ANDROID && !UNITY_EDITOR
                using(var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using(var activity=player.GetStatic<AndroidJavaObject>("currentActivity"))
                using(var provider=new AndroidJavaClass("androidx.core.content.FileProvider"))
                using(var file=new AndroidJavaObject("java.io.File",local))
                using(var uri=provider.CallStatic<AndroidJavaObject>("getUriForFile",activity,Application.identifier+".vlabfiles",file))
                using(var intent=new AndroidJavaObject("android.content.Intent","android.intent.action.VIEW"))
                {intent.Call<AndroidJavaObject>("setDataAndType",uri,"application/pdf");intent.Call<AndroidJavaObject>("addFlags",1);activity.Call("startActivity",intent);}
#endif
            }
            catch(Exception ex){failure("No PDF reader could open the bundled document. The full text remains available inside VLAB. "+ex.Message);}
            request.Dispose();
        }
    }
}
