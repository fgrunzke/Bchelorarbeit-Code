﻿using System;
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

        //Timer für Positionsvegleich und Auswertung/Aktalisierung der GUI
        private DispatcherTimer timer = new DispatcherTimer();
        private DispatcherTimer timerGUI_check = new DispatcherTimer();
        private DispatcherTimer timerGUI_actualize = new DispatcherTimer();

        //aktuelle Werte für die Position
        private int p_act = def.P_MAX;
        private int z_act = def.Z_MAX;
        private int phi_act = def.PHI_MAX;
        private int grip_act = def.GRIP_MAX;

        //soll und ist Werte als Punkt in Zylinderkoordniaten
        private Point setpoint = new Point(def.P_SOLL, def.PHI_SOLL, def.Z_SOLL);
        private Point actual = new Point();
        private Point target = new Point();

        //Vorgabe für die Positon des Greifers, da nich im Punkt mit berücksichtigt
        private int grip_soll = def.GRIP_SOLL;

        //verschiede Kontrollvariablen für Programmablaufplanung
        private bool inCalibration = false;
        private bool calibrationFinished = false;
        private bool z_ink2_pressed = false;
        private bool z_null = true;
        private bool phi_ink2_pressed = false;
        private bool phi_null = true;
        private bool p_null = true;
        private bool grip_null = true;
        private bool wasPressed = false;

        
        private List<Point> path = new List<Point>();
        private Obstacle obst = new Obstacle(def.P1, def.P2, def.Z1, def.Z2, def.Phi1, def.Phi2);

        public MainPage()
        {
            InitializeComponent();
            InitGPIO();

            timer.Interval = TimeSpan.FromMilliseconds(def.CHECKTIME_POSITION);
            timer.Tick += Timer_Tick;
            timer.Start();

            timerGUI_check.Interval = TimeSpan.FromMilliseconds(def.CHECKTIME_GUI);
            timerGUI_check.Tick += TimerGUI_check_Tick;
            timerGUI_check.Start();

            timerGUI_actualize.Interval = TimeSpan.FromMilliseconds(def.ACTUALIZETIME_GUI);
            timerGUI_actualize.Tick += TimerGUI_actualize_Tick;
            timerGUI_actualize.Start();

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
            try
            {
                if (z_cal_pin.Read() == GpioPinValue.High)
                {
                    StopZ();

                    if (inCalibration == true)
                    {
                        inCalibration = false;
                        Null_Z();
                    }
                    else
                        GoDown();
                }

                if (z_cal_pin.Read() == GpioPinValue.Low)
                {
                    StopZ();
                    z_act = 0;
                    if (z_null == true)
                    {
                        z_null = false;
                        Calibrate_Phi();
                    }
                }
            }
            catch
            {
                StopAll();
                throw;
            }
            
        }

        private void phi_cal_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            try
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
                        TurnLeft();
                
                }

                if (phi_cal_pin.Read() == GpioPinValue.Low)
                {
                    StopPhi();
                    phi_act = 0;
                    if (phi_null == true)
                    {
                        phi_null = false;
                        calibrationFinished = true;
                    }
                }
            }
            catch
            {
                StopAll();
                throw;
            }
            
        }

        private void p_cal_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            try
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
                        GoFwd();
                    }
                }

                if (p_cal_pin.Read() == GpioPinValue.Low)
                {
                    StopP();
                    p_act = 0;
                    if (p_null == true)
                    {
                        p_null = false;
                        Calibrate_Grip();
                    }
                }
            }
            catch
            {
                StopAll();
                throw;
            }
            
        }

        private void grip_cal_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            try
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
                        CloseGrip();
                    }
                }

                if (grip_cal_pin.Read() == GpioPinValue.Low)
                {
                    StopGrip();
                    grip_act = 0;
                    if (grip_null == true)
                    {

                        grip_null = false;
                        Calibrate_Z();
                    }
                }
            }
            catch
            {
                StopAll();
                throw;
            }
            
        }

        private void z_ink1_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            try
            {
                if (z_ink1_pin.Read() == GpioPinValue.High)
                {
                    //if (z_ink2_pressed == true)
                    //{
                    //    if (z_ink2_pin.Read() == GpioPinValue.High)
                    //        z_act--;
                    //    else
                    //        z_act++;
                    //    actual.setZ(z_act);
                    //}
                    if (z_up_pin.Read() == GpioPinValue.High)
                        z_act--;
                    else if (z_down_pin.Read() == GpioPinValue.High)
                        z_act++;
                    actual.setZ(z_act);
                    z_ink2_pressed = false;
                }
            }
            catch
            {
                StopAll();
                throw;
            }
            
        }

        private void z_ink2_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            try
            {
                if (z_ink2_pin.Read() == GpioPinValue.High)
                    z_ink2_pressed = true;
            }
            catch
            {
                StopAll();
                throw;
            }
            
        }

        private void phi_ink1_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            try
            {
                if (phi_ink1_pin.Read() == GpioPinValue.High)
                {
                    //if (phi_ink2_pressed == true)
                    //{
                    //    if (phi_ink2_pin.Read() == GpioPinValue.High)
                    //        phi_act--;
                    //    else
                    //        phi_act++;
                    //    actual.setPhi(phi_act);
                    //}
                    if (phi_right_pin.Read() == GpioPinValue.High)
                        phi_act--;
                    else if (phi_left_pin.Read() == GpioPinValue.High)
                        phi_act++;
                    actual.setPhi(phi_act);
                    phi_ink2_pressed = false;
                }
            }
            catch
            {
                StopAll();
                throw;
            }
            
        }

        private void phi_ink2_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            try
            {
                if (phi_ink2_pin.Read() == GpioPinValue.High)
                    phi_ink2_pressed = true;
            }
            catch
            {
                StopAll();
                throw;
            }
            
        }

        private void p_step_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            try
            {
                if (p_step_pin.Read() == GpioPinValue.High)
                    {
                        if (p_fwd_pin.Read() == GpioPinValue.High)
                            p_act++;
                        else if (p_back_pin.Read() == GpioPinValue.High)
                            p_act--;
                        actual.setP(p_act);
                    }
            }
            catch
            {
                StopAll();
                throw;
            }
            
        }

        private void grip_step_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            try
            {
                if (grip_step_pin.Read() == GpioPinValue.High)
                    {
                        if (grip_close_pin.Read() == GpioPinValue.High)
                            grip_act++;
                        else if (grip_open_pin.Read() == GpioPinValue.Low)
                            grip_act--;
                    }
            }
            catch
            {
                StopAll();
                throw;
            }
            
        }

        private void Timer_Tick(Object sender, Object args)
        {
            try
            {
                if (z_cal_pin.Read() == GpioPinValue.High)
                {
                    StopZ();
                    GoDown();
                }

                if (phi_cal_pin.Read() == GpioPinValue.High)
                {
                    StopPhi();
                    TurnLeft();
                }

                if (p_cal_pin.Read() == GpioPinValue.High)
                {
                    StopP();
                    GoFwd();
                }

                if (grip_cal_pin.Read() == GpioPinValue.High)
                {
                    StopGrip();
                    CloseGrip();
                }

                if (calibrationFinished == true)
                {
                    CheckMaximumBorders();

                    if (FreeMovement.IsChecked == false)
                    {
                        if (z_null == false)// && ((vert_down_pin.Read() == GpioPinValue.Low) || (vert_up_pin.Read() == GpioPinValue.Low)))
                            CheckPositionZ();
                        if (phi_null == false)// && ((turn_left_pin.Read() == GpioPinValue.Low) || (turn_right_pin.Read() == GpioPinValue.Low)))
                            CheckPositionPhi();
                        if (p_null == false)// && ((hor_fwd_pin.Read() == GpioPinValue.Low) || (hor_back_pin.Read() == GpioPinValue.Low)))
                            CheckPositionP();
                        if (grip_null == false)// && ((grip_open_pin.Read() == GpioPinValue.Low) || (grip_close_pin.Read() == GpioPinValue.Low)))
                            CheckPositionGrip();
                        if (Difference(actual.getP(), setpoint.getP()) <= def.OVERSHOOT && 
                            Difference(actual.getPhi(), setpoint.getPhi()) <= def.OVERSHOOT &&
                            Difference(actual.getZ(), setpoint.getZ()) <= def.OVERSHOOT &&
                            path.Any())
                        {
                            setpoint = path.First();
                            path.RemoveAt(0);
                        }
                    }
                }
            }
            catch
            {
                StopAll();
                throw;
            }
        }

        private void TimerGUI_check_Tick(Object sender, Object args)
        {
            try
            {
                if (calibrationFinished == true)
                {
                    CheckGUI();
                }
            }
            catch
            {
                StopAll();
                throw;
            }
            
            
        }

        private void TimerGUI_actualize_Tick(Object sender, Object args)
        {
            try
            {
                VertPos.Text = actual.printZ();
                HorPos.Text = actual.printP();
                TurnPos.Text = actual.printPhi();
                GripPos.Text = "" + grip_act;

                VertTarPos.Text = target.printZ();
                HorTarPos.Text = target.printP();
                TurnTarPos.Text = target.printPhi();
                GripTarPos.Text = "" + grip_soll;

                Cartesian(actual);
            }
            catch
            {
                StopAll();
                throw;
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

                target.setP(p);
                target.setZ(z);
                target.setPhi(phi);

                path = Calculatepath(actual, target);
            }
            else if (random.IsPressed == true)
            {
                Random rand = new Random();
                target.setZ(rand.Next(0, def.Z_MAX));
                target.setPhi(rand.Next(0, def.PHI_MAX));
                target.setP(rand.Next(0, def.P_MAX));

                HorIn.Text = target.getP().ToString("0000");
                VertIn.Text = target.getZ().ToString("0000");
                TurnIn.Text = target.getPhi().ToString("0000");

                path = Calculatepath(actual, target);
            }

        }

        private void CheckPositionZ()
        {
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
            if (actual.getP() >= def.P_MAX - def.TUBE && p_fwd_pin.Read() == GpioPinValue.High)
                StopP();
            if (actual.getPhi() >= def.PHI_MAX - def.TUBE && phi_left_pin.Read() == GpioPinValue.High)
                StopPhi();
            if (actual.getZ() >= def.Z_MAX - def.TUBE && z_down_pin.Read() == GpioPinValue.High)
                StopZ();
            if (grip_act >= def.GRIP_MAX - def.TUBE && grip_close_pin.Read() == GpioPinValue.High)
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

        private List<Point> Calculatepath(Point s, Point t)
        {
            List<Point> calcPath = new List<Point>();
            Point next = s;
            Point next2 = s;

            if (Obstacles.IsChecked == false)
                calcPath.Add(t);
            else
            {
                if (IsPonObstacle(s, t) == false && IsZonObstacle(s, t) == false && IsPhionObstacle(s, t) == false)
                {
                    calcPath.Add(t);
                }
                else
                {
                    //Point helper = new Point(t.getP(), obst.getPhi1() - def.TUBE - def.OVERSHOOT, s.getZ());
                    next.setP(t.getP());
                    if (getBigger(obst.getPhi1(), s.getPhi()) == obst.getPhi1())
                    {
                        next.setPhi(obst.getPhi1() - def.OVERSHOOT);
                        next.setZ(obst.getZ1() - def.OVERSHOOT);
                        calcPath.Add(next);
                        next2 = next;
                        next2.setPhi(obst.getPhi2() + def.OVERSHOOT);
                        calcPath.Add(next2);
                    }
                    else
                    {
                        next.setPhi(obst.getPhi2() + def.OVERSHOOT);
                        next.setZ(obst.getZ1() - def.OVERSHOOT);
                        calcPath.Add(next);
                        next2 = next;
                        next2.setPhi(obst.getPhi1() - def.OVERSHOOT);
                        calcPath.Add(next2);
                    }
                    calcPath.Add(t);
                    //path.Enqueue(helper);
                    //helper.setPhi(obst.getPhi2());
                    //path.Enqueue(helper);
                    //path.Enqueue(target);
                }
            }
            setpoint = calcPath.First();
            calcPath.RemoveAt(0);
            return calcPath;
        }

        private bool IsPonObstacle(Point s, Point t)
        {
            int start = getSmaller(s.getP(), t.getP());
            int target = getBigger(s.getP(), t.getP());
            
            if (Difference(start, target) == 0) { return false; }

            List<int> p = new List<int>();

            for (int i = start; i <= target; i++)
            {
                p.Add(i);
            }

            foreach (int i in p)
            {
                if (i >= obst.getP1() && i <= obst.getP2())
                    return true;
            }
            
            return false;
        }

        private bool IsZonObstacle(Point s, Point t)
        {
            int start = getSmaller(s.getZ(), t.getZ());
            int target = getBigger(s.getZ(), t.getZ());

            if (Difference(start, target) == 0) { return false; }

            List<int> z = new List<int>();

            for (int i = start; i <= target; i++)
            {
                z.Add(i);
            }

            foreach (int i in z)
            {
                if (i >= obst.getZ1() && i <= obst.getZ2())
                    return true;
            }

            return false;
        }

        private bool IsPhionObstacle(Point s, Point t)
        {
            int start = getSmaller(s.getPhi(), t.getPhi());
            int target = getBigger(s.getPhi(), t.getPhi());

            if (Difference(start, target) == 0) { return false; }

            List<int> phi = new List<int>();

            for (int i = start; i <= target; i++)
            {
                phi.Add(i);
            }

            foreach (int i in phi)
            {
                if (i >= obst.getPhi1() && i <= obst.getPhi2())
                    return true;
            }

            return false;
        }

        private int Difference(int s, int t)
        {
            int diff = s - t;
            if (diff < 0)
            {
                diff *= -1;
            }
            return diff;
        }

        private int getSmaller(int s, int t)
        {
            if (s < t)
                return s;
            else
                return t;
        }

        private int getBigger(int s, int t)
        {
            if (s > t)
                return s;
            else 
                return t;
        }

        private void Cartesian(Point act)
        {
            double phi = (act.getPhi() * def.PHI_FACT * Math.PI) / 180;
            double x = (act.getP() * Math.Sin(phi));
            double y = (act.getP() * Math.Cos(phi));
            CartX.Text = x.ToString("000.0");
            CartY.Text = y.ToString("000.0");
            CartZ.Text = act.getZ().ToString("000.0");
        }

        


    }
}
