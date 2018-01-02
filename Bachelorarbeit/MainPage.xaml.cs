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
        private const int HOR_FWD_PIN = 26;
        private const int HOR_BACK_PIN = 19;
        private const int HOR_ENABLE_PIN = 13;
        private GpioPin hor_fwd_pin;
        private GpioPin hor_back_pin;
        private GpioPin hor_enable_pin;

        //Vertikaler Motor
        private const int VERT_UP_PIN = 21;
        private const int VERT_DOWN_PIN = 20;
        private const int VERT_ENABLE_PIN = 16;
        private GpioPin vert_up_pin;
        private GpioPin vert_down_pin;
        private GpioPin vert_enable_pin;

        //Motor zur Drehung um die Mittelachse
        private const int TURN_RIGHT_PIN = 12;
        private const int TURN_LEFT_PIN = 6;
        private const int TURN_ENABLE_PIN = 5;
        private GpioPin turn_right_pin;
        private GpioPin turn_left_pin;
        private GpioPin turn_enable_pin;

        //Motor zum Öffnen/Schließen der Greifzange
        private const int GRIP_OPEN_PIN = 11;
        private const int GRIP_CLOSE_PIN = 8;
        private const int GRIP_ENABLE_PIN = 7;
        private GpioPin grip_open_pin;
        private GpioPin grip_close_pin;
        private GpioPin grip_enable_pin;

        //Eingangs GPIOs
        //Schrittzähler Horizontale Achse
        private const int HOR_STEP_PIN = 9;
        private GpioPin hor_step_pin;

        //Inkrementalgeber Vertikale Achse
        private const int VERT_INK1_PIN = 25;
        private const int VERT_INK2_PIN = 24;
        private GpioPin vert_ink1_pin;
        private GpioPin vert_ink2_pin;

        //Inkrementalgeber der Drehung
        private const int TURN_INK1_PIN = 22;
        private const int TURN_INK2_PIN = 23;
        private GpioPin turn_ink1_pin;
        private GpioPin turn_ink2_pin;

        //Schrittzähler des Greifers
        private const int GRIP_STEP_PIN = 10;
        private GpioPin grip_step_pin;

        //Kalibrierungsschalter
        private const int HOR_CAL_PIN = 27;
        private const int VERT_CAL_PIN = 17;
        private const int TURN_CAL_PIN = 18;
        private const int GRIP_CAL_PIN = 4;
        private GpioPin hor_cal_pin;
        private GpioPin vert_cal_pin;
        private GpioPin turn_cal_pin;
        private GpioPin grip_cal_pin;
        private const double input_timeout = 0.1;


        private SolidColorBrush redBrush = new SolidColorBrush(Windows.UI.Colors.Red);
        private SolidColorBrush grayBrush = new SolidColorBrush(Windows.UI.Colors.LightGray);
        private DispatcherTimer timer = new DispatcherTimer();
        private DispatcherTimer timerGUI = new DispatcherTimer();

        //private const int def.HOR_MAX = 700;
        //private const int def.VERT_MAX = 2000;
        //private const int def.TURN_MAX = 2000;
        //private const int def.GRIP_MAX = 20;

        private int hor_act = def.P_MAX;
        private int vert_act = def.Z_MAX;
        private int turn_act = def.PHI_MAX;
        private int grip_act = def.GRIP_MAX;

        private int grip_soll = def.GRIP_SOLL;

        private Point setpoint = new Point(def.P_SOLL, def.PHI_SOLL, def.Z_SOLL);
        private Point actual = new Point();

        private bool inCalibration = false;
        private bool calibrationFinished = false;
        private bool vert_ink2_pressed = false;
        private bool vert_null = true;
        private bool turn_ink2_pressed = false;
        private bool turn_null = true;
        private bool hor_null = true;
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
            if (vert_cal_pin != null)
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

            //Ausgabgs GPIOs öffnen
            hor_enable_pin = gpio.OpenPin(HOR_ENABLE_PIN);
            hor_fwd_pin = gpio.OpenPin(HOR_FWD_PIN);
            hor_back_pin = gpio.OpenPin(HOR_BACK_PIN);

            vert_enable_pin = gpio.OpenPin(VERT_ENABLE_PIN);
            vert_up_pin = gpio.OpenPin(VERT_UP_PIN);
            vert_down_pin = gpio.OpenPin(VERT_DOWN_PIN);

            turn_enable_pin = gpio.OpenPin(TURN_ENABLE_PIN);
            turn_left_pin = gpio.OpenPin(TURN_LEFT_PIN);
            turn_right_pin = gpio.OpenPin(TURN_RIGHT_PIN);

            grip_enable_pin = gpio.OpenPin(GRIP_ENABLE_PIN);
            grip_open_pin = gpio.OpenPin(GRIP_OPEN_PIN);
            grip_close_pin = gpio.OpenPin(GRIP_CLOSE_PIN);

            //Eingangs GPIOs öffnen
            hor_step_pin = gpio.OpenPin(HOR_STEP_PIN);
            vert_ink1_pin = gpio.OpenPin(VERT_INK1_PIN);
            vert_ink2_pin = gpio.OpenPin(VERT_INK2_PIN);
            turn_ink1_pin = gpio.OpenPin(TURN_INK1_PIN);
            turn_ink2_pin = gpio.OpenPin(TURN_INK2_PIN);
            grip_step_pin = gpio.OpenPin(GRIP_STEP_PIN);

            hor_cal_pin = gpio.OpenPin(HOR_CAL_PIN);
            vert_cal_pin = gpio.OpenPin(VERT_CAL_PIN);
            turn_cal_pin = gpio.OpenPin(TURN_CAL_PIN);
            grip_cal_pin = gpio.OpenPin(GRIP_CAL_PIN);

            //Ausgangs GPIOs konfigurieren und initialisieren
            SetGpioOut(vert_enable_pin);
            SetGpioOut(vert_up_pin);
            SetGpioOut(vert_down_pin);

            SetGpioOut(turn_enable_pin);
            SetGpioOut(turn_left_pin);
            SetGpioOut(turn_right_pin);

            SetGpioOut(hor_enable_pin);
            SetGpioOut(hor_fwd_pin);
            SetGpioOut(hor_back_pin);

            SetGpioOut(grip_enable_pin);
            SetGpioOut(grip_open_pin);
            SetGpioOut(grip_close_pin);

            //Eingangs GPIOs konfigurieren, entprellen und Evnetlistener Registrieren

            SetGpioIn(vert_cal_pin);
            SetGpioIn(vert_ink1_pin, input_timeout);
            SetGpioIn(vert_ink2_pin, input_timeout);
            vert_cal_pin.ValueChanged += vert_cal_pin_ValueChanged;
            vert_ink1_pin.ValueChanged += vert_ink1_pin_ValueChanged;
            vert_ink2_pin.ValueChanged += vert_ink2_pin_ValueChanged;

            SetGpioIn(turn_cal_pin);
            SetGpioIn(turn_ink1_pin, input_timeout);
            SetGpioIn(turn_ink2_pin, input_timeout);
            turn_cal_pin.ValueChanged += turn_cal_pin_ValueChanged;
            turn_ink1_pin.ValueChanged += turn_ink1_pin_ValueChanged;
            turn_ink2_pin.ValueChanged += turn_ink2_pin_ValueChanged;

            SetGpioIn(hor_cal_pin);
            SetGpioIn(hor_step_pin, input_timeout);
            hor_cal_pin.ValueChanged += hor_cal_pin_ValueChanged;
            hor_step_pin.ValueChanged += hor_step_pin_ValueChanged;

            SetGpioIn(grip_cal_pin);
            SetGpioIn(grip_step_pin, input_timeout);
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

        private void vert_cal_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (vert_cal_pin.Read() == GpioPinValue.High)
            {
                StopVert();

                if (inCalibration == true)
                {
                    //vert_cal = false;
                    //if(vert_cal == turn_cal == false)
                    inCalibration = false;
                    Null_Vert();
                }
                else
                {
                    Calibrate_Vert();
                }
            }

            if (vert_cal_pin.Read() == GpioPinValue.Low)
            {
                StopVert();
                vert_act = 0;
                emergencyStop = false;
                if (vert_null == true)
                {

                    vert_null = false;
                    calibrationFinished = true;
                    
                }
                else
                {
                    Calibrate();
                }
            }
        }

        private void turn_cal_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (turn_cal_pin.Read() == GpioPinValue.High)
            {
                StopTurn();

                if (inCalibration == false)
                {
                    inCalibration = false;
                    Null_Turn();
                }
                else
                {
                    Calibrate_Turn();
                }
            }

            if (turn_cal_pin.Read() == GpioPinValue.Low)
            {
                StopTurn();
                turn_act = 0;
                emergencyStop = false;
                if (turn_null == true)
                {
                    turn_null = false;
                    Calibrate_Vert();
                }
                else
                {
                    Calibrate();
                }
            }
        }

        private void hor_cal_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (hor_cal_pin.Read() == GpioPinValue.High)
            {
                StopHor();

                if (inCalibration == false)
                {
                    inCalibration = false;
                    Null_Hor();
                }
                else
                {
                    Calibrate_Hor();
                }
            }

            if (hor_cal_pin.Read() == GpioPinValue.Low)
            {
                StopHor();
                hor_act = 0;
                emergencyStop = false;
                if (hor_null == true)
                {
                    hor_null = false;
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
                    Calibrate_Turn();
                }
                else
                {
                    Calibrate();
                }
            }
        }

        private void vert_ink1_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (vert_ink1_pin.Read() == GpioPinValue.High)
            {
                //if (vert_ink2_pressed == true)
                //{
                //    if (vert_ink2_pin.Read() == GpioPinValue.High)
                //        vert_act--;
                //    else
                //        vert_act++;
                //    actual.setZ(vert_act);
                //}
                if (vert_up_pin.Read() == GpioPinValue.High)
                    vert_act--;
                else if (vert_down_pin.Read() == GpioPinValue.High)
                    vert_act++;
                actual.setZ(vert_act);
                vert_ink2_pressed = false;
            }
        }

        private void vert_ink2_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (vert_ink2_pin.Read() == GpioPinValue.High)
                vert_ink2_pressed = true;
        }

        private void turn_ink1_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (turn_ink1_pin.Read() == GpioPinValue.High)
            {
                //if (turn_ink2_pressed == true)
                //{
                //    if (turn_ink2_pin.Read() == GpioPinValue.High)
                //        turn_act--;
                //    else
                //        turn_act++;
                //    actual.setPhi(turn_act);
                //}
                if (turn_right_pin.Read() == GpioPinValue.High)
                    turn_act--;
                else if (turn_left_pin.Read() == GpioPinValue.High)
                    turn_act++;
                actual.setPhi(turn_act);
                turn_ink2_pressed = false;
            }
        }

        private void turn_ink2_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (turn_ink2_pin.Read() == GpioPinValue.High)
                turn_ink2_pressed = true;
        }

        private void hor_step_pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (hor_step_pin.Read() == GpioPinValue.High)
            {
                if (hor_fwd_pin.Read() == GpioPinValue.High)
                {
                    hor_act++;
                }
                else if (hor_back_pin.Read() == GpioPinValue.High)
                {
                    hor_act--;
                }
                actual.setP(hor_act);
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
            if (vert_cal_pin.Read() == GpioPinValue.High)
            {
                emergencyStop = true;
                calibrationFinished = false;
                StopVert();
                Null_Vert();
            }

            if (turn_cal_pin.Read() == GpioPinValue.High)
            {
                emergencyStop = true;
                calibrationFinished = false;
                StopTurn();
                Null_Turn();
            }

            if (hor_cal_pin.Read() == GpioPinValue.High)
            {
                emergencyStop = true;
                calibrationFinished = false;
                StopHor();
                Null_Hor();
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
                        if (vert_null == false)// && ((vert_down_pin.Read() == GpioPinValue.Low) || (vert_up_pin.Read() == GpioPinValue.Low)))
                            CheckPositionVert();
                        if (turn_null == false)// && ((turn_left_pin.Read() == GpioPinValue.Low) || (turn_right_pin.Read() == GpioPinValue.Low)))
                            CheckPositionTurn();
                        if (hor_null == false)// && ((hor_fwd_pin.Read() == GpioPinValue.Low) || (hor_back_pin.Read() == GpioPinValue.Low)))
                            CheckPositionHor();
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

        private void CheckPositionVert()
        {
            //if (vert_act <= vert_soll - def.tube || vert_act <= 0)
            //    GoDown();
            //else if (vert_act >= vert_soll + def.tube || vert_act >= def.VERT_MAX)
            //    GoUp();
            //else if (vert_act < vert_soll + def.tube && vert_act > vert_soll - def.tube)
            //    StopVert();
            if (actual.getZ() >= def.Z_MAX - def.TUBE)
            {
                StopVert();
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
                StopVert();
            
            
        }

        private void CheckPositionTurn()
        {
            if (actual.getPhi() <= setpoint.getPhi() - def.TUBE || actual.getPhi() <= 0)
                TurnLeft();
            else if (actual.getPhi() >= setpoint.getPhi() + def.TUBE || actual.getPhi() >= def.PHI_MAX)
                TurnRight();
            else if (actual.getPhi() < setpoint.getPhi() + def.TUBE && actual.getPhi() > setpoint.getPhi() - def.TUBE)
                StopTurn();
            else if (actual.getPhi() >= def.PHI_MAX - def.TUBE)
            {
                StopTurn();
                setpoint.setPhi(actual.getPhi());
            }
            else
                StopTurn();
        }

        private void CheckPositionHor()
        {
            if (actual.getP() <= setpoint.getP() - def.TUBE || actual.getP() <= 0)
                GoFwd();
            else if (actual.getP() >= setpoint.getP() + def.TUBE || actual.getP() >= def.P_MAX)
                GoBack();
            else if (actual.getP() < setpoint.getP() + def.TUBE && actual.getP() > setpoint.getP() - def.TUBE)
                StopHor();
            else if (actual.getP() >= def.P_MAX - def.TUBE)
            {
                StopHor();
                setpoint.setP(actual.getP());
            }
            else
                StopHor();
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
                StopHor();
            if (actual.getPhi() >= def.PHI_MAX - def.TUBE)
                StopTurn();
            if (actual.getZ() >= def.Z_MAX - def.TUBE)
                StopVert();
            if (grip_act >= def.GRIP_MAX - def.TUBE)
                StopGrip();
        }

        private void StopAll()
        {
            StopVert();
            StopHor();
            StopTurn();
            StopGrip();
        }

        private void StopVert()
        {
            PinLow(vert_enable_pin);
            PinLow(vert_down_pin);
            PinLow(vert_up_pin);
        }

        private void StopTurn()
        {
            PinLow(turn_enable_pin);
            PinLow(turn_left_pin);
            PinLow(turn_right_pin);
        }

        private void StopHor()
        {
            PinLow(hor_enable_pin);
            PinLow(hor_fwd_pin);
            PinLow(hor_back_pin);
        }

        private void StopGrip()
        {
            PinLow(grip_enable_pin);
            PinLow(grip_open_pin);
            PinLow(grip_close_pin);
        }

        private void GoUp()
        {
            PinHigh(vert_enable_pin);
            PinHigh(vert_up_pin);
            PinLow(vert_down_pin);
        }

        private void GoDown()
        {
            PinHigh(vert_enable_pin);
            PinHigh(vert_down_pin);
            PinLow(vert_up_pin);
        }

        private void TurnRight()
        {
            PinHigh(turn_enable_pin);
            PinHigh(turn_right_pin);
            PinLow(turn_left_pin);
        }

        private void TurnLeft()
        {
            PinHigh(turn_enable_pin);
            PinHigh(turn_left_pin);
            PinLow(turn_right_pin);
        }

        private void GoFwd()
        {
            PinHigh(hor_enable_pin);
            PinHigh(hor_fwd_pin);
            PinLow(hor_back_pin);
        }

        private void GoBack()
        {
            PinHigh(hor_enable_pin);
            PinLow(hor_fwd_pin);
            PinHigh(hor_back_pin);
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
            Calibrate_Hor();
        }

        private void Calibrate_Hor()
        {
            inCalibration = true;
            actual.setP(def.P_MAX);
            GoBack();
        }

        private void Calibrate_Vert()
        {
            inCalibration = true;
            actual.setZ(def.Z_MAX);
            GoUp();
        }

        private void Calibrate_Turn()
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

        private void Null_Vert()
        {
            vert_null = true;
            GoDown();
        }

        private void Null_Turn()
        {
            turn_null = true;
            TurnLeft();
        }

        private void Null_Hor()
        {
            hor_null = true;
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
