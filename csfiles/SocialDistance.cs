using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using Rhino.Collections;

namespace SidewalkCOVID19
{
    public class SocialDistance : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public SocialDistance()
          : base("Social Distance Analysis", "Distance Analysis",
              "This component analyzes social distance potential on given sidewalk segments",
              "COVID19", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Pedestrian Count", "PedTraffic", "Number of People on the sidewalk segments", GH_ParamAccess.list);
            pManager.AddBrepParameter("Sidewalk Brep", "Sidewalk", "Sidewalk segments", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Radius", "Radius", "Radius", GH_ParamAccess.item);
            pManager.AddPointParameter("Subway Stops", "Subway", "Nearby Subway Stops", GH_ParamAccess.tree);
            pManager.AddPointParameter("Interest Points", "IntPts", "Nearby Interest Points", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Attraction Strength", "Strength", "How much subway stops and interest points affect distribution", GH_ParamAccess.item, 2);
            pManager.AddNumberParameter("Desired Distance", "ftApart", "Default to 6ft. Desirable Distance between people", GH_ParamAccess.item, 6);
            pManager.AddNumberParameter("Grid Size", "GridSize", "Default to 3ft. Size of each grid to be visualized", GH_ParamAccess.item, 3);
            pManager.AddNumberParameter("Iteration", "Iteration", "Solution Iterations", GH_ParamAccess.item, 5);
            pManager.AddBooleanParameter("Run Analysis", "Run", "Anaylize Pedestrian Traffic", GH_ParamAccess.item, false);
            for (int i = 3; i < pManager.ParamCount - 1; i++)
            {
                pManager[i].Optional = true;
            }

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Average Distance", "Distance", "Average Distance at Grid Points", GH_ParamAccess.tree);
            pManager.AddMeshParameter("Visualized Data Map", "Color Map", "Average Distance Mapped with Color", GH_ParamAccess.list);
            pManager.AddNumberParameter("Smaller than Desired Distance", "Collision", "Number of Grid Points with Distance Smaller than Desired (Too Crowded)", GH_ParamAccess.list);
            pManager.AddMeshParameter("Problem Area", "ProblemArea", "Sidwalk Areas that are too Crowded", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool run = false;
            if (!DA.GetData(9, ref run))
            {
                return;
            }
            else if (run == false)
            {
                return;
            }

            //import parameters
            List<double> traffic = new List<double>();
            if (!DA.GetDataList(0, traffic))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No Traffic Data");
                return;
            }
            if (!DA.GetDataTree(1, out GH_Structure<GH_Brep> sidewalk))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No Sidewalk Data");
                return;
            }
            double r = 0;
            double iteration = 0;
            double pow = 0;
            double delta = 0;
            double ftApart = 0;
            DA.GetData(2, ref r);
            DA.GetDataTree(3, out GH_Structure<GH_Point> subway);
            DA.GetDataTree(4, out GH_Structure<GH_Point> intpts);
            DA.GetData(5, ref pow);
            DA.GetData(6, ref ftApart);
            DA.GetData(7, ref delta);
            DA.GetData(8, ref iteration);
            GH_Structure<GH_Number> distance = new GH_Structure<GH_Number>();
            GH_Structure<GH_Point> attractors = new GH_Structure<GH_Point>();

            //flatten all interest points
            for (int i = 0; i < traffic.Count; i++)
            {
                List<GH_Point> tmp = new List<GH_Point>();
                if (subway.DataCount != 0)
                {
                    tmp.AddRange(subway[i]);
                }
                if (intpts.DataCount != 0)
                { 
                    tmp.AddRange(intpts[i]);
                }
                attractors.AppendRange(tmp, new GH_Path(i));
            }

            
            List<Point3d[]> vertextmp = new List<Point3d[]>();

            //calculate and iterate distance
            for (int i = 0; i < traffic.Count; i++)
            {
                for (int j = 0; j < sidewalk[i].Count; j++)
                {
                    //create grid points
                    Point3d[] corners = sidewalk[i][j].Value.GetBoundingBox(true).GetCorners();
                    Point3d mincorner = corners[0];
                    Point3d maxcorner = corners[2];
                    double x_length = maxcorner.X - mincorner.X;
                    double y_length = maxcorner.Y - mincorner.Y;
                    int x_count = Convert.ToInt32(Math.Ceiling(x_length / delta));
                    int y_count = Convert.ToInt32(Math.Ceiling(y_length / delta));
                    List<Point3d> grid = squaregrid(mincorner, delta, x_count, y_count);
                    Point3d[] projected = Intersection.ProjectPointsToBreps(GHBrepToBrep(sidewalk[i]), grid, new Vector3d(0, 0, 1), 0.01);
                    vertextmp.Add(projected);

                    //iterations
                    for (int k = 0; k < iteration; k++)
                    {
                        Point3dList rndpts = new Point3dList();
                        List<GH_Number> ptdistances = new List<GH_Number>();
                        List<Point3d> att = new List<Point3d>();

                        //check if attractor exist
                        if (attractors[i].Count != 0)
                        {
                            att.AddRange(GHPtToPt(attractors[i]));
                            rndpts = Populate(sidewalk[i][j].Value, traffic[i], k, att, r, pow);
                        }
                        else
                        {
                            att.Add(new Point3d(0, 0, 0));
                            rndpts = Populate(sidewalk[i][j].Value, traffic[i], k, att, 0.001, 0);
                        }
                        for (int g = 0; g < projected.Length; g++)
                        {
                            Point3dList tmp = rndpts.Duplicate();
                            double tmpDist = 0;

                            //find 2 cloest points to grid point
                            for (int aa = 0; aa < 2; aa++)
                            {
                                int id1 = tmp.ClosestIndex(projected[g]);
                                Point3d tmppt = tmp[id1];
                                tmp.RemoveAt(id1);
                                for (int ab = 0; ab < 2; ab++)
                                {
                                    Point3dList tmp2 = tmp.Duplicate();
                                    int tmpid = tmp2.ClosestIndex(tmppt);
                                    tmpDist += tmppt.DistanceTo(tmp2[tmpid]);
                                    tmp2.RemoveAt(tmpid);
                                }
                            }

                            //average
                            tmpDist /= 4;
                            ptdistances.Add(new GH_Number(tmpDist));
                        }
                        distance.AppendRange(ptdistances, new GH_Path(new int[] { i, j, k }));
                    }
                }
            }

            //collapse dtat tree and calculate average distance at each grid point

            List<List<double>> avg = new List<List<double>>();
            for (int i = 0; i < traffic.Count; i++)
            {
                List<double> tmp = new List<double>();
                for (int j = 0; j < sidewalk[i].Count; j++)
                {
                    for (int k = 0; k < iteration; k++)
                    {
                        List<double> tmpNumber = GHNumToDouble(distance.get_Branch(new GH_Path(new int[] { i, j, k })) as List<GH_Number>);
                        if (tmp.Count == 0)
                        {
                            tmp.AddRange(tmpNumber);
                        }
                        else
                        {
                            for (int t = 0; t < tmp.Count; t++)
                            {
                                tmp[t] += tmpNumber[t];
                                tmp[t] /= 2;
                            }
                        }

                    }
                }
                avg.Add(tmp);
            }

            //convert double to GH_Number
            GH_Structure<GH_Number> dist = new GH_Structure<GH_Number>();
            List<GH_Number> incidents = new List<GH_Number>();
            List<List<int>> colids = new List<List<int>>();

            for (int i = 0; i < avg.Count; i++)
            {
                dist.AppendRange(DoubleToGHNum(avg[i]), new GH_Path(i));
                List<int> collisionindex = new List<int>();
                //counts grid points with distance smaller than desired
                int counter = 0;
                for (int j = 0; j < avg[i].Count; j++)
                {
                    if (avg[i][j] < ftApart)
                    {
                        collisionindex.Add(j);
                        counter++;
                    }
                }
                colids.Add(collisionindex);
                incidents.Add(new GH_Number(counter));
            }

            //create mesh and visualize data
            List<GH_Mesh> problem = new List<GH_Mesh>();
            List<GH_Mesh> ghmesh = new List<GH_Mesh>();
            for (int i = 0; i < vertextmp.Count; i++)
            {
                Mesh pArea = new Mesh();
                Mesh sidewalkmesh = new Mesh();
                List<double> tmpdist = new List<double>();
                tmpdist.AddRange(avg[i]);
                tmpdist.Sort();
                double minD = tmpdist[0];
                double maxD = tmpdist[tmpdist.Count - 1];
                double range = Math.Max(Math.Abs(minD - ftApart), Math.Abs(maxD - ftApart)); 
                for (int j = 0; j < vertextmp[i].Length; j++)
                {
                    double colordata = avg[i][j];
                    double dif = Math.Abs(ftApart - colordata);
                    if (dif > range)
                    {
                        dif = range;
                    }
                    double normalized = dif / range * 255;
                    int red = 0;
                    int green = 255;
                    int blue = 0;
                    Mesh meshtmp = new Mesh();
                    double startx = vertextmp[i][j].X;
                    double starty = vertextmp[i][j].Y;
                    double startz = vertextmp[i][j].Z;
                    double hd = delta / 2;
                    meshtmp.Vertices.Add(new Point3d(startx - hd, starty - hd, startz));
                    meshtmp.Vertices.Add(new Point3d(startx + hd, starty - hd, startz));
                    meshtmp.Vertices.Add(new Point3d(startx + hd, starty + hd, startz));
                    meshtmp.Vertices.Add(new Point3d(startx - hd, starty + hd, startz));
                    if (colordata <= ftApart)
                    {
                        red = Convert.ToInt32(Math.Round(normalized));
                        
                    }
                    else
                    {
                        blue = Convert.ToInt32(Math.Round(normalized)) ;
                    }
                    meshtmp.VertexColors.Add(red, green, blue);
                    meshtmp.VertexColors.Add(red, green, blue);
                    meshtmp.VertexColors.Add(red, green, blue);
                    meshtmp.VertexColors.Add(red, green, blue);
                    meshtmp.Faces.AddFace(0, 1, 2, 3);
                    sidewalkmesh.Append(meshtmp);
                    for (int k = 0; k < colids[i].Count; k++)
                    {
                        if (j == colids[i][k])
                        {
                            pArea.Append(meshtmp);
                        }
                    }
                }
                problem.Add(new GH_Mesh(pArea));
                ghmesh.Add(new GH_Mesh(sidewalkmesh));
            }
  
            DA.SetDataTree(0, dist);
            DA.SetDataList(1, ghmesh);
            DA.SetDataList(2, incidents);
            DA.SetDataList(3, problem);
        }

        //weighted random populate point with attractors
        //reference Junichiro Horikawa on Github
        Point3dList Populate(Brep brep, double n, int ite, List<Point3d> attractors, double attractor_r, double attractor_strength)
        {
            var points = new Point3dList();
            int num = Convert.ToInt32(n);
            if (brep != null) 
            {
                var attracts = new Point3dList(attractors);
                var rnd = new Random();
                var bbox = brep.GetBoundingBox(true);
                attractor_strength = (-1) * attractor_strength;

                for (int i = 0; i < num; i++)
                {
                    if (points.Count == 0)
                    {
                        var rndpt = CreateRandomPoint(rnd, brep, bbox);
                        points.Add(rndpt);
                    }
                    else
                    {
                        double fdist = -1;
                        Point3d fpt = new Point3d();
                        for (int t = 0; t < Math.Max(Math.Min(ite, i), 10); t++)
                        {
                            var nrndpt = CreateRandomPoint(rnd, brep, bbox);
                            var nindex = points.ClosestIndex(nrndpt);
                            var npts = points[nindex];

                            var ndist = npts.DistanceTo(nrndpt);

                            if (attractor_strength != 0)
                            {
                                var nattractid = attracts.ClosestIndex(nrndpt);
                                var nattarctpts = attracts[nattractid];
                                var mindist = attractor_r;
                                var pow = attractor_strength;
                                var nattractdist = Math.Pow(Remap(Math.Min(nattarctpts.DistanceTo(nrndpt), mindist), 0, mindist, 0, 1.0), pow);
                                ndist *= nattractdist;
                            }

                            if (fdist < ndist)
                            {
                                fdist = ndist;
                                fpt = nrndpt;
                            }
                        }
                        points.Add(fpt);
                    }

                }
            }
            return points;
        }
        double Remap(double val, double smin, double smax, double tmin, double tmax)
        {
            return (val - smin) / (smax - smin) * (tmax - tmin) + tmin;
        }
        Point3d CreateRandomPoint(Random rnd, Brep brep, BoundingBox bbox)
        {
            var x = Remap(rnd.NextDouble(), 0.0, 1.0, bbox.Min.X, bbox.Max.X);
            var y = Remap(rnd.NextDouble(), 0.0, 1.0, bbox.Min.Y, bbox.Max.Y);
            var z = Remap(rnd.NextDouble(), 0.0, 1.0, bbox.Min.Z, bbox.Max.Z);

            return brep.ClosestPoint(new Point3d(x, y, z));
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

        List<GH_Point> PtToGHPt(Point3d[] pt)
        {
            List<GH_Point> tmp = new List<GH_Point>();
            for (int i = 0; i < pt.Length; i++)
            {
                tmp.Add(new GH_Point(pt[i]));
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
        List<Brep> GHBrepToBrep(List<GH_Brep> brep)
        {
            List<Brep> tmp = new List<Brep>();
            for (int i = 0; i < brep.Count; i++)
            {
                tmp.Add(brep[i].Value);
            }
            return tmp;
        }
        List<double> GHNumToDouble(List<GH_Number> number)
        {
            List<double> tmp = new List<double>();
            for (int i = 0; i < number.Count; i++)
            {
                tmp.Add(number[i].Value);
            }
            return tmp;
        }
        List<GH_Number> DoubleToGHNum(List<double> num)
        {
            List<GH_Number> tmp = new List<GH_Number>();
            for (int i = 0; i < num.Count; i++)
            {
                tmp.Add(new GH_Number(num[i]));
            }
            return tmp;
        }
        List<Point3d> squaregrid(Point3d start, double delta, int xcount, int ycount) 
        {
            List<Point3d> grid = new List<Point3d>();
            double z = start.Z;
            for (int i = 0; i < ycount; i++)
            {
                for (int j = 0; j < xcount; j++)
                {
                    double x = start.X + delta * j;
                    double y = start.Y + delta * i;
                    grid.Add(new Point3d(x, y, z));
                }
            }
            return grid;
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

                //credit: social distancing by Victoruler from the Noun Project
                return Resource1.socialdistancing;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b0be27cd-01b2-4d7b-8eae-a5d45e291e93"); }
        }
    }
}
