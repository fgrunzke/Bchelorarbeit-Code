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

        private GPIO gpio = new GPIO();

        public MainPage()
        {
            InitializeComponent();
            timer.Interval = TimeSpan.FromMilliseconds(50);
            timer.Tick += Timer_Tick;
            timer.Start();
            timerGUI.Interval = TimeSpan.FromMilliseconds(50);
            timerGUI.Tick += TimerGUI_Tick;
            timerGUI.Start();

            Calibrate();

            HorIn.Text = "" + def.P_SOLL;
            VertIn.Text = "" + def.Z_SOLL;
            TurnIn.Text = "" + def.PHI_SOLL;
        }

        

        private void Timer_Tick(Object sender, Object args)
        {
            //if (z_cal_pin.Read() == GpioPinValue.High)
            //{
            //    emergencyStop = true;
            //    calibrationFinished = false;
            //    StopZ();
            //    Null_Z();
            //}

            //if (phi_cal_pin.Read() == GpioPinValue.High)
            //{
            //    emergencyStop = true;
            //    calibrationFinished = false;
            //    StopPhi();
            //    Null_Phi();
            //}

            //if (p_cal_pin.Read() == GpioPinValue.High)
            //{
            //    emergencyStop = true;
            //    calibrationFinished = false;
            //    StopP();
            //    Null_P();
            //}

            //if (grip_cal_pin.Read() == GpioPinValue.High)
            //{
            //    emergencyStop = true;
            //    calibrationFinished = false;
            //    StopGrip();
            //    Null_Grip();
            //}

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

        

        
    }
}
