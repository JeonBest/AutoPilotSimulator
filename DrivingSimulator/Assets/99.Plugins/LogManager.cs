// System
using System;
using System.IO;
using System.Diagnostics;

using System.Collections;
using System.Collections.Generic;
using NWH.Common.Input;

// Unity
using UnityEngine;


// Project
// Alias
namespace NWH.VehiclePhysics2.Input
{
    public class LogManager : InputProvider
    {
        // public readonly variables
        private static string FilePath;
        private static readonly string TimeFormat = "yyyy/MM/dd HH:mm:ss";

        // private cached variables
        private static string currentTime = string.Empty;
        private static string callerName = string.Empty;
        private static string logContentMerged = string.Empty;
        string userID;

        float curveRate;
        int curveNum;
        int brNum;
        public bool isGood;

        VehicleController myvehicle;
        bool logging;

        new void Awake()
        {
            string flag;
            if (isGood) flag = "G";
            else flag = "B";
            brNum = 0;
            curveNum = 0;
            myvehicle = GetComponent<VehicleController>();
            userID = UnityEngine.Random.Range(0, 999999).ToString("D6");
            FilePath = Application.persistentDataPath + "/" + userID + flag + "_log.txt";
            Log("Hello " + userID);
            Log("Velocity\tSteering");
            UnityEngine.Debug.Log(Application.persistentDataPath);
            StartCoroutine("TimeLog");
            StartCoroutine("curveSaver");
        }

        void Update()
        {
            if (!logging && (1 == 2))
            {
                if (myvehicle.input.Brakes > 0.9f)
                {
                    logging = true;
                    brNum++;
                    //                Log($"Brake {brNum}");
                    Invoke("setLog", 2.0f);
                }

                if (myvehicle.input.Steering - curveRate > 0.5f || myvehicle.input.Steering - curveRate < -0.5f)
                {
                    logging = true;
                    curveNum++;
                    //              Log($"Steer {curveNum} : {curveRate} to {myvehicle.input.Steering}");
                    Invoke("setLog", 2.0f);
                }
            }
        }

        void setLog()
        {
            logging = false;
        }

        IEnumerator curveSaver()
        {
            curveRate = myvehicle.input.Steering;
            yield return new WaitForSeconds(0.5f);
            StartCoroutine("curveSaver");
        }

        IEnumerator TimeLog()
        {
            currentTime = GetCurrentTimeFormatted();
            float velo = myvehicle.LocalForwardVelocity;
            float steer = myvehicle.input.Steering;

            logContentMerged = $"[{currentTime}]\t{velo}\t{steer}";
            //            UnityEngine.Debug.Log(logContentMerged);
            SaveToFile(logContentMerged);
            yield return new WaitForSeconds(1.0f);
            StartCoroutine("TimeLog");
        }

        public void Log(object content)
        {
            currentTime = GetCurrentTimeFormatted();
            callerName = GetCallerName();
            logContentMerged = $"[{currentTime}]\t{content}";

            //          UnityEngine.Debug.Log(logContentMerged);
            SaveToFile(logContentMerged);
        }



        // TODO : Set delete

        private static void SaveToFile(string content)
        {
            using (StreamWriter writer = new StreamWriter(FilePath, true))
            {
                writer.WriteLine(logContentMerged);
            }

        }

        private static string GetCallerName()
        {
            StackTrace stackTrace = new StackTrace(true);
            StackFrame stackFrame = stackTrace.GetFrame(2);
            string className = stackFrame.GetMethod().DeclaringType.Name;
            string methodName = stackFrame.GetMethod().Name;
            int lineNumber = stackFrame.GetFileLineNumber();

            return $"{className}.{methodName} (line:{lineNumber})";
        }

        private static string GetCurrentTimeFormatted()
        {
            return DateTime.Now.ToString(TimeFormat);
        }
    }
}