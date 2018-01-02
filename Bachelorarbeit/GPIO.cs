using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace Bachelorarbeit
{
    class GPIO
    {
        //Ausgangs GPIOs
        //Horizontaler Motor
        public GpioPin p_fwd_pin;
        public GpioPin p_back_pin;
        public GpioPin p_enable_pin;

        //Vertikaler Motor
        public GpioPin z_up_pin;
        public GpioPin z_down_pin;
        public GpioPin z_enable_pin;

        //Motor zur Drehung um die Mittelachse
        public GpioPin phi_right_pin;
        public GpioPin phi_left_pin;
        public GpioPin phi_enable_pin;

        //Motor zum Öffnen/Schließen der Greifzange
        public GpioPin grip_open_pin;
        public GpioPin grip_close_pin;
        public GpioPin grip_enable_pin;

        //Eingangs GPIOs
        //Schrittzähler Horizontale Achse
        public GpioPin p_step_pin;

        //Inkrementalgeber Vertikale Achse
        public GpioPin z_ink1_pin;
        public GpioPin z_ink2_pin;

        //Inkrementalgeber der Drehung
        public GpioPin phi_ink1_pin;
        public GpioPin phi_ink2_pin;

        //Schrittzähler des Greifers
        public GpioPin grip_step_pin;

        //Kalibrierungsschalter
        public GpioPin p_cal_pin;
        public GpioPin z_cal_pin;
        public GpioPin phi_cal_pin;
        public GpioPin grip_cal_pin;

        public GPIO()
        {
            InitGPIO();
        }

        private void InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                return;
            }

            //Ausgangs GPIOs öffnen
            p_enable_pin = gpio.OpenPin(def.P_ENABLE_PIN);
            p_fwd_pin = gpio.OpenPin(def.P_FWD_PIN);
            p_back_pin = gpio.OpenPin(def.P_BACK_PIN);

            z_enable_pin = gpio.OpenPin(def.Z_ENABLE_PIN);
            z_up_pin = gpio.OpenPin(def.Z_UP_PIN);
            z_down_pin = gpio.OpenPin(def.Z_DOWN_PIN);

            phi_enable_pin = gpio.OpenPin(def.PHI_ENABLE_PIN);
            phi_left_pin = gpio.OpenPin(def.PHI_LEFT_PIN);
            phi_right_pin = gpio.OpenPin(def.PHI_RIGHT_PIN);

            grip_enable_pin = gpio.OpenPin(def.GRIP_ENABLE_PIN);
            grip_open_pin = gpio.OpenPin(def.GRIP_OPEN_PIN);
            grip_close_pin = gpio.OpenPin(def.GRIP_CLOSE_PIN);

            //Eingangs GPIOs öffnen
            p_step_pin = gpio.OpenPin(def.P_STEP_PIN);
            z_ink1_pin = gpio.OpenPin(def.Z_INK1_PIN);
            z_ink2_pin = gpio.OpenPin(def.Z_INK2_PIN);
            phi_ink1_pin = gpio.OpenPin(def.PHI_INK1_PIN);
            phi_ink2_pin = gpio.OpenPin(def.PHI_INK2_PIN);
            grip_step_pin = gpio.OpenPin(def.GRIP_STEP_PIN);

            p_cal_pin = gpio.OpenPin(def.P_CAL_PIN);
            z_cal_pin = gpio.OpenPin(def.Z_CAL_PIN);
            phi_cal_pin = gpio.OpenPin(def.PHI_CAL_PIN);
            grip_cal_pin = gpio.OpenPin(def.GRIP_CAL_PIN);

            //Ausgangs GPIOs konfigurieren und initialisieren
            SetGpioOut(z_enable_pin);
            SetGpioOut(z_up_pin);
            SetGpioOut(z_down_pin);

            SetGpioOut(phi_enable_pin);
            SetGpioOut(phi_left_pin);
            SetGpioOut(phi_right_pin);

            SetGpioOut(p_enable_pin);
            SetGpioOut(p_fwd_pin);
            SetGpioOut(p_back_pin);

            SetGpioOut(grip_enable_pin);
            SetGpioOut(grip_open_pin);
            SetGpioOut(grip_close_pin);

            //Eingangs GPIOs konfigurieren, entprellen und Evnetlistener Registrieren

            SetGpioIn(z_cal_pin);
            SetGpioIn(z_ink1_pin, def.input_timeout);
            SetGpioIn(z_ink2_pin, def.input_timeout);
            z_cal_pin.ValueChanged += z_cal_pin_ValueChanged;
            z_ink1_pin.ValueChanged += z_ink1_pin_ValueChanged;
            z_ink2_pin.ValueChanged += z_ink2_pin_ValueChanged;

            SetGpioIn(phi_cal_pin);
            SetGpioIn(phi_ink1_pin, def.input_timeout);
            SetGpioIn(phi_ink2_pin, def.input_timeout);
            phi_cal_pin.ValueChanged += phi_cal_pin_ValueChanged;
            phi_ink1_pin.ValueChanged += phi_ink1_pin_ValueChanged;
            phi_ink2_pin.ValueChanged += phi_ink2_pin_ValueChanged;

            SetGpioIn(p_cal_pin);
            SetGpioIn(p_step_pin, def.input_timeout);
            p_cal_pin.ValueChanged += p_cal_pin_ValueChanged;
            p_step_pin.ValueChanged += p_step_pin_ValueChanged;

            SetGpioIn(grip_cal_pin);
            SetGpioIn(grip_step_pin, def.input_timeout);
            grip_cal_pin.ValueChanged += grip_cal_pin_ValueChanged;
            grip_step_pin.ValueChanged += grip_step_pin_ValueChanged;

        }

        private void z_cal_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (z_cal_pin.Read() == GpioPinValue.High)
            {
                MainPage.StopZ();

                if (inCalibration == true)
                {
                    //vert_cal = false;
                    //if(vert_cal == turn_cal == false)
                    inCalibration = false;
                    Null_Z();
                }
                else
                {
                    Calibrate_Z();
                }
            }

            if (z_cal_pin.Read() == GpioPinValue.Low)
            {
                StopZ();
                z_act = 0;
                emergencyStop = false;
                if (z_null == true)
                {

                    z_null = false;
                    calibrationFinished = true;

                }
                else
                {
                    Calibrate();
                }
            }
        }

        private void phi_cal_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (phi_cal_pin.Read() == GpioPinValue.High)
            {
                StopPhi();

                if (inCalibration == false)
                {
                    inCalibration = false;
                    Null_Phi();
                }
                else
                {
                    Calibrate_Phi();
                }
            }

            if (phi_cal_pin.Read() == GpioPinValue.Low)
            {
                StopPhi();
                phi_act = 0;
                emergencyStop = false;
                if (phi_null == true)
                {
                    phi_null = false;
                    Calibrate_Z();
                }
                else
                {
                    Calibrate();
                }
            }
        }

        private void p_cal_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (p_cal_pin.Read() == GpioPinValue.High)
            {
                StopP();

                if (inCalibration == false)
                {
                    inCalibration = false;
                    Null_P();
                }
                else
                {
                    Calibrate_P();
                }
            }

            if (p_cal_pin.Read() == GpioPinValue.Low)
            {
                StopP();
                p_act = 0;
                emergencyStop = false;
                if (p_null == true)
                {
                    p_null = false;
                    Calibrate_Grip();
                }
                else
                {
                    Calibrate();
                }
            }
        }

        private void grip_cal_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (grip_cal_pin.Read() == GpioPinValue.High)
            {
                StopGrip();

                if (inCalibration == false)
                {
                    inCalibration = false;
                    Null_Grip();
                }
                else
                {
                    Calibrate_Grip();
                }
            }

            if (grip_cal_pin.Read() == GpioPinValue.Low)
            {
                StopGrip();
                grip_act = 0;
                emergencyStop = false;
                if (grip_null == true)
                {

                    grip_null = false;
                    Calibrate_Phi();
                }
                else
                {
                    Calibrate();
                }
            }
        }

        private void z_ink1_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (z_ink1_pin.Read() == GpioPinValue.High)
            {
                //if (vert_ink2_pressed == true)
                //{
                //    if (vert_ink2_pin.Read() == GpioPinValue.High)
                //        vert_act--;
                //    else
                //        vert_act++;
                //    actual.setZ(vert_act);
                //}
                if (z_up_pin.Read() == GpioPinValue.High)
                    z_act--;
                else if (z_down_pin.Read() == GpioPinValue.High)
                    z_act++;
                actual.setZ(z_act);
                z_ink2_pressed = false;
            }
        }

        private void z_ink2_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (z_ink2_pin.Read() == GpioPinValue.High)
                z_ink2_pressed = true;
        }

        private void phi_ink1_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (phi_ink1_pin.Read() == GpioPinValue.High)
            {
                //if (turn_ink2_pressed == true)
                //{
                //    if (turn_ink2_pin.Read() == GpioPinValue.High)
                //        turn_act--;
                //    else
                //        turn_act++;
                //    actual.setPhi(turn_act);
                //}
                if (phi_right_pin.Read() == GpioPinValue.High)
                    phi_act--;
                else if (phi_left_pin.Read() == GpioPinValue.High)
                    phi_act++;
                actual.setPhi(phi_act);
                phi_ink2_pressed = false;
            }
        }

        private void phi_ink2_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (phi_ink2_pin.Read() == GpioPinValue.High)
                phi_ink2_pressed = true;
        }

        private void p_step_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (p_step_pin.Read() == GpioPinValue.High)
            {
                if (p_fwd_pin.Read() == GpioPinValue.High)
                {
                    p_act++;
                }
                else if (p_back_pin.Read() == GpioPinValue.High)
                {
                    p_act--;
                }
                actual.setP(p_act);
            }
        }

        private void grip_step_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (grip_step_pin.Read() == GpioPinValue.High)
            {
                if (grip_close_pin.Read() == GpioPinValue.High)
                {
                    grip_act++;
                }
                else if (grip_open_pin.Read() == GpioPinValue.Low)
                {
                    grip_act--;
                }
            }
        }

        private void SetGpioOut(GpioPin pin)
        {
            pin.SetDriveMode(GpioPinDriveMode.Output);
            PinLow(pin);
        }

        private void SetGpioIn(GpioPin pin, double debounce = 50)
        {
            pin.SetDriveMode(GpioPinDriveMode.Input);
            pin.DebounceTimeout = TimeSpan.FromMilliseconds(debounce);
        }

        public void PinLow(GpioPin pin)
        {
            pin.Write(GpioPinValue.Low);
        }

        public void PinHigh(GpioPin pin)
        {
            pin.Write(GpioPinValue.High);
        }
    }
}
