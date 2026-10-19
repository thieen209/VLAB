using System;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public sealed class VlabProjectile : MonoBehaviour
{
    [SerializeField] private float diameter=.016f;
    private Vector3 origin;
    private Vector3 initialVelocity;
    private double gravity;
    private double duration;
    public bool IsFlying { get; private set; }
    public bool IsPaused { get; set; }
    public double Elapsed { get; private set; }
    public float Diameter => diameter;
    public Vector3 Velocity { get; private set; }
    public Vector3 ImpactVelocity { get; private set; }
    public double HorizontalRange { get; private set; }
    public event Action<Vector3> FirstContact;

    private static bool Finite(double value) => !double.IsNaN(value)&&!double.IsInfinity(value);
    private static bool Finite(Vector3 value) => Finite(value.x)&&Finite(value.y)&&Finite(value.z);
    private void Awake() => SetDiameter(diameter);

    public void SetDiameter(float meters)
    {
        if(!Finite(meters)||meters<.012f||meters>.020f) throw new ArgumentOutOfRangeException(nameof(meters));
        if(IsFlying) throw new InvalidOperationException("Reset flight before changing diameter.");
        diameter=meters;
        transform.localScale=Vector3.one*(meters/.016f);
        GetComponent<SphereCollider>().radius=.008f;
        GetComponent<SphereCollider>().center=Vector3.zero;
    }

    public bool Launch(Vector3 position,Vector3 velocity,double gravityMetersPerSecondSquared=9.81,double landingCenterY=0)
    {
        if(!Finite(position)||!Finite(velocity)||!Finite(gravityMetersPerSecondSquared)||gravityMetersPerSecondSquared<=0||
           !Finite(landingCenterY)||position.y<landingCenterY) throw new ArgumentOutOfRangeException("Invalid projectile conditions.");
        if(IsFlying) return false;
        origin=position; initialVelocity=velocity; gravity=gravityMetersPerSecondSquared;
        double height=position.y-landingCenterY;
        double discriminant=Math.Sqrt(velocity.y*velocity.y+2*gravity*height);
        duration=velocity.y>=0?(velocity.y+discriminant)/gravity:2*height/(discriminant-velocity.y);
        Elapsed=0; HorizontalRange=0; ImpactVelocity=Vector3.zero;
        IsPaused=false; IsFlying=true; Velocity=velocity;
        transform.position=position;
        return true;
    }

    public void Advance(double seconds)
    {
        if(!Finite(seconds)||seconds<0) throw new ArgumentOutOfRangeException(nameof(seconds));
        if(!IsFlying||IsPaused||seconds==0) return;
        Elapsed=Math.Min(duration,Elapsed+seconds);
        double t=Elapsed;
        var position=new Vector3((float)(origin.x+initialVelocity.x*t),(float)(origin.y+initialVelocity.y*t-.5*gravity*t*t),(float)(origin.z+initialVelocity.z*t));
        Velocity=new Vector3(initialVelocity.x,(float)(initialVelocity.y-gravity*t),initialVelocity.z);
        transform.position=position;
        if(Elapsed>=duration)
        {
            HorizontalRange=new Vector2(position.x-origin.x,position.z-origin.z).magnitude;
            ImpactVelocity=Velocity; Velocity=Vector3.zero; IsFlying=false;
            FirstContact?.Invoke(position);
        }
    }

    public void ResetFlight() { IsFlying=false; IsPaused=false; Elapsed=0; HorizontalRange=0; Velocity=ImpactVelocity=Vector3.zero; transform.position=origin; }
    private void FixedUpdate() => Advance(Time.fixedDeltaTime);
}
