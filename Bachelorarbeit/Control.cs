using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bachelorarbeit
{
    class Control
    {
        private void CheckPositionZ()
        {
            //if (vert_act <= vert_soll - def.tube || vert_act <= 0)
            //    GoDown();
            //else if (vert_act >= vert_soll + def.tube || vert_act >= def.VERT_MAX)
            //    GoUp();
            //else if (vert_act < vert_soll + def.tube && vert_act > vert_soll - def.tube)
            //    StopVert();
            if (actual.getZ() >= def.Z_MAX - def.TUBE)
            {
                StopZ();
                setpoint.setZ(actual.getZ());
            }
            if (actual.getZ() <= setpoint.getZ() - def.TUBE || actual.getZ() <= 0)
            {
                GoDown();
                return;
            }
            else if (actual.getZ() >= setpoint.getZ() + def.TUBE || actual.getZ() >= def.Z_MAX)
            {
                GoUp();
                return;
            }
            else if (actual.getZ() < setpoint.getZ() + def.TUBE && actual.getZ() > setpoint.getZ() - def.TUBE)
                StopZ();


        }

        private void CheckPositionPhi()
        {
            if (actual.getPhi() <= setpoint.getPhi() - def.TUBE || actual.getPhi() <= 0)
                TurnLeft();
            else if (actual.getPhi() >= setpoint.getPhi() + def.TUBE || actual.getPhi() >= def.PHI_MAX)
                TurnRight();
            else if (actual.getPhi() < setpoint.getPhi() + def.TUBE && actual.getPhi() > setpoint.getPhi() - def.TUBE)
                StopPhi();
            else if (actual.getPhi() >= def.PHI_MAX - def.TUBE)
            {
                StopPhi();
                setpoint.setPhi(actual.getPhi());
            }
            else
                StopPhi();
        }

        private void CheckPositionP()
        {
            if (actual.getP() <= setpoint.getP() - def.TUBE || actual.getP() <= 0)
                GoFwd();
            else if (actual.getP() >= setpoint.getP() + def.TUBE || actual.getP() >= def.P_MAX)
                GoBack();
            else if (actual.getP() < setpoint.getP() + def.TUBE && actual.getP() > setpoint.getP() - def.TUBE)
                StopP();
            else if (actual.getP() >= def.P_MAX - def.TUBE)
            {
                StopP();
                setpoint.setP(actual.getP());
            }
            else
                StopP();
        }

        private void CheckPositionGrip()
        {
            if (grip_act <= grip_soll - def.TUBE || grip_act <= 0)
                CloseGrip();
            else if (grip_act >= grip_soll + def.TUBE || grip_act >= def.GRIP_MAX)
                OpenGrip();
            else if (grip_act < grip_soll + def.TUBE && grip_act > grip_soll - def.TUBE)
                StopGrip();
            else if (grip_act >= def.GRIP_MAX - def.TUBE)
            {
                StopGrip();
                grip_soll = grip_act;
            }
            else
                StopGrip();
        }

        private void CheckMaximumBorders()
        {
            if (actual.getP() >= def.P_MAX - def.TUBE)
                StopP();
            if (actual.getPhi() >= def.PHI_MAX - def.TUBE)
                StopPhi();
            if (actual.getZ() >= def.Z_MAX - def.TUBE)
                StopZ();
            if (grip_act >= def.GRIP_MAX - def.TUBE)
                StopGrip();
        }

        public void StopAll()
        {
            StopZ();
            StopP();
            StopPhi();
            StopGrip();
        }

        public void StopZ()
        {
            gpio.PinLow(gpio.z_enable_pin);
            gpio.PinLow(gpio.z_down_pin);
            gpio.PinLow(gpio.z_up_pin);
        }

        public void StopPhi()
        {
            gpio.PinLow(gpio.phi_enable_pin);
            gpio.PinLow(gpio.phi_left_pin);
            gpio.PinLow(gpio.phi_right_pin);
        }

        public void StopP()
        {
            gpio.PinLow(gpio.p_enable_pin);
            gpio.PinLow(gpio.p_fwd_pin);
            gpio.PinLow(gpio.p_back_pin);
        }

        public void StopGrip()
        {
            gpio.PinLow(gpio.grip_enable_pin);
            gpio.PinLow(gpio.grip_open_pin);
            gpio.PinLow(gpio.grip_close_pin);
        }

        public void GoUp()
        {
            gpio.PinHigh(gpio.z_enable_pin);
            gpio.PinHigh(gpio.z_up_pin);
            gpio.PinLow(gpio.z_down_pin);
        }

        public void GoDown()
        {
            gpio.PinHigh(gpio.z_enable_pin);
            gpio.PinHigh(gpio.z_down_pin);
            gpio.PinLow(gpio.z_up_pin);
        }

        public void TurnRight()
        {
            gpio.PinHigh(gpio.phi_enable_pin);
            gpio.PinHigh(gpio.phi_right_pin);
            gpio.PinLow(gpio.phi_left_pin);
        }

        public void TurnLeft()
        {
            gpio.PinHigh(gpio.phi_enable_pin);
            gpio.PinHigh(gpio.phi_left_pin);
            gpio.PinLow(gpio.phi_right_pin);
        }

        public void GoFwd()
        {
            gpio.PinHigh(gpio.p_enable_pin);
            gpio.PinHigh(gpio.p_fwd_pin);
            gpio.PinLow(gpio.p_back_pin);
        }

        public void GoBack()
        {
            gpio.PinHigh(gpio.p_enable_pin);
            gpio.PinLow(gpio.p_fwd_pin);
            gpio.PinHigh(gpio.p_back_pin);
        }

        public void OpenGrip()
        {
            gpio.PinHigh(gpio.grip_enable_pin);
            gpio.PinHigh(gpio.grip_open_pin);
            gpio.PinLow(gpio.grip_close_pin);
        }

        public void CloseGrip()
        {
            gpio.PinHigh(gpio.grip_enable_pin);
            gpio.PinLow(gpio.grip_open_pin);
            gpio.PinHigh(gpio.grip_close_pin);
        }

        public void Calibrate()
        {
            inCalibration = true;
            calibrationFinished = false;
            Calibrate_P();
        }

        public void Calibrate_P()
        {
            inCalibration = true;
            actual.setP(def.P_MAX);
            GoBack();
        }

        public void Calibrate_Z()
        {
            inCalibration = true;
            actual.setZ(def.Z_MAX);
            GoUp();
        }

        public void Calibrate_Phi()
        {
            inCalibration = true;
            actual.setPhi(def.PHI_MAX);
            TurnRight();
        }

        public void Calibrate_Grip()
        {
            inCalibration = true;
            grip_act = def.GRIP_MAX;
            OpenGrip();
        }

        public void Null_Z()
        {
            z_null = true;
            GoDown();
        }

        public void Null_Phi()
        {
            phi_null = true;
            TurnLeft();
        }

        public void Null_P()
        {
            p_null = true;
            GoFwd();
        }

        public void Null_Grip()
        {
            grip_null = true;
            CloseGrip();
        }
    }
}
