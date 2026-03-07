using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class AttackStat
{
    [field: SerializeField] public float AttackRadius { get; private set; }
    [field: SerializeField] public float AttackKnock { get; private set; }
    [field: SerializeField] public float AttackForce { get; private set; }
    [field: SerializeField] public float AttackDamage { get; private set; }
    [field: SerializeField] public float AttackCooldown { get; private set; }
    public bool OnCooldown { get; private set; }

    float timer;

    public AttackStat()
    {
        AttackRadius = 0;
        AttackKnock = 0;
        AttackForce = 0;
        AttackDamage = 0;
        AttackCooldown = 0;
    }

    public AttackStat(AttackStat stat, float attackDamage)
    {
        AttackRadius = stat.AttackRadius;
        AttackKnock = stat.AttackKnock;
        AttackForce = stat.AttackForce;
        AttackDamage = attackDamage;
        AttackCooldown = stat.AttackCooldown;
    }

    public IEnumerator CountdownCooldown()
    {
        OnCooldown = true;
        timer = AttackCooldown;

        while (timer > 0)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        OnCooldown = false;
    }
}
