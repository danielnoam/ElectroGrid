using UnityEngine;

public class ClickerManager : MonoBehaviour
{
    public static ClickerManager Instance { get; private set; }
    
    
    private void Awake()
    {
        if (!Instance || Instance == this)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
}
