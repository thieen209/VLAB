#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using Vlab.Product;

public static class Patch1SettingsChecks
{
    [Serializable] sealed class Envelope {public int format=1;public string payload,sha256;}
    public static string Run()
    {
        int count=0;void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;}
        string folder=Path.Combine(Path.GetTempPath(),"VLAB-Patch1-settings-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        var profile=new LocalProfile{locale="vi"};var store=new ProfileStore(folder);
        string payload=JsonUtility.ToJson(profile);
        foreach(string key in new[]{"invertLookX","invertLookY","phoneGyro","showPhoneJoystick","focusAssist","moveSpeed","cameraFov"})payload=Regex.Replace(payload,"\\\""+key+"\\\":(?:true|false|[0-9.]+),?","");
        payload=payload.Replace(",}","}");
        File.WriteAllText(Path.Combine(folder,"profile.json"),JsonUtility.ToJson(new Envelope{payload=payload,sha256=ProfileStore.Hash(payload)}));
        var loaded=store.Load();Check(!loaded.writeProtected&&!loaded.recovered,"0.1.1 profile missing new phone fields still loads");
        var settings=loaded.profile.settings;Check(settings.moveSpeed==1&&settings.cameraFov==65&&settings.showPhoneJoystick&&settings.focusAssist,"Absent phone settings receive explicit defaults");
        settings.invertLookX=settings.invertLookY=settings.phoneGyro=true;settings.showPhoneJoystick=settings.focusAssist=false;settings.moveSpeed=.7f;settings.cameraFov=80;
        Check(store.Save(loaded.profile,out var error),"Phone settings save: "+error);
        var reopened=new ProfileStore(folder).Load();var copy=reopened.profile.settings;
        Check(!reopened.writeProtected&&copy.invertLookX&&copy.invertLookY&&copy.phoneGyro&&!copy.showPhoneJoystick&&!copy.focusAssist&&Mathf.Abs(copy.moveSpeed-.7f)<.0001f&&copy.cameraFov==80,"Every phone setting survives a fresh store reopen");
        foreach(float invalid in new[]{0,float.NaN,float.PositiveInfinity,121}){var bad=copy.Copy();bad.cameraFov=invalid;bool rejected=false;try{bad.Validate();}catch(InvalidDataException){rejected=true;}Check(rejected,"Invalid explicit FOV rejected "+invalid);}
        foreach(float invalid in new[]{0,float.NaN,-1,3}){var bad=copy.Copy();bad.moveSpeed=invalid;bool rejected=false;try{bad.Validate();}catch(InvalidDataException){rejected=true;}Check(rejected,"Invalid explicit movement speed rejected "+invalid);}
        return "PASS "+count+" phone settings migration, persistence and invalid-value checks. Isolated Temp profile retained: "+folder;
    }
}
#endif
