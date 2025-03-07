using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] protected float Damage;
    [SerializeField] protected float ReloadTime;
    [SerializeField] protected float UnloadTime;
    [SerializeField] protected float CurrentAmmo;
    [SerializeField] protected int MaxAmmo;
    protected int Ammo;
    protected float CurrReloadTime;
    protected float CurrUnloadTime;
    
    protected List<int> ReturnAmmoStats() {
        return new List<int> {Ammo, MaxAmmo};
    }
 }
