using System;
using System.Runtime.Remoting.Channels;
using PoePricer.Extensions;

namespace PoePricer
{
    public class Item
    {
        public string Name = "Buriza";
        public string BaseType ="Cool Bow";
        public string ClassType = "Bow";
        public string GripType = "";
        public string Implicit = "";

        public byte iLevel = 0;

        public ushort BaseAR = 0;
        public ushort BaseEV = 0;
        public ushort BaseES = 0;



        public ushort[] BaseDamage;
        public float BaseAPS = 0;

        public float BaseCrit = 0;

        
        public float GetPropValue(string fieldName)
        {
            try
            {
                //return this.GetType().GetField(fieldName).GetValue(this) as float;
                return 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}