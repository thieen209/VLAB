using System;using System.Linq;using UnityEngine;
public sealed class VlabMitosisAtlas:MonoBehaviour{
 [SerializeField] Texture2D mask;[SerializeField] TextAsset manifest;
 [Serializable] public sealed class Cell{public int cell_id;public string stage;public bool scorable;public float[] center_uv;}
 [Serializable] sealed class Data{public Cell[] cells;}
 Cell[] cells;static readonly string[] Stages={"interphase","prophase","metaphase","anaphase","telophase"};
 bool Ready{get{if(!isActiveAndEnabled||!mask||!manifest)return false;if(cells==null){var d=JsonUtility.FromJson<Data>(manifest.text);if(d?.cells==null||d.cells.Length!=100||d.cells.Select(c=>c.cell_id).Distinct().Count()!=100||d.cells.Any(c=>c.cell_id<1||c.cell_id>100||c.scorable&&!Stages.Contains(c.stage)))return false;cells=d.cells;}return true;}}
 public bool TryResolve(Vector2 uv,out int id,out string stage,out bool scorable){id=0;stage=null;scorable=false;if(!Ready||!float.IsFinite(uv.x)||!float.IsFinite(uv.y)||uv.x<0||uv.x>=1||uv.y<0||uv.y>=1)return false;int key=((Color32)mask.GetPixel((int)(uv.x*mask.width),(int)(uv.y*mask.height))).r;var cell=cells.FirstOrDefault(c=>c.cell_id==key);if(cell==null)return false;id=key;stage=cell.stage;scorable=cell.scorable;return true;}
 public bool TryCheck(int id,string answer,out bool correct){correct=false;if(!Ready||!Stages.Contains(answer))return false;var cell=cells.FirstOrDefault(c=>c.cell_id==id);if(cell==null||!cell.scorable)return false;correct=cell.stage==answer;return true;}
 // shortcut: conceptual schematic cues and representative chromosomes only; no realmicrograph, karyotype, biologicalphasefrequency or diagnostic interpretation.
}
