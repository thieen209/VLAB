using UnityEngine;

public sealed class VlabBubbleReview : MonoBehaviour
{
    [SerializeField]private VlabBubble effect;
    [SerializeField]private VlabBeaker beaker;
    private bool light;
    private void Start()
    {
        beaker.Add(150);
        effect.SetRiseHeight((float)beaker.SurfaceHeightMeters-effect.transform.position.y-.002f);
    }
    private void OnGUI()
    {
        var previousColor=GUI.contentColor;GUI.contentColor=light?Color.black:Color.white;
        GUI.Label(new Rect(20,20,940,28),"BỌT KHÍ MINH HỌA · Sprite 64 px · Không dùng đếm bọt để đo lượng khí");
        GUI.Label(new Rect(20,48,940,28),"Phát "+effect.Rate.ToString("F0")+" / giây · Đang hiện "+effect.Particles.particleCount+" / "+VlabBubble.MaxLive+" · Cốc "+beaker.VolumeMl.ToString("F0")+" mL");
        foreach(int rate in new[]{4,10,20})
        {
            int x=rate==4?20:rate==10?144:268;
            if(GUI.Button(new Rect(x,88,112,32),rate+" bọt / giây"))effect.SetEmission(rate);
        }
        if(GUI.Button(new Rect(20,132,112,32),"Dừng phát"))effect.Stop(false);
        if(GUI.Button(new Rect(144,132,112,32),"Xóa bọt"))effect.Stop(true);
        if(GUI.Button(new Rect(268,132,112,32),"Nền sáng / tối")){light=!light;Camera.main.backgroundColor=light?new Color(.85f,.85f,.85f):new Color(.08f,.12f,.16f);}
        GUI.Label(new Rect(20,178,360,52),"Bọt chỉ là hiệu ứng, không phải vật rắn.\nLượng khí cần mô hình thí nghiệm riêng.");
        GUI.contentColor=previousColor;
    }
}
