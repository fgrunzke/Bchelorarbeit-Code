using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bachelorarbeit
{
    class Obstacle
    {
        private int P1;
        private int P2;
        private int Z1;
        private int Z2;
        private int Phi1;
        private int Phi2;

        public Obstacle()
        {
            P1 = 0;
            P2 = 0;
            Z1 = 0;
            Z2 = 0;
            Phi1 = 0;
            Phi2 = 0;
        }

        public Obstacle(int P1, int P2, int Z1, int Z2, int Phi1, int Phi2)
        {
            this.P1 = P1;
            this.P2 = P2;
            this.Z1 = Z1;
            this.Z2 = Z2;
            this.Phi1 = Phi1;
            this.Phi2 = Phi2;
        }

        public int getP1() { return P1; }
        public int getP2() { return P2; }
        public int getZ1() { return Z1; }
        public int getZ2() { return Z2; }
        public int getPhi1() { return Phi1; }
        public int getPhi2() { return Phi2; }
    }
}
