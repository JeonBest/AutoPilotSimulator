using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class SimulatorInstaller : MonoInstaller
{
    GuidePivotManager _trackManager;


    public override void InstallBindings()
    {
        _trackManager = FindObjectOfType<GuidePivotManager>();

        Container.Bind<GuidePivotManager>().FromInstance(_trackManager);

        AppInstaller.PrintSystemInfo();
    }

}
