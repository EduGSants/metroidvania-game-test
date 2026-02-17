using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] float health;
    void Start()
    {
        
    }

    void Update()
    {
        if(health <= 0)
        {
            Destroy(gameObject);
        }
    }

    public void EnemyHit(float _damageDone)
    {
        health -= _damageDone;
    }
}
