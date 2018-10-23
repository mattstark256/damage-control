﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


// This is used for linking plane ScriptableObjects to countries
public enum CountryName { KingdomOfFuntz, Azerbania, Transchania, Bulukun, Lateria }


[RequireComponent(typeof(ArrivingPlanes), typeof(PlaneSelection), typeof(AirportManager))]
public class GameController : MonoBehaviour
{
    private ArrivingPlanes arrivingPlanes;
    private PlaneSelection planeSelection;
    private AirportManager airportManager;
    private PopupScheduler popupScheduler;

    [SerializeField, Tooltip("The duration between the end of one turn and the start of the next")]
    private float transitionDuration = 1f;
    [SerializeField]
    private GameObject pauseUI;
    [SerializeField]
    private PlaneInfo planeInfoPrefab;
    private List<PlaneInfo> planeInfos = new List<PlaneInfo>();
    [SerializeField]
    private RectTransform planeInfoParent;
    [SerializeField]
    private PopupSO losePopup;
    [SerializeField]
    private PopupSO winPopup;
    [SerializeField]
    private int hostileCountriesForGameOver = 1;
    [SerializeField]
    private int turnsForWin;

    [SerializeField]
    private Country[] countries;

    private int turnNumber = 0;
    [HideInInspector]
    public List<Plane> planes = new List<Plane>();
    public bool TransitionInProgress { get; private set; }


    private void Awake()
    {
        arrivingPlanes = GetComponent<ArrivingPlanes>();
        planeSelection = GetComponent<PlaneSelection>();
        airportManager = GetComponent<AirportManager>();
        popupScheduler = GetComponent<PopupScheduler>();
    }


    private void Start()
    {
        StartCoroutine(TransitionToNextTurn());
    }


    public void EndTurn()
    {
        StartCoroutine(TransitionToNextTurn());
    }


    private IEnumerator TransitionToNextTurn()
    {
        TransitionInProgress = true;

        pauseUI.SetActive(false);
        HidePlaneUI();

        List<Plane> rejectedPlanes = new List<Plane>(); // These planes will have a negative impact on your relationship with the country
        List<Plane> removedPlanes = new List<Plane>(); // This is used because you can't modify a list you are iterating through.
        foreach (Plane plane in planes)
        {
            if (plane.country.IsHostile) // Any remaining hostile planes can't be selected and go back home asap
            {
                MakePlaneLeave(plane);
                removedPlanes.Add(plane);
            }
            if (plane.isSelected)
            {
                MakePlaneLand(plane);
                removedPlanes.Add(plane);
            }
            else if (plane.RemainingWaits > 0)
            {
                MakePlaneWait(plane);
            }
            else
            {
                MakePlaneLeave(plane);
                removedPlanes.Add(plane);
                rejectedPlanes.Add(plane);
            }
        }
        foreach (Plane plane in removedPlanes) { planes.Remove(plane); }
        planeSelection.DeselectAll();
        airportManager.ClearAirportRunways();

        // send in new planes
        SpawnNewPlanes();

        // Wait until all animations are complete
        yield return new WaitForSeconds(transitionDuration);

        // Decrease your relationship with any countries you have rejected planes from
        foreach (Plane plane in rejectedPlanes) { FaceConsequencesForRejection(plane); }
        CheckForGameOver();

        ShowPlaneUI();
        pauseUI.SetActive(true);
        planeSelection.UpdateEndTurnButton();

        turnNumber++;
        popupScheduler.ShowPopupsForTurn(turnNumber);
        TransitionInProgress = false;
    }

    
    private void MakePlaneLand(Plane plane)
    {
        Airport airport = airportManager.GetNearestAirport(plane.transform.position);
        airport.runwayInUse = true;
        plane.Land(transitionDuration, airport);

    }


    private void MakePlaneWait(Plane plane)
    {
        plane.RemainingWaits--;
        // do a loop animation
    }


    private void MakePlaneLeave(Plane plane)
    {
        plane.Leave(transitionDuration);
    }


    private void SpawnNewPlanes()
    {
        PlaneSO[] planesToSpawn = arrivingPlanes.GetArrivals(turnNumber);
        foreach (PlaneSO planeSO in planesToSpawn)
        {
            Country country = GetCountryFromName(planeSO.countryName);
            if (country == null) continue; // This should never happen
            WaitSlot waitSlot = country.GetAvailableWaitSlot();
            if (waitSlot == null) { Debug.Log("No available waiting slots for plane " + planeSO.name); continue; }

            Plane newPlane = Instantiate(country.planePrefab);
            newPlane.transform.SetParent(waitSlot.transform, false);
            newPlane.planeSO = planeSO;
            newPlane.waitSlot = waitSlot;
            newPlane.country = country;
            waitSlot.isOccupied = true;
            planes.Add(newPlane);
            newPlane.Arrive(transitionDuration);
            //Debug.Log("Plane " + planeSO.name + " has been spawned");
        }
    }


    private Country GetCountryFromName(CountryName countryName)
    {
        foreach (Country country in countries)
        {
            if (country.countryName == countryName)
            {
                return country;
            }
        }
        Debug.Log("No country found with the name " + countryName);
        return null;
    }


    private void FaceConsequencesForRejection(Plane plane)
    {
        plane.country.DecreaseFrendliness(plane.planeSO.significanceValue);
    }


    private void ShowPlaneUI()
    {
        foreach (Plane plane in planes)
        {
            if (plane.country.IsHostile) continue;
            PlaneInfo planeInfo = Instantiate(planeInfoPrefab);
            planeInfo.transform.SetParent(planeInfoParent);
            planeInfo.transform.position = Camera.main.WorldToScreenPoint(plane.transform.position);
            planeInfo.ShowInfo(plane);
            planeInfos.Add(planeInfo);
        }
    }


    private void HidePlaneUI()
    {
        while(planeInfos.Count>0)
        {
            planeInfos[0].Dissappear();
            planeInfos.RemoveAt(0);
        }
    }


    private void CheckForGameOver()
    {
        int hostileCountries = 0;
        foreach(Country country in countries)
        {
            if (country == null) { continue; }
            if (country.IsHostile) { hostileCountries++; }
        }


        if (hostileCountries >= hostileCountriesForGameOver)
        {
            PopupManager.instance.ShowPopup(losePopup);
        }
        else if (turnNumber == turnsForWin)
        {
            PopupManager.instance.ShowPopup(winPopup);
        }
    }
}