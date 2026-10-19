using UnityEngine;

public sealed class VlabBallReview : MonoBehaviour
{
    [SerializeField] private VlabProjectile ball;
    private float angle=45;
    private float speed=5;
    private float launchHeight;
    private Vector3 landing;
    private bool hasLanding;
    private void Start() => ball.FirstContact+=RecordContact;
    private void OnDestroy() { if(ball!=null) ball.FirstContact-=RecordContact; }
    private void RecordContact(Vector3 position) { landing=position; hasLanding=true; }

    public void Fire()
    {
        if(ball.IsFlying) return;
        hasLanding=false;
        float radians=angle*Mathf.Deg2Rad;
        ball.Launch(new Vector3(0,launchHeight+ball.Diameter/2,0),new Vector3(speed*Mathf.Cos(radians),speed*Mathf.Sin(radians),0),9.81,ball.Diameter/2);
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(20,20,720,28),"VIÊN BI · Mô hình lý tưởng, không lực cản · g = 9,81 m/s²");
        GUI.Label(new Rect(20,48,720,28),"Bản thử chuyển động; bộ phóng và tấm hứng sẽ được ghép ở prompt tương ứng.");
        if(!ball.IsFlying)
        {
            if(GUI.Button(new Rect(20,84,120,32),"Góc "+angle+"°")) angle=angle==20?30:angle==30?45:angle==45?60:angle==60?70:20;
            if(GUI.Button(new Rect(152,84,120,32),"v₀ = "+speed+" m/s")) speed=speed==5?10:5;
            if(GUI.Button(new Rect(284,84,120,32),"h₀ = "+launchHeight+" m")) launchHeight=launchHeight==0?.5f:0;
        }
        if(GUI.Button(new Rect(20,126,120,32),"Phóng")) Fire();
        if(GUI.Button(new Rect(152,126,120,32),ball.IsPaused?"Tiếp tục":"Tạm dừng")) ball.IsPaused=!ball.IsPaused;
        if(GUI.Button(new Rect(284,126,120,32),"Đặt lại")) { ball.ResetFlight(); hasLanding=false; }
        GUI.Label(new Rect(20,170,720,28),"t = "+ball.Elapsed.ToString("F3")+" s · v = "+ball.Velocity.magnitude.ToString("F3")+" m/s");
        if(hasLanding) GUI.Label(new Rect(20,198,720,28),"Chạm đầu: R = "+ball.HorizontalRange.ToString("F3")+" m");
        // The screen marker aids inspection without enlarging the physical ball mesh.
        Vector3 screen=Camera.main.WorldToScreenPoint(ball.transform.position);
        if(screen.z>0) GUI.Label(new Rect(screen.x+8,Screen.height-screen.y-16,100,30),"● bi");
    }
}
