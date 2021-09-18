using UniRx;
using UnityEngine;
using Zenject;

public class AppEvent
{
    public BehaviorSubject<bool> OnPauseSubject { get; private set; } = new BehaviorSubject<bool>(false);
    public BehaviorSubject<bool> OnSwitchSubject { get; private set; } = new BehaviorSubject<bool>(true);
    public BehaviorSubject<bool> OnFinishLoadAppConfigSubject { get; private set; } = new BehaviorSubject<bool>(false);
}

public class AppInstaller : MonoInstaller
{

    public override void InstallBindings()
    {
        Container.Bind<AppEvent>().FromNew().AsSingle();
    }

    public override void Start()
    {
        base.Start();

        // ExternalLibrary.ClearIndexedDB();
        PrintSystemInfo();
    }

    public static void PrintSystemInfo()
    {
        // NOTE Test Script
        Debug.Log($"appinstaller.start screen.width:{Screen.width}, screen.height:{Screen.height}\n" +
        $"resolution:{Screen.currentResolution}, frame:{Application.targetFrameRate}\n");
    }
}