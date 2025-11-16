using System.Collections;
using DNExtensions.ObjectPooling;
using UnityEngine;

public class OneShotSfx : MonoBehaviour, IPooledObject
{
    public AudioSource audioSource;

    
    public void Play(AudioClip clip)
    {
        if (!audioSource) return;
        
        audioSource.clip = clip;
        audioSource.Play();
        
        float duration = audioSource.clip.length;
        StartCoroutine(ReturnAfter(duration));
    }
    
    public void Play(AudioClip clip, Vector3 position)
    {
        if (!audioSource) return;
        
        audioSource.clip = clip;
        transform.position = position;
        audioSource.Play();
        
        float duration = audioSource.clip.length;
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
        if (audioSource) audioSource.Stop();
    }

    public void OnPoolRecycle()
    {
        if (audioSource) audioSource.Stop();
    }
}