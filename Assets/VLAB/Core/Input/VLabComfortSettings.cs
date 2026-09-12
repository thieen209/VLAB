using System;
using UnityEngine;

namespace VLAB.Core.Input
{
    [Serializable]
    public sealed class VLabComfortSettings
    {
        public float movementSpeed=1.8f;
        public float pointerLength=5;
        public float textScale=1;
        public bool leftHanded;
        public bool reducedMotion;
        public bool highContrast;
        public bool smoothTurn;
        public float snapAngle=30;
        public float turnSpeed=45;
        public const string StorageKey="VLAB.Comfort.v1";
        public static VLabComfortSettings Current {get; private set;}=new VLabComfortSettings();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Load()
        {
            try {Current=JsonUtility.FromJson<VLabComfortSettings>(PlayerPrefs.GetString(StorageKey,""))??new VLabComfortSettings();}
            catch(ArgumentException){Current=new VLabComfortSettings();}
            Current.Validate();
        }
        public void Validate()
        {
            movementSpeed=Clamp(movementSpeed,.5f,3,1.8f);pointerLength=Clamp(pointerLength,2,8,5);
            textScale=Clamp(textScale,.9f,1.15f,1);snapAngle=Clamp(snapAngle,15,60,30);turnSpeed=Clamp(turnSpeed,15,90,45);
        }
        private static float Clamp(float value,float min,float max,float fallback)=>float.IsNaN(value)||float.IsInfinity(value)?fallback:Mathf.Clamp(value,min,max);
        public static void Save(){Current.Validate();PlayerPrefs.SetString(StorageKey,JsonUtility.ToJson(Current));PlayerPrefs.Save();}
    }
}
