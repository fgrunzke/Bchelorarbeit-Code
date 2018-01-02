using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bachelorarbeit
{
    class Point
    {
        private int p;
        private int phi;
        private int z;

        private double P_FACT = def.P_VALUE;
        private double PHI_FACT = def.PHI_VALUE;
        private double Z_FACT = def.Z_VALUE;

        public Point()
        {
            p = 0;
            phi = 0;
            z = 0;

            P_FACT /= def.P_MAX;
            PHI_FACT /= def.PHI_MAX;
            Z_FACT /= -def.Z_MAX;
    }

        public Point(int pIn = 0, int phiIn = 0, int zIn = 0)
        {
            p = pIn;
            phi = phiIn;
            z = zIn;

            P_FACT = def.P_VALUE / def.P_MAX;
            PHI_FACT = def.PHI_VALUE / def.PHI_MAX;
            Z_FACT = -def.Z_VALUE / def.Z_MAX;
        }

        public int getP()
        {
            return p;
        }
        public int getPhi()
        {
            return phi;
        }
        public int getZ()
        {
            return z;
        }
        public void setP(int pIn)
        {
            p = pIn;
        }
        public void setPhi(int phiIn)
        {
            phi = phiIn;
        }
        public void setZ(int zIn)
        {
            z = zIn;
        }
        public string printP()
        {
            double value = ((p * P_FACT) / 10);
            string ret = String.Format("{0} / {1:0.00}cm",p, value);
            return ret;// "" + this.p + " / " + (this.p * P_FACT)/10;
        }
        public string printPhi()
        {
            double value = phi * PHI_FACT;
            string ret = String.Format("{0} / {1:000.00}°", phi, value);
            return ret;// this.phi + " / " + (this.phi * PHI_FACT);
        }
        public string printZ()
        {
            double value = (z * Z_FACT) / 10;
            string ret = String.Format("{0} / {1:00.00}cm", z, value);
            return ret;// "" + this.z + " / " + (this.z * Z_FACT)/10;
        }
    }
}
