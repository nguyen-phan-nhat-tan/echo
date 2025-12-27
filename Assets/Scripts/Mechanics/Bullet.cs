using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 10f;
    public bool isEnemyBullet = false;
    
    void OnEnable() 
    {
        StartCoroutine(DeactivateRoutine());
    }

    void Update()
    {
        transform.Translate(Vector3.up * speed * Time.deltaTime);
    }

    IEnumerator DeactivateRoutine()
    {
        yield return new WaitForSeconds(lifeTime);
        gameObject.SetActive(false); 
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Wall"))
        {
            gameObject.SetActive(false);
            return;
        }

        if (isEnemyBullet)
        {
            if (other.CompareTag("Player"))
            {
                Debug.Log("Game Over! Player Hit.");
                GameManager.Instance.EndLoop(false); 
                gameObject.SetActive(false); 
            }
        }
        else
        {
            if (other.CompareTag("Enemy"))
            {
                EchoController echo = other.GetComponent<EchoController>();
                if (echo != null) echo.Die();
            
                gameObject.SetActive(false);
                GameManager.Instance.CheckWinCondition();
            }
        }
    }
}