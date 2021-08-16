using System;
using UnityEngine;

namespace NWH.WheelController3D
{
    [Serializable]
    public class StandardFrictionModel : IFrictionModel
    {
        private Vector2 _combinedSlip;
        private Vector2 _slipDir;


        public void SlipCircle(ref float Sx, ref float Sy, ref float Fx, ref float Fy, float slipCircleShape)
        {
            float SxAbs = Sx < 0 ? -Sx : Sx;
            if (SxAbs > 0.01f)
            {
                float SxClamped = Sx < -1f ? -1f : Sx > 1f ? 1f : Sx;
                float SyClamped = Sy < -1f ? -1f : Sy > 1f ? 1f : Sy;
                _combinedSlip.x = SxClamped * slipCircleShape;
                _combinedSlip.y = SyClamped;
                _slipDir        = _combinedSlip.normalized;

                float F           = Mathf.Sqrt(Fx * Fx + Fy * Fy);
                float absSlipDirY = _slipDir.y < 0 ? -_slipDir.y : _slipDir.y;
                Fy = F * absSlipDirY * (Fy < 0 ? -1f : 1f);
            }
        }


        public void StepLongitudinal(float Tm, float Tb, float Vx, float Vy, ref float W, float Lc, float dt, float R,
            float I,
            AnimationCurve frictionCurve, float BCDEz, float kFx, float kSx, ref float Sx, ref float Fx, ref float Tcnt)
        {
            if (dt < 0.00001f) dt = 0.00001f;

            if (I < 0.0001f) I = 0.0001f;

            float Winit = W;
            float VxAbs = Vx < 0 ? -Vx : Vx;
            float WAbs  = W < 0 ? -W : W;

            if (VxAbs >= 0.1f)
                Sx = (Vx - W * R) / VxAbs;
            else
                Sx = (Vx - W * R) * 0.6f;

            Sx *= kSx;
            Sx =  Sx < -1f ? -1f : Sx > 1f ? 1f : Sx;

            W += Tm / I * dt;

            Tb = Tb * (W > 0 ? -1f : 1f);
            float TbCap = (W < 0 ? -W : W) * I / dt;
            float Tbr   = (Tb < 0 ? -Tb : Tb) - (TbCap < 0 ? -TbCap : TbCap);
            Tbr = Tbr < 0 ? 0 : Tbr;
            Tb = Tb > TbCap  ? TbCap :
                 Tb < -TbCap ? -TbCap : Tb;
            W += Tb / I * dt;

            float maxTorque   = frictionCurve.Evaluate(Sx < 0f ? -Sx : Sx) * Lc * kFx * R;
            float errorTorque = (W - Vx / R) * I / dt;
            float surfaceTorque = errorTorque < -maxTorque ? -maxTorque :
                                  errorTorque > maxTorque  ? maxTorque : errorTorque;

            W  -= surfaceTorque / I * dt;
            Fx =  surfaceTorque / R;

            Tbr = Tbr * (W > 0 ? -1f : 1f);
            float TbCap2 = (W < 0 ? -W : W) * I / dt;
            float Tb2 = Tbr > TbCap2  ? TbCap2 :
                        Tbr < -TbCap2 ? -TbCap2 : Tbr;
            W += Tb2 / I * dt;

            float deltaOmegaTorque = (W - Winit) * I / dt;
            Tcnt = -surfaceTorque + Tb + Tb2 - deltaOmegaTorque;

            // Force 0 slip when in air.
            if (Lc < 0.001f) Sx = 0;
        }


        public void StepLateral(float Vx, float Vy, float Lc, float dt, AnimationCurve frictionCurve, float BCDEz,
            float                     kFy,
            float                     kSy, ref float Sy, ref float Fy)
        {
            if (dt < 1e-6) return;

            float VxAbs = Vx < 0 ? -Vx : Vx;

            if (VxAbs > 0.3f)
            {
                Sy =  Mathf.Atan(Vy / VxAbs) * Mathf.Rad2Deg;
                Sy /= 50f;
            }
            else
                Sy = Vy * (0.003f / dt);

            Sy *= kSy * 0.95f;
            Sy =  Sy < -1f ? -1f : Sy > 1f ? 1f : Sy;
            float slipSign = Sy < 0 ? -1f : 1f;
            Fy = -slipSign * frictionCurve.Evaluate(Sy < 0 ? -Sy : Sy) * Lc * kFy;

            // Force 0 slip when in air.
            if (Lc < 0.0001f) Sy = 0;
        }
    }
}