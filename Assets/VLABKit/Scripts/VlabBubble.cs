using System;
using UnityEngine;

public sealed class VlabBubble : MonoBehaviour
{
    public const int MaxLive=64;
    public const float MaxRate=20,RiseSpeed=.02f;
    [SerializeField] private ParticleSystem particles;
    public ParticleSystem Particles=>particles;
    public float Rate {get;private set;}
    public float RiseHeight {get;private set;}=.04f;
    private void Awake()
    {
        var main=particles.main;main.maxParticles=MaxLive;
        SetRiseHeight(RiseHeight);Stop(true);
    }
    public void SetRiseHeight(float height)
    {
        if(!float.IsFinite(height)||height<=0||height>.1f)throw new ArgumentOutOfRangeException(nameof(height));
        particles.Clear();RiseHeight=height;var main=particles.main;main.startLifetime=height/RiseSpeed;
    }
    public void SetEmission(float requestedRate)
    {
        if(!float.IsFinite(requestedRate)||requestedRate<0)throw new ArgumentOutOfRangeException(nameof(requestedRate));
        if(!isActiveAndEnabled)throw new InvalidOperationException("Enable the effect before emitting.");
        Rate=Mathf.Min(requestedRate,MaxRate);var emission=particles.emission;emission.rateOverTime=Rate;
        if(Rate>0)particles.Play();else particles.Stop(true,ParticleSystemStopBehavior.StopEmitting);
    }
    public void Stop(bool clear)
    {
        Rate=0;var emission=particles.emission;emission.rateOverTime=0;
        particles.Stop(true,clear?ParticleSystemStopBehavior.StopEmittingAndClear:ParticleSystemStopBehavior.StopEmitting);
    }
    private void OnDisable(){if(particles!=null)Stop(true);}
}
