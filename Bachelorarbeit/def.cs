using System;

namespace Bachelorarbeit
{
    public static class def
    {
        public const int P_SOLL = 250;
        public const int Z_SOLL = 1000;
        public const int PHI_SOLL = 500;
        public const int GRIP_SOLL = 25;
        public const int TUBE = 15;
        public const int OVERSHOOT = 20;

        public const int P_MAX = 800;
        public const int Z_MAX = 2000;
        public const int PHI_MAX = 2150;
        public const int GRIP_MAX = 40;

        public const double P_VALUE = 70;
        public const double Z_VALUE = 150;
        public const double PHI_VALUE = 180;

        public const double P_FACT = P_VALUE / P_MAX;
        public const double Z_FACT = Z_VALUE / Z_MAX;
        public const double PHI_FACT = PHI_VALUE / PHI_MAX;

        public const double input_timeout = 0.1;
        public const int CHECKTIME_POSITION = 20;
        public const int CHECKTIME_GUI = 100;
        public const int ACTUALIZETIME_GUI = 200;

        //Standerdhindernis
        public const int P1 = 0;
        public const int P2 = P_MAX;
        public const int Z1 = Z_MAX - 1250;
        public const int Z2 = Z_MAX;
        public const int Phi1 = 600;
        public const int Phi2 = 650;

        //Ausgangs GPIOs
        //Horizontaler Motor
        public const int P_FWD_PIN = 26;
        public const int P_BACK_PIN = 19;
        public const int P_ENABLE_PIN = 13;

        //Vertikaler Motor
        public const int Z_UP_PIN = 21;
        public const int Z_DOWN_PIN = 20;
        public const int Z_ENABLE_PIN = 16;

        //Motor zur Drehung um die Mittelachse
        public const int PHI_RIGHT_PIN = 12;
        public const int PHI_LEFT_PIN = 6;
        public const int PHI_ENABLE_PIN = 5;

        //Motor zum Öffnen/Schließen der Greifzange
        public const int GRIP_OPEN_PIN = 11;
        public const int GRIP_CLOSE_PIN = 8;
        public const int GRIP_ENABLE_PIN = 7;

        //Eingangs GPIOs
        //Schrittzähler Horizontale Achse
        public const int P_STEP_PIN = 9;

        //Inkrementalgeber Vertikale Achse
        public const int Z_INK1_PIN = 25;
        public const int Z_INK2_PIN = 24;

        //Inkrementalgeber der Drehung
        public const int PHI_INK1_PIN = 22;
        public const int PHI_INK2_PIN = 23;

        //Schrittzähler des Greifers
        public const int GRIP_STEP_PIN = 10;

        //Kalibrierungsschalter
        public const int P_CAL_PIN = 27;
        public const int Z_CAL_PIN = 17;
        public const int PHI_CAL_PIN = 18;
        public const int GRIP_CAL_PIN = 4;

    }
}