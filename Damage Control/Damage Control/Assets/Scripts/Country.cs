﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Country : MonoBehaviour
{
    public CountryName countryName;
    [Range(0, 100)]
    public int friendliness = 100;
    public Plane planePrefab;
    public CountryUI countryUI;
    public PopupSO hostilePopup;
    public Airport[] blockableAirports;
    public GameObject noFlyZone;

    public bool IsHostile { get { return friendliness == 0; } }

    private WaitSlot[] waitSlots;


    private void Awake()
    {
        waitSlots = GetComponentsInChildren<WaitSlot>();
    }


    private void Start()
    {
        countryUI.SetFriendliness(friendliness);
        countryUI.SetName(name);
        noFlyZone.SetActive(false);
    }


    public WaitSlot GetAvailableWaitSlot()
    {
        foreach(WaitSlot waitSlot in waitSlots)
        {
            if (!waitSlot.isOccupied)
            {
                return waitSlot;
            }
        }
        return null;
    }


    public void DecreaseFrendliness(int amount)
    {
        if (IsHostile) return;
        friendliness -= amount;
        friendliness = Mathf.Clamp(friendliness, 0, 100);
        countryUI.SetFriendliness(friendliness);
        Debug.Log(name + "'s relationship with your country has been decreased by " + amount + " to " + friendliness);
        if (friendliness == 0)
        {
            Debug.Log("They are now hostile!");
            CreateNoFlyZone();
            PopupManager.instance.ShowPopup(hostilePopup);
        }
    }

    private void CreateNoFlyZone()
    {
        Debug.Log("no fly zone created");
        noFlyZone.SetActive(true);

        foreach (Airport airport in blockableAirports)
        {
            airport.PutInNoFlyZone();
        }

    }
}