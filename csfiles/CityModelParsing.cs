using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace SidewalkCOVID19
{
    public class CityModelParsing : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public CityModelParsing()
          : base("Parse Data in Rhino Model", "ParseModel",
              "This component analyze sidewalks' social distancing in New York City",
              "COVID19", "Parsing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Roof Surfaces", "RoofSrf", "Add roof surfaces in desirable area from the Buildings:RoofTop Surface layer", GH_ParamAccess.list);
            pManager.AddCurveParameter("Curb Curve", "Curb", "Add curves in desirable area from the Pavement Edge layer", GH_ParamAccess.list);
            pManager.AddCurveParameter("Lot Lines Curve", "LotLns", "Add curves in desirable area from the Lot Lines layer", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Parse Data", "Run", "Runs this component", GH_ParamAccess.item, false);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Buildings", "Bldgs", "Closed Brep of Buildings parsed onto blocks", GH_ParamAccess.tree);
            pManager.AddCurveParameter("City Block", "Block", "Closed Curve of City Block in desirable area", GH_ParamAccess.list);
            pManager.AddCurveParameter("Lot Lines", "LotsLns", "Lot Lines parsed onto blocks", GH_ParamAccess.tree);
        }
 
        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool run = false;
            if (!DA.GetData(3, ref run))
            {
                return;
            }
            else if (run == false)
            {
                return;
            }
            List<GH_Curve> curb = new List<GH_Curve>();
            List<GH_Brep> building = new List<GH_Brep>();
            List<GH_Curve> lot = new List<GH_Curve>();
            //Check if there are inputs
            if (!DA.GetDataList(0, building))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No Roof Data");
                return;
            }
            if (!DA.GetDataList(1, curb)) 
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No Curb Data");
                return;
            }
            if (!DA.GetDataList(2, lot)) 
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No Lot Lines data");
                return;
            }
            GH_Structure<GH_Brep> bldgonblock = new GH_Structure<GH_Brep>();
            GH_Structure<GH_Curve> lotsonblock = new GH_Structure<GH_Curve>();

            //convert GH_Geometry to Rhino.Geometry
            List<Curve> curbs = GHCrvToCrv(curb);
            List<Brep> bldgs = GHBrepToBrep(building);
            List<Curve> lots = ReparamCrv(GHCrvToCrv(lot));

            //join curb curves
            Curve[] tmp = Curve.JoinCurves(curbs);
            List<Curve> ClosedCurbs = ReparamCrv(tmp);

            //find centroids
            List<Point3d> bldg_centroid = FindCentroid(bldgs);
            List<Point3d> lots_centroid = FindCentroid(lots);
            List<Point3d> block_centroid = FindCentroid(ClosedCurbs);


            for (int i = 0; i < ClosedCurbs.Count; i++)
            {
                List<GH_Brep> tmpBrep = new List<GH_Brep>();
                List<GH_Curve> tmpCrv = new List<GH_Curve>();
                for (int j = 0; j < bldg_centroid.Count; j++)
                {
                    if (PtinCrvWorldXY(ClosedCurbs[i], bldg_centroid[j]))
                    {
                        Point3d projection = new Point3d(bldg_centroid[j].X, bldg_centroid[j].Y, block_centroid[i].Z);
                        Curve path = new Line(bldg_centroid[j], projection).ToNurbsCurve();
                        Brep extrude = bldgs[j].Faces[0].CreateExtrusion(path, true);
                        tmpBrep.Add(new GH_Brep(extrude));
                    }
                }
                for (int j = 0; j < lots_centroid.Count; j++)
                {
                    if (PtinCrvWorldXY(ClosedCurbs[i], lots_centroid[j]))
                    {
                        if(lots[j].IsClosed)
                        {
                            tmpCrv.Add(new GH_Curve(lots[j]));
                        }
                    }
                }
                bldgonblock.AppendRange(tmpBrep, new GH_Path(i));
                lotsonblock.AppendRange(tmpCrv, new GH_Path(i));
            }
            DA.SetDataTree(0, bldgonblock);
            DA.SetDataList(1, ClosedCurbs);
            DA.SetDataTree(2, lotsonblock);
        }

        List<Curve> ReparamCrv(List<Curve> Crv)
        {
            for (int i = 0; i < Crv.Count; i++)
            {
                Crv[i].Domain = new Interval(0.0, 1.0);
            }
            return Crv;
        }
        List<Curve> ReparamCrv(Curve[] Crv)
        {
            List<Curve> tmp = new List<Curve>();
            for (int i = 0; i < Crv.Length; i++)
            {
                Crv[i].Domain = new Interval(0.0, 1.0);
                tmp.Add(Crv[i]);
            }
            return tmp;
        }
        List<Curve> GHCrvToCrv(List<GH_Curve> Crv)
        {
            List<Curve> tmp = new List<Curve>();
            for (int i = 0; i < Crv.Count; i++)
            {
                tmp.Add(Crv[i].Value);
            }
            return tmp;
        }
        List<Brep> GHBrepToBrep(List<GH_Brep> brep)
        {
            List<Brep> tmp = new List<Brep>();
            for (int i = 0; i < brep.Count; i++)
            {
                tmp.Add(brep[i].Value);
            }
            return tmp;
        }
        List<Point3d> FindCentroid(List<Brep> geometry)
        {
            List<Point3d> centroid = new List<Point3d>();
            for (int i = 0; i < geometry.Count; i++)
            {
                centroid.Add(AreaMassProperties.Compute(geometry[i], false, true, false, false).Centroid);
            }
            return centroid;
        }
        List<Point3d> FindCentroid(List<Curve> Crv)
        {
            List<Point3d> centroid = new List<Point3d>();
            for (int i = 0; i < Crv.Count; i++)
            {
                if (Crv[i].IsClosed)
                {
                    centroid.Add(AreaMassProperties.Compute(Crv[i]).Centroid);
                }
                else if (Crv[i].MakeClosed(0.01))
                {
                    centroid.Add(AreaMassProperties.Compute(Crv[i]).Centroid);
                }
                else
                {
                    centroid.Add(Crv[i].PointAt(0.5));
                }
            }
            return centroid;
        }
        bool PtinCrvWorldXY(Curve ClosedCrv, Point3d testPt)
        {
            var events = ClosedCrv.Contains(testPt, Rhino.Geometry.Plane.WorldXY, 0.01);
            if (events == Rhino.Geometry.PointContainment.Inside || events == Rhino.Geometry.PointContainment.Coincident)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return Resource1.cityblock;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("9858cd68-658e-4b8e-b3f9-8c047f2b6c79"); }
        }
    }

  
}
