using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WaypointNavigator : MonoBehaviour
{
    CharacterNavigationController controller;

    public Waypoint currentWaypoint;
    int direction;

    private void Awake()
    {
        controller = GetComponent<CharacterNavigationController>();
    }

    private void Start()
    {
        direction = Mathf.RoundToInt(Random.Range(0,2));
        controller.SetDestination(currentWaypoint.GetPosition());
    }

    private void Update()
    {
        if (currentWaypoint == null)
        {
            gameObject.SetActive(false); // Recycle for reuse
        }

        if (controller.reachedDestinations)
        {
            bool shouldBranch = currentWaypoint.branches != null && currentWaypoint.branches.Count > 0 && Random.value <= currentWaypoint.branchRatio;

            if (shouldBranch)
            {
                Waypoint branch = currentWaypoint.branches
                .Where(b => b != null)
                .OrderBy(b => Random.value)
                .FirstOrDefault();

                if (branch != null) currentWaypoint = branch;
            }
            else
            {
                if (direction == 0)
                {
                    if (currentWaypoint.nextWaypoint != null)
                    {
                        currentWaypoint = currentWaypoint.nextWaypoint;
                    }
                    else
                    {
                        currentWaypoint = currentWaypoint.previousWaypoint;
                        direction = 1;
                    }
                }
                else if (direction == 1)
                {
                    if (currentWaypoint.previousWaypoint != null)
                    {
                        currentWaypoint = currentWaypoint.previousWaypoint;
                    }
                    else
                    {
                        currentWaypoint = currentWaypoint.nextWaypoint;
                        direction = 0;
                    }
                }
            }         

            controller.SetDestination(currentWaypoint.GetPosition());
        }
    }
}
