using System.Collections;
using DNExtensions.ObjectPooling;
using UnityEngine;

public class OneShotParticle : MonoBehaviour, IPooledObject
{
    public ParticleSystem particle;

    
    public void Play()
    {
        if (!particle) return;
        
        particle.Play();
        
        float duration = particle.main.duration + particle.main.startLifetime.constantMax;
        StartCoroutine(ReturnAfter(duration));
    }
    
    public void Play(Vector3 position)
    {
        if (!particle) return;
        
        transform.position = position;
        particle.Play();
        
        float duration = particle.main.duration + particle.main.startLifetime.constantMax;
        StartCoroutine(ReturnAfter(duration));
    }
    
    private IEnumerator DestroyAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
    
    private IEnumerator ReturnAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        ObjectPooler.ReturnObjectToPool(gameObject);
    }

    public void OnPoolGet()
    {
        
    }

    public void OnPoolReturn()
    {
        if (particle) particle.Clear(true);
    }

    public void OnPoolRecycle()
    {
        if (particle) particle.Clear(true);
    }
}