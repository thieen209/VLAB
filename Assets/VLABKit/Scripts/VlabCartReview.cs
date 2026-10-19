using UnityEngine;

public sealed class VlabCartReview : MonoBehaviour
{
    [SerializeField] private VlabCart cartA,cartB;
    public VlabCart A=>cartA;
    public VlabCart B=>cartB;
    public VlabCart Active=>useB?cartB:cartA;
    bool useB,wide;
    double force=.05,mass=.2,initialSpeed;
    public void Select(bool b){cartA.Paused=cartB.Paused=true;useB=b;cartA.gameObject.SetActive(!b);cartB.gameObject.SetActive(b);ResetTrial();}
    public void ResetTrial(){Active.ResetTrial(mass,force<0?.3:-.3,initialSpeed);}
    void Start(){Select(false);Frame();}
    void Update(){Active.Advance(Time.deltaTime,force);Frame();}
    void Frame()
    {
        var c=GetComponent<Camera>();var focus=wide?new Vector3(-.34f,0,0):Active.transform.position+new Vector3(-.05f,0,0);
        c.transform.position=focus+(wide?new Vector3(.25f,.5f,-.65f):new Vector3(.18f,.16f,-.27f));c.transform.LookAt(focus);
        c.orthographicSize=wide?1.03f/Mathf.Max(1,c.aspect):.135f;
    }
    void OnGUI()
    {
        var c=Active;GUILayout.BeginArea(new Rect(12,12,338,475),GUI.skin.box);
        GUILayout.Label($"XE TRƯỢT {c.CartId} · MÔ HÌNH 1 CHIỀU");
        GUILayout.BeginHorizontal();if(GUILayout.Button("Xe A · xanh",GUILayout.Height(27)))Select(false);if(GUILayout.Button("Xe B · cam",GUILayout.Height(27)))Select(true);GUILayout.EndHorizontal();
        GUILayout.Label($"m = {c.MassKg:F2} kg · F = {force:F2} N · a = {force/c.MassKg:F3} m/s²");
        GUILayout.Label($"x = {c.PositionM:F4} m · v = {c.VelocityMps:F4} m/s");
        GUILayout.Label($"t = {c.ElapsedSeconds:F4} s · K = {c.KineticJ:F5} J");
        GUILayout.Label($"Công F: {c.WorkJ:F5} J · mất khi dừng: {c.StopLossJ:F5} J");
        GUILayout.BeginHorizontal();if(GUILayout.Button("0,20 kg")){mass=.2;ResetTrial();}if(GUILayout.Button("0,40 kg")){mass=.4;ResetTrial();}GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();foreach(double f in new[]{-.05,0,.05,.10})if(GUILayout.Button(f.ToString("F2")+" N")){force=f;ResetTrial();}GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();if(GUILayout.Button("v₀ = 0")){initialSpeed=0;ResetTrial();}if(GUILayout.Button("v₀ = +0,2 m/s")){initialSpeed=.2;ResetTrial();}GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();if(GUILayout.Button(c.Paused?"Chạy":"Tạm dừng",GUILayout.Height(28))&&!c.AtLimit)c.Paused=!c.Paused;
        if(GUILayout.Button("Bước 0,1 s",GUILayout.Height(28))){c.Paused=false;c.Advance(.1,force);c.Paused=true;}GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();if(GUILayout.Button("Đặt lại",GUILayout.Height(28)))ResetTrial();if(GUILayout.Button(wide?"Theo xe":"Xem cả ray",GUILayout.Height(28))){wide=!wide;Frame();}GUILayout.EndHorizontal();
        GUILayout.Label(c.AtLimit?$"Đã dừng ở giới hạn thử · v trước dừng {c.ImpactVelocity:F4} m/s":"Giới hạn kiểm tra x = ±0,40 m.");
        GUILayout.Label("Lực và khối lượng là giá trị đặt của mô hình; ray này chỉ kiểm tra hình học. Chưa có cổng quang, va chạm hai xe, khí cấp hoặc tải treo.",new GUIStyle(GUI.skin.label){wordWrap=true});
        GUILayout.EndArea();
    }
}
