using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace VLAB.MainMenu
{
    /// <summary>Legacy world signage uses scene depth instead of the overlay font shader.</summary>
    public sealed class VLabWorldTextDepth : MonoBehaviour
    {
        private readonly Dictionary<Material,Material> materials=new Dictionary<Material,Material>();
        private void Start()
        {
            // Canvas graphics do not write depth, so world text sorts before the lesson panels.
            foreach(var label in FindObjectsByType<TextMeshPro>())label.GetComponent<Renderer>().sortingOrder=-1;
            var shader=Shader.Find("TextMeshPro/Mobile/Bitmap");
            if(shader==null)return;
            foreach(var label in FindObjectsByType<TextMesh>())
            {
                var renderer=label.GetComponent<Renderer>();var original=renderer.sharedMaterial;
                if(original==null)continue;
                if(!materials.TryGetValue(original,out var material))
                {
                    // Bitmap font atlases store glyph coverage in alpha, not RGB.
                    material=new Material(shader){mainTexture=original.mainTexture,color=Color.white,renderQueue=3000};
                    materials.Add(original,material);
                }
                renderer.sharedMaterial=material;renderer.sortingOrder=-1;
            }
            Font.textureRebuilt+=RefreshAtlas;
        }
        private void RefreshAtlas(Font font)
        {foreach(var pair in materials)if(pair.Key!=null)pair.Value.mainTexture=pair.Key.mainTexture;}
        private void OnDestroy()
        {Font.textureRebuilt-=RefreshAtlas;foreach(var material in materials.Values)if(material!=null)Destroy(material);}
    }
}
