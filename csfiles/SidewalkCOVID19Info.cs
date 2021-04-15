using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace SidewalkCOVID19
{
    public class SidewalkCategoryIcon : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            //credit: virus by Ayub Irawan from the Noun Project
            Grasshopper.Instances.ComponentServer.AddCategoryIcon("COVID19", Resource1.virus);
            Grasshopper.Instances.ComponentServer.AddCategorySymbolName("COVID19", 'C');
            return GH_LoadingInstruction.Proceed;
        }
    }
    public class SidewalkCOVID19Info : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "SidewalkCOVID19";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                //credit: virus by Ayub Irawan from the Noun Project
                return Resource1.virus_24X24;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "This library analyzes and visualizes New York City's sidewalk social distancing potential.";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("b02a50fb-f106-4366-aaaa-ca034bdf78ab");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Yaxuan Liu";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "yaxuanliu.com";
            }
        }
    }
}
