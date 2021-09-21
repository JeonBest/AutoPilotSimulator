using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class SimulatorInstaller : MonoInstaller
{
    GuidePivotManager _trackManager;
    CarMover _carMover;

    public override void InstallBindings()
    {
        _trackManager = FindObjectOfType<GuidePivotManager>();
        _carMover = FindObjectOfType<CarMover>();

        Container.Bind<GuidePivotManager>().FromInstance(_trackManager);
        Container.Bind<CarMover>().FromInstance(_carMover);

        AppInstaller.PrintSystemInfo();
    }

}
