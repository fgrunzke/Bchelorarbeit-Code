using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Gpio;
using Windows.UI.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Bachelorarbeit
{
    public sealed partial class MainPage : Page
    {
        //Ausgangs GPIOs
        //Horizontaler Motor
        private GpioPin p_fwd_pin;
        private GpioPin p_back_pin;
        private GpioPin p_enable_pin;

        //Vertikaler Motor
        private GpioPin z_up_pin;
        private GpioPin z_down_pin;
        private GpioPin z_enable_pin;

        //Motor zur Drehung um die Mittelachse
        private GpioPin phi_right_pin;
        private GpioPin phi_left_pin;
        private GpioPin phi_enable_pin;

        //Motor zum Öffnen/Schließen der Greifzange
        private GpioPin grip_open_pin;
        private GpioPin grip_close_pin;
        private GpioPin grip_enable_pin;

        //Eingangs GPIOs
        //Schrittzähler Horizontale Achse
        private GpioPin p_step_pin;

        //Inkrementalgeber Vertikale Achse
        private GpioPin z_ink1_pin;
        private GpioPin z_ink2_pin;

        //Inkrementalgeber der Drehung
        private GpioPin phi_ink1_pin;
        private GpioPin phi_ink2_pin;

        //Schrittzähler des Greifers
        private GpioPin grip_step_pin;

        //Kalibrierungsschalter
        private GpioPin p_cal_pin;
        private GpioPin z_cal_pin;
        private GpioPin phi_cal_pin;
        private GpioPin grip_cal_pin;
        
        private DispatcherTimer timer = new DispatcherTimer();
        private DispatcherTimer timerGUI = new DispatcherTimer();

        private int p_act = def.P_MAX;
        private int z_act = def.Z_MAX;
        private int phi_act = def.PHI_MAX;
        private int grip_act = def.GRIP_MAX;

        private int grip_soll = def.GRIP_SOLL;

        private Point setpoint = new Point(def.P_SOLL, def.PHI_SOLL, def.Z_SOLL);
        private Point actual = new Point();

        private bool inCalibration = false;
        private bool calibrationFinished = false;
        private bool z_ink2_pressed = false;
        private bool z_null = true;
        private bool phi_ink2_pressed = false;
        private bool phi_null = true;
        private bool p_null = true;
        private bool grip_null = true;
        private bool emergencyStop = false;
        private bool wasPressed = false;

        private int gui_counter = 0;

        public MainPage()
        {
            InitializeComponent();
            InitGPIO();
            timer.Interval = TimeSpan.FromMilliseconds(50);
            timer.Tick += Timer_Tick;
            if (z_cal_pin != null)
            {
                timer.Start();
            }
            timerGUI.Interval = TimeSpan.FromMilliseconds(50);
            timerGUI.Tick += TimerGUI_Tick;
            timerGUI.Start();

            Calibrate();

            HorIn.Text = "" + def.P_SOLL;
            VertIn.Text = "" + def.Z_SOLL;
            TurnIn.Text = "" + def.PHI_SOLL;
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

        private void z_cal_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (z_cal_pin.Read() == GpioPinValue.High)
            {
                StopZ();

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

        private void Timer_Tick(Object sender, Object args)
        {
            if (z_cal_pin.Read() == GpioPinValue.High)
            {
                emergencyStop = true;
                calibrationFinished = false;
                StopZ();
                Null_Z();
            }

            if (phi_cal_pin.Read() == GpioPinValue.High)
            {
                emergencyStop = true;
                calibrationFinished = false;
                StopPhi();
                Null_Phi();
            }

            if (p_cal_pin.Read() == GpioPinValue.High)
            {
                emergencyStop = true;
                calibrationFinished = false;
                StopP();
                Null_P();
            }

            if (grip_cal_pin.Read() == GpioPinValue.High)
            {
                emergencyStop = true;
                calibrationFinished = false;
                StopGrip();
                Null_Grip();
            }

            if (calibrationFinished == true && emergencyStop == false)
            {
                CheckMaximumBorders();

                if (FreeMovement.IsChecked == false)
                {
                    try
                    {
                        if (z_null == false)// && ((vert_down_pin.Read() == GpioPinValue.Low) || (vert_up_pin.Read() == GpioPinValue.Low)))
                            CheckPositionZ();
                        if (phi_null == false)// && ((turn_left_pin.Read() == GpioPinValue.Low) || (turn_right_pin.Read() == GpioPinValue.Low)))
                            CheckPositionPhi();
                        if (p_null == false)// && ((hor_fwd_pin.Read() == GpioPinValue.Low) || (hor_back_pin.Read() == GpioPinValue.Low)))
                            CheckPositionP();
                        if (grip_null == false)// && ((grip_open_pin.Read() == GpioPinValue.Low) || (grip_close_pin.Read() == GpioPinValue.Low)))
                            CheckPositionGrip();
                    }
                    catch
                    {
                        StopAll();
                        throw;
                    }
                    
                }
            }

            

        }

        private void TimerGUI_Tick(Object sender, Object args)
        {
            if (calibrationFinished == true && emergencyStop == false)
            {
                CheckGUI();
            }
            gui_counter++;
            if (gui_counter >= 10)
            {
                VertPos.Text = actual.printZ();
                HorPos.Text = actual.printP();
                TurnPos.Text = actual.printPhi();
                GripPos.Text = "" + grip_act;

                VertTarPos.Text = setpoint.printZ();
                HorTarPos.Text = setpoint.printP();
                TurnTarPos.Text = setpoint.printPhi();
                GripTarPos.Text = "" + grip_soll;
                gui_counter = 0;
            }
            
        }

        private void CheckGUI()
        {
            if (ButtonCalibrate.IsPressed == true)
                Calibrate();

            if (ButtonUp.IsPressed == true)
            {
                GoUp();
                wasPressed = true;
            }
            else if (ButtonDown.IsPressed == true)
            {
                GoDown();
                wasPressed = true;
            }
            else if (ButtonLeft.IsPressed == true)
            {
                TurnLeft();
                wasPressed = true;
            }
            else if (ButtonRight.IsPressed == true)
            {
                TurnRight();
                wasPressed = true;
            }
            else if (ButtonFwd.IsPressed == true)
            {
                GoFwd();
                wasPressed = true;
            }
            else if (ButtonBack.IsPressed == true)
            {
                GoBack();
                wasPressed = true;
            }
            else if (ButtonOpen.IsPressed == true)
            {
                OpenGrip();
                wasPressed = true;
            }
            else if (ButtonClose.IsPressed == true)
            {
                CloseGrip();
                wasPressed = true;
            }
            else if (wasPressed == true)
            {
                StopAll();
                wasPressed = false;
            }

            if (SetPoint.IsPressed == true)
            {
                int p = 0;
                int.TryParse(HorIn.Text, out p);

                int z = 0;
                int.TryParse(VertIn.Text, out z);

                int phi = 0;
                int.TryParse(TurnIn.Text, out phi);

                setpoint.setP(p);
                setpoint.setZ(z);
                setpoint.setPhi(phi);
            }
            else if (random.IsPressed == true)
            {
                Random rand = new Random();
                setpoint.setZ(rand.Next(0, def.Z_MAX));
                setpoint.setPhi(rand.Next(0, def.PHI_MAX));
                setpoint.setP(rand.Next(0, def.P_MAX));
            }

        }

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

        private void StopAll()
        {
            StopZ();
            StopP();
            StopPhi();
            StopGrip();
        }

        private void StopZ()
        {
            PinLow(z_enable_pin);
            PinLow(z_down_pin);
            PinLow(z_up_pin);
        }

        private void StopPhi()
        {
            PinLow(phi_enable_pin);
            PinLow(phi_left_pin);
            PinLow(phi_right_pin);
        }

        private void StopP()
        {
            PinLow(p_enable_pin);
            PinLow(p_fwd_pin);
            PinLow(p_back_pin);
        }

        private void StopGrip()
        {
            PinLow(grip_enable_pin);
            PinLow(grip_open_pin);
            PinLow(grip_close_pin);
        }

        private void GoUp()
        {
            PinHigh(z_enable_pin);
            PinHigh(z_up_pin);
            PinLow(z_down_pin);
        }

        private void GoDown()
        {
            PinHigh(z_enable_pin);
            PinHigh(z_down_pin);
            PinLow(z_up_pin);
        }

        private void TurnRight()
        {
            PinHigh(phi_enable_pin);
            PinHigh(phi_right_pin);
            PinLow(phi_left_pin);
        }

        private void TurnLeft()
        {
            PinHigh(phi_enable_pin);
            PinHigh(phi_left_pin);
            PinLow(phi_right_pin);
        }

        private void GoFwd()
        {
            PinHigh(p_enable_pin);
            PinHigh(p_fwd_pin);
            PinLow(p_back_pin);
        }

        private void GoBack()
        {
            PinHigh(p_enable_pin);
            PinLow(p_fwd_pin);
            PinHigh(p_back_pin);
        }

        private void OpenGrip()
        {
            PinHigh(grip_enable_pin);
            PinHigh(grip_open_pin);
            PinLow(grip_close_pin);
        }

        private void CloseGrip()
        {
            PinHigh(grip_enable_pin);
            PinLow(grip_open_pin);
            PinHigh(grip_close_pin);
        }

        private void Calibrate()
        {
            inCalibration = true;
            calibrationFinished = false;
            Calibrate_P();
        }

        private void Calibrate_P()
        {
            inCalibration = true;
            actual.setP(def.P_MAX);
            GoBack();
        }

        private void Calibrate_Z()
        {
            inCalibration = true;
            actual.setZ(def.Z_MAX);
            GoUp();
        }

        private void Calibrate_Phi()
        {
            inCalibration = true;
            actual.setPhi(def.PHI_MAX);
            TurnRight();
        }

        private void Calibrate_Grip()
        {
            inCalibration = true;
            grip_act = def.GRIP_MAX;
            OpenGrip();
        }

        private void Null_Z()
        {
            z_null = true;
            GoDown();
        }

        private void Null_Phi()
        {
            phi_null = true;
            TurnLeft();
        }

        private void Null_P()
        {
            p_null = true;
            GoFwd();
        }

        private void Null_Grip()
        {
            grip_null = true;
            CloseGrip();
        }

        private void PinLow(GpioPin pin)
        {
            pin.Write(GpioPinValue.Low);
        }

        private void PinHigh(GpioPin pin)
        {
            pin.Write(GpioPinValue.High);
        }
    }
}
