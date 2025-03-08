using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [Header("Basic Stats")]
    [SerializeField] protected float Damage;
    [SerializeField] protected float ReloadTime;
    [SerializeField] protected float UnloadTime;
    [SerializeField] protected float CurrentAmmo;
    [SerializeField] protected int MaxAmmo;
    protected int Ammo;
    protected float CurrReloadTime;
    protected float CurrUnloadTime;
    bool IsReloading;
    
    protected List<int> ReturnAmmoStats() {
        return new List<int> {Ammo, MaxAmmo};
    }

    protected void CheckReload(bool ReloadInput) {
        if(!IsReloading && ReloadInput) {
            Reload();
        }

        if(IsReloading) {
            if(CurrReloadTime + ReloadTime <= Time.time) {
                Ammo = MaxAmmo;
                IsReloading = false;
            }
        }
    }

    protected void Reload() {
        CurrReloadTime = Time.time;
        IsReloading = true;
    }
 }
