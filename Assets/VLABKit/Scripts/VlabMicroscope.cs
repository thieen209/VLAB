using UnityEngine;
public sealed class VlabMicroscope : MonoBehaviour
{
 [SerializeField] Transform stage,turret,coarse,fine;
 [SerializeField] Transform[] clips,tips;
 [SerializeField] Vector3 localUp,localRight,stageRest;
 [SerializeField] Quaternion turretRest,coarseRest,fineRest;
 [SerializeField] Quaternion[] clipRest;
 [SerializeField] Renderer eyepiece;
 [SerializeField] int imageSlot;
 MaterialPropertyBlock properties;
 public float FocusMm{get;private set;}
 public int ObjectiveIndex{get;private set;}
 public bool ClipsOpen{get;private set;}
 public Texture2D ViewImage{get;private set;}
 public Transform ActiveTip=>tips[ObjectiveIndex];
 public Transform Stage=>stage;
 bool Ready=>isActiveAndEnabled&&(transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f;
 public bool TrySetFocus(float mm){if(!Ready||!float.IsFinite(mm)||mm<0||mm>13.5f)return false;stage.localPosition=stageRest+localUp*(mm*.001f);coarse.localRotation=Quaternion.AngleAxis(-mm*30,localRight)*coarseRest;fine.localRotation=Quaternion.AngleAxis(-mm*180,localRight)*fineRest;FocusMm=mm;return true;}
 public bool TrySetObjective(int index){if(!Ready||index<0||index>2)return false;turret.localRotation=Quaternion.AngleAxis(120*index,localUp)*turretRest;ObjectiveIndex=index;return true;}
 public bool TrySetClips(bool open){if(!Ready)return false;var axis=stage.InverseTransformDirection(transform.TransformDirection(localRight));for(int i=0;i<clips.Length;i++)clips[i].localRotation=Quaternion.AngleAxis(open?35:0,axis)*clipRest[i];ClipsOpen=open;return true;}
 public bool TrySetEyepieceImage(Texture2D image){if(!Ready)return false;properties??=new MaterialPropertyBlock();eyepiece.GetPropertyBlock(properties,imageSlot);properties.SetTexture("_BaseMap",image?image:Texture2D.blackTexture);properties.SetColor("_BaseColor",Color.white);eyepiece.SetPropertyBlock(properties,imageSlot);ViewImage=image;return true;}
 // shortcut: safe geometric focus travel and supplied2D imagery; knob angles are illustrative, not optical calibration or a microscopy simulator.
}
