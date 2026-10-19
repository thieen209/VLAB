using System;using UnityEngine;
public sealed class VlabYeast:MonoBehaviour
{
 [SerializeField] Texture2D schematic;
 [SerializeField] GameObject cellSprite;
 public string SampleId{get;private set;}="";
 public Texture2D Schematic=>schematic;
 public bool IsIllustration=>true;
 bool Ready=>isActiveAndEnabled&&(transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f;
 public bool TryConfigureSample(string id){if(!Ready||string.IsNullOrWhiteSpace(id)||id.Length>64)return false;foreach(char c in id)if(char.IsControl(c))return false;try{SampleId=id.Trim().Normalize();}catch(ArgumentException){return false;}return true;}
 public bool TryShowMicroscope(VlabMicroscope microscope)=>Ready&&SampleId.Length>0&&schematic!=null&&microscope!=null&&microscope.TrySetEyepieceImage(schematic);
 public bool TrySetSpritePreview(bool visible){if(!Ready)return false;cellSprite.SetActive(visible);return true;}
 void OnDisable(){if(cellSprite)cellSprite.SetActive(false);}
 // shortcut: explicit sample identity and supplied educational image; no optical calibration, live-cell assay or fermentation solver.
}
