using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace SidewalkCOVID19
{
    public class TrafficVolume : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public TrafficVolume()
          : base("Pedestrian Traffic Volume Analysis", "Pedestrian Traffic",
              "This component analysis selected city blocks' pedestrian traffic",
              "COVID19", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Buildings", "Bldgs", "Parsed Volumetric Building", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Blocks", "Blocks", "Parsed Closed Block Curves", GH_ParamAccess.list);
            pManager.AddCurveParameter("Lot Lines", "LotLns", "Parsed Lot Lines", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Target Blocks", "Target", "Target Blocks to Analyze", GH_ParamAccess.list);
            pManager.AddNumberParameter("Affecting Radius", "Radius", "Radius from Target Blocks", GH_ParamAccess.item);
            pManager.AddPointParameter("Subway Stops", "Subway", "Optional. Nearby Subway Stops", GH_ParamAccess.list);
            pManager.AddPointParameter("Interest Points", "IntPts", "Optional. Nearby Interest Points", GH_ParamAccess.list);
            pManager.AddNumberParameter("Average Floor Height", "AvgFlrH", "Default to 12 ft. Average Floor Height of Buildings Used to Approximate Floor Area", GH_ParamAccess.item, 12);
            //city average (total building area / total population)
            pManager.AddNumberParameter("Average Squarefoot Per Person", "AvgSqft/Person", "Default to the City Average 460 sqft/person. Used to Approximate Occupancy", GH_ParamAccess.item, 460);
            pManager.AddNumberParameter("Percentage of Occupants", "%Occupants", "Default to 10%. Percentage of Occupants from Buildings on Target Blocks on Target Sidewalks", GH_ParamAccess.item, 0.1);
            pManager.AddNumberParameter("Percentage of Surrounding Occupants","%SurOccupants", "Default to 1%. Percentage of Occupants from Nearby Buildings within Radius on Target Sidwalk", GH_ParamAccess.item, 0.01);
            pManager.AddNumberParameter("Subway Pedestrian Traffic", "SubwayTraffic", "Default to 50 people per stop. Traffic Brought to the Sidewalk by Nearyby Stops", GH_ParamAccess.item, 50.0);
            pManager.AddNumberParameter("Interest Points Pedestrian Traffic", "IntPtsTraffic", "Default to 50 people per point. Traffic Brought to the Sidewalk by Nearby Interest Points", GH_ParamAccess.item, 50.0);
            pManager.AddBooleanParameter("Run Analysis", "Run", "Anaylize Pedestrian Traffic", GH_ParamAccess.item, false);
            for (int i = 5; i < pManager.ParamCount - 1; i++)
            {
                pManager[i].Optional = true;
            }

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Pedestrian Traffic Volume", "PedTraffic", "Approximate Pedestrian Traffic Volume on Target Sidewalk", GH_ParamAccess.item);
            pManager.AddBrepParameter("Target Blocks", "Blocks", "Target Blocks", GH_ParamAccess.list);
            pManager.AddBrepParameter("Target Buildings", "Buildings", "Buildings on Target Blocks", GH_ParamAccess.tree);
            pManager.AddBrepParameter("Target Sidewalk", "Sidewalk", "Sidewalk on Target Blocks", GH_ParamAccess.tree);
            pManager.AddBrepParameter("Nearby Buildings", "NearbyBldgs", "Adjacent Buildings within the Radius", GH_ParamAccess.tree);
            pManager.AddPointParameter("Nearby Subway Stops", "Subway", "Subway Stops within the Radius", GH_ParamAccess.tree);
            pManager.AddPointParameter("Nearby Interest Points", "IntPts", "Interest Points with in the Radius", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool run = false;
            if (!DA.GetData(13, ref run))
            {
                return;
            }
            else if (run == false)
            {
                return;
            }
            List<GH_Curve> blocks = new List<GH_Curve>();
            List<GH_Curve> targets = new List<GH_Curve>();
            List<GH_Point> subway = new List<GH_Point>();
            List<GH_Point> interests = new List<GH_Point>();
            double r = 0;
            double flr = 0;
            double sqftperson = 0;
            double occupant = 0;
            double srdoccupant = 0;
            double subped = 0;
            double intped = 0;

            if (!DA.GetDataTree(0, out GH_Structure<GH_Brep> buildings))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No Building Data");
                return;
            }
            
            if (!DA.GetDataList(1, blocks))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No Block Data");
                return;
            }

            if (!DA.GetDataTree(2, out GH_Structure<GH_Curve> lots))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No Lot Line Data");
                return;
            }

            if (!DA.GetDataList(3, targets))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No Target Data");
                return;
            }
            if (!DA.GetData(4, ref r))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No Radius");
                return;
            }
            DA.GetDataList(5, subway);
            DA.GetDataList(6, interests);
            DA.GetData(7, ref flr);
            DA.GetData(8, ref sqftperson);
            DA.GetData(9, ref occupant);
            DA.GetData(10, ref srdoccupant);
            DA.GetData(11, ref subped);
            DA.GetData(12, ref intped);

            double rsq = Math.Pow(r, 2);
            List<Curve> trgts = GHCrvToCrv(targets);
            List<int> index = new List<int>();
            List<Curve> joined = ReparamCrv(Curve.JoinCurves(trgts));
            List<Point3d> centroid = FindCentroid(joined);
            GH_Structure<GH_Brep> sidewalk = new GH_Structure<GH_Brep>();
            GH_Structure<GH_Point> AdjSub = new GH_Structure<GH_Point>();
            GH_Structure<GH_Point> AdjInt = new GH_Structure<GH_Point>();
            GH_Structure<GH_Brep> AdjBldgs = new GH_Structure<GH_Brep>();
            GH_Structure<GH_Brep> targetBldgs = new GH_Structure<GH_Brep>();
            List<double> pedCount = new List<double>();
            for (int i = 0; i < joined.Count; i++)
            {
                for (int j = 0; j < blocks.Count; j++)
                {
                    if (PtinCrvWorldXY(blocks[j].Value, centroid[i]))
                    {
                        index.Add(j);
                        List<Curve> srfboundaries = new List<Curve>();
                        List<Curve> tmp = new List<Curve>();
                        srfboundaries.Add(blocks[j].Value);
                        tmp.AddRange(GHCrvToCrv(lots[j]));
                        srfboundaries.AddRange(Curve.CreateBooleanUnion(tmp, 0.01));
                        //create sidewalk surface
                        sidewalk.AppendRange(BrepToGHBrep(Brep.CreatePlanarBreps(srfboundaries, 0.01)), new GH_Path(i));
                        targetBldgs.AppendRange(buildings[j], new GH_Path(i));
                    }
                }
            }

            for (int i = 0; i < joined.Count; i++)
            {
                //create radius
                Curve cir = NurbsCurve.CreateFromCircle(new Circle(centroid[i], r));
                double subwayv = 0;
                double interestv = 0;

                //find subway stops within raidus
                List<GH_Point> subtmp = new List<GH_Point>();
                
                for (int j = 0; j < subway.Count; j++)
                {
                    double d = planardsquared(subway[j].Value, centroid[i]);
                    if (d <= rsq)
                    {
                        double probability = (r - Math.Sqrt(d)) / r;
                        subwayv += probability * subped;
                        subtmp.Add(subway[j]);
                    }
                }

                AdjSub.AppendRange(subtmp, new GH_Path(i));

                //find interes points within radius
                List<GH_Point> inttmp = new List<GH_Point>();

                for (int j = 0; j < interests.Count; j++)
                {
                    double d = planardsquared(interests[j].Value, centroid[i]);
                    if (d <= rsq)
                    {
                        double probability = (r - Math.Sqrt(d)) / r;
                        interestv += probability * intped;
                        inttmp.Add(interests[j]);
                        
                    }
                }
                AdjInt.AppendRange(inttmp, new GH_Path(i));
                //calculate building volume
                double v = 0;

                for (int j = 0; j < targetBldgs[i].Count; j++)
                {
                    var properties = VolumeMassProperties.Compute(targetBldgs[i][j].Value, true, false, false, false);
                    v += Math.Abs(properties.Volume);
                }

                //calculate surrounding building volume
                double sv = 0;

                List<GH_Brep> breptmp = new List<GH_Brep>();

                for (int j = 0; j < blocks.Count; j++)
                {
                    if (j != index[i])
                    {
                        for (int k = 0; k < buildings[j].Count; k++)
                        {
                            var properties = VolumeMassProperties.Compute(buildings[j][k].Value, false, true, false, false);
                            double d = planardsquared(properties.Centroid, centroid[i]);
                            if (d <= rsq)
                            {
                                double probability = (r - Math.Sqrt(d)) / r;
                                breptmp.Add(buildings[j][k]);
                                sv += Math.Abs(properties.Volume * probability);
                            }
                        }
                    }
                }
                AdjBldgs.AppendRange(breptmp, new GH_Path(i));

                double BlockTraffic = v / flr / sqftperson * occupant + sv / flr / sqftperson * srdoccupant + subwayv + interestv;
                pedCount.Add(BlockTraffic);
            }

            DA.SetDataList(0, pedCount);
            DA.SetDataList(1, CrvToGHCrv(joined));
            DA.SetDataTree(2, targetBldgs);
            DA.SetDataTree(3, sidewalk);
            DA.SetDataTree(4, AdjBldgs);
            DA.SetDataTree(5, AdjSub);
            DA.SetDataTree(6, AdjInt);
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
        List<GH_Curve> CrvToGHCrv(List<Curve> Crv)
        {
            List<GH_Curve> tmp = new List<GH_Curve>();
            for (int i = 0; i < Crv.Count; i++)
            {
                tmp.Add(new GH_Curve(Crv[i]));
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
        List<GH_Brep> BrepToGHBrep(List<Brep> brep)
        {
            List<GH_Brep> tmp = new List<GH_Brep>();
            for (int i = 0; i < brep.Count; i++)
            {
                tmp.Add(new GH_Brep(brep[i]));
            }
            return tmp;
        }

        List<GH_Brep> BrepToGHBrep(Brep[] brep)
        {
            List<GH_Brep> tmp = new List<GH_Brep>();
            for (int i = 0; i < brep.Length; i++)
            {
                tmp.Add(new GH_Brep(brep[i]));
            }
            return tmp;
        }
        List<Point3d> GHPtToPt(List<GH_Point> pt)
        {
            List<Point3d> tmp = new List<Point3d>();
            for (int i = 0; i < pt.Count; i++)
            {
                tmp.Add(pt[i].Value);
            }
            return tmp;
        }
        List<GH_Point> PtToGHPt(List<Point3d> pt)
        {
            List<GH_Point> tmp = new List<GH_Point>();
            for (int i = 0; i < pt.Count; i++)
            {
                tmp.Add(new GH_Point(pt[i]));
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

        double planardsquared(Point3d a, Point3d b)
        {
            Point3d tmp1 = new Point3d(a.X, a.Y, 0);
            Point3d tmp2 = new Point3d(b.X, b.Y, 0);
            double d = tmp1.DistanceToSquared(tmp2);
            return d;
        }
        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                //credit: bottleneck by Stephen Plaster from the Noun Project
                return Resource1.traffic;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("edac34f6-3b14-43c7-ab9e-c51b7844e2e6"); }
        }
    }
}