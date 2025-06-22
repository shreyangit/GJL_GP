using UnityEngine;

public class ZombieAttackTrigger : MonoBehaviour
{
    [HideInInspector] public ZombieAttack parentAttack;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (parentAttack != null)
        {
            parentAttack.OnPlayerEnterRange(other);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (parentAttack != null)
        {
            parentAttack.OnPlayerExitRange(other);
        }
    }
}
