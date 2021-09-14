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
    private int _dimCount;
    private bool _isiOSDevice;

    public override void InstallBindings()
    {
        Container.Bind<AppEvent>().FromNew().AsSingle();
        Container.Bind<LoadingSceneManager>().FromInstance(new LoadingSceneManager(this));
    }

    public override void Start()
    {
        base.Start();

#if UNITY_EDITOR
        _isiOSDevice = false;
#else
        _isiOSDevice = ExternalLibrary.IsiOSDevice();
        ExternalLibrary.registerVisibilityChangeEvent();
#endif

        // ExternalLibrary.ClearIndexedDB();
        PrintSystemInfo();
    }

    public static void PrintSystemInfo()
    {
        // NOTE Test Script
        Debug.Log($"appinstaller.start screen.width:{Screen.width}, screen.height:{Screen.height}\n" +
        $"resolution:{Screen.currentResolution}, frame:{Application.targetFrameRate}\n" +
        $"IsiOS:{ExternalLibrary.IsiOSDevice()}");
    }


    void OnVisibilityChange(string visibilityState)
    {
        if (_isiOSDevice && visibilityState == "visible" && _dimCount <= 0)
        {
            ExternalLibrary.ActivateDim();
            _dimCount += 1;
            System.Console.WriteLine($"Dim Count : {_dimCount}");
        }
    }

    void OnDimClicked()
    {
        _dimCount -= 1;
    }
}