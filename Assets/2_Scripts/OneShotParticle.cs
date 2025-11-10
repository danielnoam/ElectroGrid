using System.Collections;
using UnityEngine;

public class OneShotParticle : MonoBehaviour
{
    public ParticleSystem particle;

    
    public void Play()
    {
        if (!particle) return;
        
        particle.Play();
        
        float duration = particle.main.duration + particle.main.startLifetime.constantMax;
        StartCoroutine(DestroyAfter(duration));
    }
    
    public void Play(Vector3 position)
    {
        if (!particle) return;
        
        transform.position = position;
        particle.Play();
        
        float duration = particle.main.duration + particle.main.startLifetime.constantMax;
        StartCoroutine(DestroyAfter(duration));
    }
    
    private IEnumerator DestroyAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}