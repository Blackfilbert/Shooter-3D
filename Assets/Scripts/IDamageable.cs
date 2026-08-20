using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int damage, DamageType damageType);
    void TakeDamage(int damage, DamageType damageType, Vector3 hitPoint, Vector3 hitDirection);
}
