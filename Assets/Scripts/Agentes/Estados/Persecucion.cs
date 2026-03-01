using UnityEngine;

public class Persecucion : GuardBehavior
{
    public GuardVision sensor;

    void Start()
    {
        if (sensor == null)
        {
            sensor = GetComponent<GuardVision>();
        }
    }

    public override bool CanActivate()
    {
        if (sensor != null)
        {
            return sensor.PuedeVerAlLadron();
        }
        return false;
    }

    public override void Action()
    {
        agent.speed = 8.0f;
        agent.SetDestination(thief.position);
    }
}