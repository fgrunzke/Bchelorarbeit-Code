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

        public Point()
        {
            this.p = 0;
            this.phi = 0;
            this.z = 0;
        }

        public Point(int p = 0, int phi = 0, int z = 0)
        {
            this.p = p;
            this.phi = phi;
            this.z = z;
        }

        public int getP()
        {
            return this.p;
        }
        public int getPhi()
        {
            return this.phi;
        }
        public int getZ()
        {
            return this.z;
        }
        public void setP(int p)
        {
            this.p = p;
        }
        public void setPhi(int phi)
        {
            this.phi = phi;
        }
        public void setZ(int z)
        {
            this.z = z;
        }
    }
}
