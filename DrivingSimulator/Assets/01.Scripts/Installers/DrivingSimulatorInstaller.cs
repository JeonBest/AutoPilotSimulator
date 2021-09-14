using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class DrivingSimulatorInstaller : MonoInstaller
{
    [SerializeField]
    GuidePivotManager _trackManager = null;


    public override void InstallBindings()
    {


        AppInstaller.PrintSystemInfo();
    }

}
