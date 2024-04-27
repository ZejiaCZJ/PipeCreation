using System;
using System.Collections.Generic;
using Priority_Queue; //https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using Rhino.Render;
using Rhino.DocObjects;
using System.Drawing;
using System.Linq;
using Rhino.Geometry.Intersect;
using Rhino.Collections;
using Rhino.DocObjects.Tables;
using Rhino.UI;
using System.Threading.Tasks;
using Alea;
using ILGPU;
using ILGPU.Runtime;
using System.Collections.Concurrent;
using System.Windows.Forms;

namespace MyUI
{

    public class Index
    {
        public int i { get; set; } 
        public int j { get; set; }
        public int k { get; set; }

        public Index()
        {
            i = 0;
            j = 0;
            k = 0;
        }

        public Index(int i, int j, int k)
        {
            this.i = i;
            this.j = j;
            this.k = k;
        }

        public bool Equal(Index index)
        {
            return index.i == i && index.j == j && index.k == k;
        }
    }

    public class Voxel
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double Cost { get; set; } //True cost from start point, AKA. g
        public double Distance { get; set; } //Estimated distance to end point, AKA. h
        public double CostDistance => Cost + Distance; //True cost + estimated distance, AKA. f
        public Voxel Parent { get; set; }

        public bool isTaken { get; set; }

        public Index Index { get; set; }

        public Vector3d vector { get; set; }

        public Voxel()
        {
            X=0; Y=0; Z=0; Cost=0; Distance=0; isTaken = false; vector = Vector3d.Unset;
        }
        
        //This function set the distance with Manhattan distance
        public void SetDistance(double targetX, double targetY, double targetZ)
        {
            this.Distance = Math.Abs(targetX - X) + Math.Abs(targetY - Y) + Math.Abs(targetZ - Z);
        }

        //This function get the distance with Manhattan distance
        public double GetDistance(double targetX, double targetY, double targetZ)
        {
            //return Math.Abs(targetX - X) + Math.Abs(targetY - Y) + Math.Abs(targetZ - Z);//Manhattan
            return Math.Sqrt(Math.Pow(targetX - X,2) + Math.Pow(targetY - Y,2) + Math.Pow(targetZ - Z,2)); //Euclidean
        }

        public bool Equal(Voxel voxel)
        {
            return voxel.X == this.X && voxel.Y == this.Y && voxel.Z == this.Z;
        }
    }

    public class PipeExit
    {
        public Point3d location { get; set; }
        public Boolean isTaken { get; set; }

        public PipeExit(Point3d location) 
        {
            this.location = location;
            this.isTaken = false;
        }
    }


    public class Auto_route : GH_Component
    {
        RhinoDoc myDoc;
        private Voxel[,,] voxelSpace;
        double pcbWidth;
        double pcbHeight;
        double pipeRadius;
        private static List<PipeExit> pipeExitPts;
        int voxelSpace_offset;
        ObjectAttributes solidAttribute, lightGuideAttribute, redAttribute, yellowAttribute, soluableAttribute;
        private static List<Brep> allPipes;
        private static List<Guid> allTempBoxesGuid;
        

        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public Auto_route()
          : base("Auto_route", "route",
              "This component triggers the auto-routing between the customized part and base PCI part",
              "MyUI", "Main")
        {
            myDoc = RhinoDoc.ActiveDoc;
            voxelSpace = null;
            pipeExitPts = new List<PipeExit>();
            allPipes = new List<Brep>();
            allTempBoxesGuid = new List<Guid>();

            //TODO: please adjust the size when the actual size is given
            pcbWidth = 20;
            pcbHeight = 20;
            pipeRadius = 3.5;
            voxelSpace_offset = 1; //Each voxel is now stands for 1/voxelSpace, 1/voxelSpace, 1/voxelSpace


            int solidIndex = myDoc.Materials.Add();
            Rhino.DocObjects.Material solidMat = myDoc.Materials[solidIndex];
            solidMat.DiffuseColor = System.Drawing.Color.White;
            solidMat.SpecularColor = System.Drawing.Color.White;
            solidMat.Transparency = 0;
            solidMat.CommitChanges();
            solidAttribute = new ObjectAttributes();
            //solidAttribute.LayerIndex = 2;
            solidAttribute.MaterialIndex = solidIndex;
            solidAttribute.MaterialSource = Rhino.DocObjects.ObjectMaterialSource.MaterialFromObject;
            solidAttribute.ObjectColor = Color.White;
            solidAttribute.ColorSource = ObjectColorSource.ColorFromObject;

            int lightGuideIndex = myDoc.Materials.Add();
            Rhino.DocObjects.Material lightGuideMat = myDoc.Materials[lightGuideIndex];
            lightGuideMat.DiffuseColor = System.Drawing.Color.Orange;
            lightGuideMat.Transparency = 0.3;
            lightGuideMat.SpecularColor = System.Drawing.Color.Orange;
            lightGuideMat.CommitChanges();
            lightGuideAttribute = new ObjectAttributes();
            //orangeAttribute.LayerIndex = 3;
            lightGuideAttribute.MaterialIndex = lightGuideIndex;
            lightGuideAttribute.MaterialSource = Rhino.DocObjects.ObjectMaterialSource.MaterialFromObject;
            lightGuideAttribute.ObjectColor = Color.Orange;
            lightGuideAttribute.ColorSource = ObjectColorSource.ColorFromObject;

            int redIndex = myDoc.Materials.Add();
            Rhino.DocObjects.Material redMat = myDoc.Materials[redIndex];
            redMat.DiffuseColor = System.Drawing.Color.Red;
            redMat.Transparency = 0.3;
            redMat.SpecularColor = System.Drawing.Color.Red;
            redMat.CommitChanges();
            redAttribute = new ObjectAttributes();
            //redAttribute.LayerIndex = 4;
            redAttribute.MaterialIndex = redIndex;
            redAttribute.MaterialSource = Rhino.DocObjects.ObjectMaterialSource.MaterialFromObject;
            redAttribute.ObjectColor = Color.Red;
            redAttribute.ColorSource = ObjectColorSource.ColorFromObject;

            int yellowIndex = myDoc.Materials.Add();
            Rhino.DocObjects.Material yellowMat = myDoc.Materials[yellowIndex];
            yellowMat.DiffuseColor = System.Drawing.Color.Yellow;
            yellowMat.Transparency = 0.3;
            yellowMat.SpecularColor = System.Drawing.Color.Yellow;
            yellowMat.CommitChanges();
            yellowAttribute = new ObjectAttributes();
            //yellowAttribute.LayerIndex = 4;
            yellowAttribute.MaterialIndex = yellowIndex;
            yellowAttribute.MaterialSource = Rhino.DocObjects.ObjectMaterialSource.MaterialFromObject;
            yellowAttribute.ObjectColor = Color.Yellow;
            yellowAttribute.ColorSource = ObjectColorSource.ColorFromObject;

            int soluableIndex = myDoc.Materials.Add();
            Rhino.DocObjects.Material soluableMat = myDoc.Materials[soluableIndex];
            soluableMat.DiffuseColor = System.Drawing.Color.Green;
            soluableMat.Transparency = 0.3;
            soluableMat.SpecularColor = System.Drawing.Color.Green;
            soluableMat.CommitChanges();
            soluableAttribute = new ObjectAttributes();
            //yellowAttribute.LayerIndex = 4;
            soluableAttribute.MaterialIndex = soluableIndex;
            soluableAttribute.MaterialSource = Rhino.DocObjects.ObjectMaterialSource.MaterialFromObject;
            soluableAttribute.ObjectColor = Color.Green;
            soluableAttribute.ColorSource = ObjectColorSource.ColorFromObject;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Customized Part", "Part", "The brep of the customized part", GH_ParamAccess.item);
            pManager.AddGenericParameter("Base Part Center", "Base", "The center point of the Base part", GH_ParamAccess.item);
            pManager.AddGenericParameter("The Model brep", "Model", "The brep of the Model", GH_ParamAccess.item);
            //pManager.AddGenericParameter("Voxel space", "voxels", "The voxel space of the brep", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddTextParameter("Voxel Space", "voxels", "The voxel space of the brep ", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Brep customized_part = new Brep();
            Point3d base_part_center = new Point3d();
            Brep currModel = new Brep();

            if (!DA.GetData(0, ref customized_part))
                return;
            if (!DA.GetData(1, ref base_part_center))
                return;
            if (!DA.GetData(2, ref currModel))
                return;
            //if (!DA.GetData(3, ref voxelSpace))
            //    return;


            #region Get the line that don't combine all gears ---> bestRoute1
            Point3d customized_part_center = customized_part.GetBoundingBox(true).Center;
            GetVoxelSpace(customized_part, currModel, 1);

            //Find the Pipe exit location
            if (pipeExitPts.Count == 0)
                GetPipeExits(base_part_center, currModel);

            Point3d pipeExit = new Point3d();
            foreach (var item in pipeExitPts)
            {
                if (item.isTaken == false)
                {
                    pipeExit = item.location;
                    item.isTaken = true;
                    break;
                }
            }

            List<Point3d> bestRoute1 = FindShortestPath(customized_part_center, pipeExit, customized_part, currModel);
            myDoc.Objects.Add(Curve.CreateInterpolatedCurve(bestRoute1, 1), redAttribute);
            myDoc.Views.Redraw();
            #endregion

            #region Get the line that combine all gears ---> bestRoute2
            CombineBreps(customized_part, currModel);
            GetVoxelSpace(customized_part, currModel, 2);

            List<Point3d> bestRoute2 = FindShortestPath(customized_part_center, pipeExit, customized_part, currModel);
            myDoc.Objects.Add(Curve.CreateInterpolatedCurve(bestRoute2, 1), soluableAttribute);
            myDoc.Views.Redraw();
            #endregion

            #region Determine which line is better by calculating the total angle of the route
            double angle_Route1 = AngleOfCurve(bestRoute1);
            double angle_Route2 = AngleOfCurve(bestRoute2);

            Curve bestRoute;

            if (angle_Route1 <= angle_Route2)
                bestRoute = Curve.CreateInterpolatedCurve(bestRoute1, 1);
            else
                bestRoute = Curve.CreateInterpolatedCurve(bestRoute2, 1);
            #endregion



            #region Create Pipes
            //Cut the first 5mm of the bestRoute to generate the inner pipe
            //Double[] divisionParameters = bestRoute.DivideByLength(5, true, out Point3d[] points);
            Curve lightGuidePipeRoute = bestRoute.Trim(CurveEnd.Start, bestRoute.GetLength() - 7);
            Curve soluablePipeRoute = bestRoute.Trim(CurveEnd.End, 7);

            List<GeometryBase> geometryBases = new List<GeometryBase>();
            geometryBases.Add(customized_part);

            lightGuidePipeRoute = lightGuidePipeRoute.Extend(CurveEnd.End, 100, CurveExtensionStyle.Line);
            soluablePipeRoute = soluablePipeRoute.Extend(CurveEnd.Start, 100, CurveExtensionStyle.Line);

            Brep[] soluablePipe = Brep.CreatePipe(soluablePipeRoute, 2, true, PipeCapMode.Flat, false, myDoc.ModelAbsoluteTolerance, myDoc.ModelAngleToleranceRadians);
            Brep[] lightGuidePipe = Brep.CreatePipe(lightGuidePipeRoute, 2, true, PipeCapMode.Flat, true, myDoc.ModelAbsoluteTolerance, myDoc.ModelAngleToleranceRadians);

            soluablePipe = Brep.CreateBooleanSplit(soluablePipe[0], currModel, myDoc.ModelAbsoluteTolerance);
            lightGuidePipe = Brep.CreateBooleanSplit(lightGuidePipe[0], currModel, myDoc.ModelAbsoluteTolerance);

            myDoc.Objects.AddBrep(soluablePipe[0], soluableAttribute); //soluablePipe[1] if used CreateBooleanSplit
            myDoc.Objects.AddBrep(lightGuidePipe[0], lightGuideAttribute);
            #endregion

            //myDoc.Objects.AddCurve(bestRoute);

            ////TODO: This block of code print the Maximum Curvature Points, maybe we can use them to calculate the curvature of the curve. Delete them when finish.
            //Point3d[] maxCurvaturePts = bestRoute.MaxCurvaturePoints();
            //foreach (var item in maxCurvaturePts)
            //{
            //    myDoc.Objects.AddPoint(item);
            //}

            #region BooleanDifference out the pipe portion from the current model
            allPipes.Add(soluablePipe[0]);
            allPipes.Add(lightGuidePipe[0]);

            //Show the new current model that lost the pipe portions only when the user is finished editing the model
            Boolean finish = true;
            foreach (var exit in pipeExitPts)
            {
                if (exit.isTaken == false)
                {
                    finish = false; break;
                }
            }

            if (finish)
            {
                List<Brep> firstSet = new List<Brep>();
                firstSet.Add(currModel);

                Brep[] currModel_new = Brep.CreateBooleanDifference(firstSet, allPipes, myDoc.ModelAbsoluteTolerance, false);
                myDoc.Objects.AddBrep(currModel_new[0]);
            }
            #endregion

            #region Delete current model and customized part bounding box
            var allObjects = new List<RhinoObject>(myDoc.Objects.GetObjectList(ObjectType.Brep));
            foreach (var item in allObjects)
            {
                Guid guid = item.Id;
                ObjRef currObj = new ObjRef(guid);
                Brep brep = currObj.Brep();

                if (brep != null)
                {
                    if (brep.IsDuplicate(customized_part, myDoc.ModelAbsoluteTolerance) || brep.IsDuplicate(currModel, myDoc.ModelAbsoluteTolerance) || allTempBoxesGuid.Any(thisGuid => thisGuid == guid))
                    {
                        myDoc.Objects.Delete(guid, true);
                        break;
                    }
                }
            }

            myDoc.Views.Redraw();
            #endregion
        }

        /// <summary>
        /// This method generates the total angle of a list of control points of a curve of degree 1
        /// </summary>
        /// <param name="controlPoints">a list of Point3d object that is meant to be control points of a curve of degree 1</param>
        /// <returns>The total angle of the curve</returns>
        private double AngleOfCurve(List<Point3d> controlPoints)
        {
            //Calculate the total angles
            double angleSum = 0.0;
            for (int i = 0; i < controlPoints.Count - 2; i++)
            {
                Vector3d dir1 = controlPoints[i + 1] - controlPoints[i];
                Vector3d dir2 = controlPoints[i + 2] - controlPoints[i + 1];

                double angle = Vector3d.VectorAngle(dir1, dir2);
                angleSum += angle;
            }

            // Convert the angle to degrees
            angleSum = RhinoMath.ToDegrees(angleSum);

            return angleSum;
        }

        /// <summary>
        /// This method generate a box that connect two breps' bounding boxes on overlap areas on x-y plane
        /// </summary>
        /// <param name="brepA">a Brep Object</param>
        /// <param name="brepB">a Brep Object</param>
        /// <returns>a bounding box that connects two breps</returns>
        private BoundingBox GetOverlapBoundingBox(Brep brepA, Brep brepB)
        {
            BoundingBox bboxA = brepA.GetBoundingBox(true);
            BoundingBox bboxB = brepB.GetBoundingBox(true);


            // Calculate overlap region in X and Y dimensions
            double xMin = Math.Max(bboxA.Min.X, bboxB.Min.X);
            double xMax = Math.Min(bboxA.Max.X, bboxB.Max.X);
            double yMin = Math.Max(bboxA.Min.Y, bboxB.Min.Y);
            double yMax = Math.Min(bboxA.Max.Y, bboxB.Max.Y);

            // Calculate overlap region in Z dimension
            double zMin = Math.Max(bboxA.Min.Z, bboxB.Min.Z);
            double zMax = Math.Min(bboxA.Max.Z, bboxB.Max.Z);

            // Calculate the dimensions of the overlap box
            double width = xMax - xMin;
            double length = yMax - yMin;
            double height = zMax - zMin;

            // Create the overlapping bounding box
            Point3d min = new Point3d(xMin, yMin, zMin);
            Point3d max = new Point3d(xMin + width, yMin + length, zMax);
            BoundingBox overlapBox = new BoundingBox(min, max);

            return overlapBox;
        }

        /// <summary>
        /// This method determines if two breps' bounding boxes are overlaped on x-y plane
        /// </summary>
        /// <param name="brepA"></param>
        /// <param name="brepB"></param>
        /// <returns></returns>
        private bool OverlapsInXY(Brep brepA, Brep brepB)
        {
            // Get bounding boxes for brepA and brepB
            BoundingBox bboxA = brepA.GetBoundingBox(true);
            BoundingBox bboxB = brepB.GetBoundingBox(true);

            // Check if the bounding boxes overlap in the X-Y dimension but not in the Z dimension
            bool overlapInX = (bboxA.Max.X > bboxB.Min.X && bboxA.Min.X < bboxB.Max.X) || (bboxB.Max.X > bboxA.Min.X && bboxB.Min.X < bboxA.Max.X);
            bool overlapInY = (bboxA.Max.Y > bboxB.Min.Y && bboxA.Min.Y < bboxB.Max.Y) || (bboxB.Max.Y > bboxA.Min.Y && bboxB.Min.Y < bboxA.Max.Y);
            //bool overlapInZ = (bboxA.Max.Z > bboxB.Min.Z && bboxA.Min.Z < bboxB.Max.Z) || (bboxB.Max.Z > bboxA.Min.Z && bboxB.Min.Z < bboxA.Max.Z);

            return overlapInX && overlapInY;
        }

        /// <summary>
        /// This method generates overlap boxes of all generated Brep objects in the view, except pipes, currModel and customized_part
        /// </summary>
        /// <param name="customized_part">The customized part that user wants</param>
        /// <param name="currModel">current model that the user wants to add pipe into</param>
        private void CombineBreps(Brep customized_part, Brep currModel)
        {
            List<Brep> breps = new List<Brep>();
            var allObjects = new List<RhinoObject>(myDoc.Objects.GetObjectList(ObjectType.Brep));
            foreach (var item in allObjects)
            {
                Guid guid = item.Id;
                ObjRef currObj = new ObjRef(guid);
                Brep brep = currObj.Brep();

                if (brep != null)
                {
                    //Ignore the current model and customized part
                    if (brep.IsDuplicate(currModel, myDoc.ModelAbsoluteTolerance) || brep.IsDuplicate(customized_part, myDoc.ModelAbsoluteTolerance) || allPipes.Any(pipeBrep => brep.IsDuplicate(pipeBrep, myDoc.ModelAbsoluteTolerance)))
                    {
                        continue;
                    }
                    breps.Add(brep);
                }
            }

            Quicksort(breps, 0, breps.Count - 1);

            for(int i = breps.Count-1; i > 0; i--)
            {
                for(int j = i - 1; j >= 0; j--)
                {
                    BoundingBox overlapBox;
                    if (breps[j].GetBoundingBox(true).Min.Z < breps[i].GetBoundingBox(true).Min.Z && OverlapsInXY(breps[i], breps[j]))
                    {
                        overlapBox = GetOverlapBoundingBox(breps[i], breps[j]);
                        if(overlapBox.ToBrep() != null)
                            allTempBoxesGuid.Add(myDoc.Objects.Add(overlapBox.ToBrep()));
                    }
                }
                
            }
        }

        
        /// <summary>
        /// This method generates a list of possible pipe exits for the PCB
        /// </summary>
        /// <param name="base_part_center">The base part center of the current model</param>
        /// <param name="currModel">current model that the user wants to add the pipe</param>
        private void GetPipeExits(Point3d base_part_center, Brep currModel)
        {
            BoundingBox boundingBox = currModel.GetBoundingBox(true);
            double offset = 2; //This offset stands for the gap between the edge of the PCB and the pipe exit

            //Left upper corner of the PCB
            PipeExit leftUpperCorner = new PipeExit(new Point3d(base_part_center.X - pcbWidth / 2 + pipeRadius + offset, base_part_center.Y + pcbHeight / 2 - pipeRadius - offset, base_part_center.Z));

            //Right upper corner of the PCB
            PipeExit rightUpperCorner = new PipeExit(new Point3d(base_part_center.X + pcbWidth / 2 - pipeRadius - offset, base_part_center.Y + pcbHeight / 2 - pipeRadius - offset, base_part_center.Z));

            //Left lower corner of the PCB
            PipeExit leftLowerCorner = new PipeExit(new Point3d(base_part_center.X - pcbWidth / 2 + pipeRadius + offset, base_part_center.Y - pcbHeight / 2 + pipeRadius + offset, base_part_center.Z));

            //Right lower corner of the PCB
            PipeExit rightLowerCorner = new PipeExit(new Point3d(base_part_center.X + pcbWidth / 2 - pipeRadius - offset, base_part_center.Y - pcbHeight / 2 + pipeRadius + offset, base_part_center.Z));

            pipeExitPts.Add(leftUpperCorner);
            pipeExitPts.Add(rightUpperCorner);
            pipeExitPts.Add(leftLowerCorner);
            pipeExitPts.Add(rightLowerCorner);
        }

        
        /// <summary>
        /// This method sorts a Point3d array based its Z value
        /// </summary>
        /// <param name="arr">A Point3d array</param>
        /// <param name="left">The start index of the sorting process</param>
        /// <param name="right">The end index of the sorting process</param>
        private void Quicksort(Point3d[] arr, int left, int right)
        {
            if (left < right)
            {
                int pivotIndex = Partition(arr, left, right);

                Quicksort(arr, left, pivotIndex - 1);
                Quicksort(arr, pivotIndex + 1, right);
            }
        }

        private int Partition(Point3d[] arr, int left, int right)
        {
            Point3d pivot = arr[right];
            int i = left - 1;

            for (int j = left; j < right; j++)
            {
                if (arr[j].Z < pivot.Z)
                {
                    i++;
                    Swap(arr, i, j);
                }
            }

            Swap(arr, i + 1, right);
            return i + 1;
        }

        private void Swap(Point3d[] arr, int i, int j)
        {
            Point3d temp = arr[i];
            arr[i] = arr[j];
            arr[j] = temp;
        }

        /// <summary>
        /// This method sorts a list of Brep objects based bounding boxes' center Z value
        /// </summary>
        /// <param name="arr">A list of Brep objects</param>
        /// <param name="left">The start index of the sorting process</param>
        /// <param name="right">The end index of the sorting process</param>
        private void Quicksort(List<Brep> arr, int left, int right)
        {
            if (left < right)
            {
                int pivotIndex = Partition(arr, left, right);

                Quicksort(arr, left, pivotIndex - 1);
                Quicksort(arr, pivotIndex + 1, right);
            }
        }

        private int Partition(List<Brep> arr, int left, int right)
        {
            Brep pivot = arr[right];
            int i = left - 1;

            for (int j = left; j < right; j++)
            {
                if (arr[j].GetBoundingBox(true).Center.Z < pivot.GetBoundingBox(true).Center.Z)
                {
                    i++;
                    Swap(arr, i, j);
                }
            }

            Swap(arr, i + 1, right);
            return i + 1;
        }

        private void Swap(List<Brep> arr, int i, int j)
        {
            Brep temp = arr[i];
            arr[i] = arr[j];
            arr[j] = temp;
        }

        /// <summary>
        /// This method obtains the 3D grid of the current model
        /// </summary>
        /// <param name="customized_part">The customized part that user wants</param>
        /// <param name="currModel">current model that the user wants to add pipe into</param>
        /// <param name="mode">1 = don't combine gears, 2 = combine gears</param>
        private void GetVoxelSpace(Brep customized_part, Brep currModel, int mode)
        {
            var allObjects = new List<RhinoObject>(myDoc.Objects.GetObjectList(ObjectType.Brep));
            Guid customized_part_Guid = Guid.Empty;
            Guid currModel_Guid = Guid.Empty;

            foreach (var item in allObjects)
            {
                Guid guid = item.Id;
                ObjRef currObj = new ObjRef(guid);
                Brep brep = currObj.Brep();
                if(brep != null)
                {
                    if (brep.IsDuplicate(currModel, myDoc.ModelAbsoluteTolerance))
                    {
                        currModel_Guid = guid;
                    }
                    if (brep.IsDuplicate(customized_part, myDoc.ModelAbsoluteTolerance))
                    {
                        customized_part_Guid = guid;
                    }
                }
               
            }



            BoundingBox boundingBox = currModel.GetBoundingBox(true);

            int w = (int)Math.Abs(boundingBox.Max.X - boundingBox.Min.X) * voxelSpace_offset; //width
            int l = (int)Math.Abs(boundingBox.Max.Y - boundingBox.Min.Y) * voxelSpace_offset; //length
            int h = (int)Math.Abs(boundingBox.Max.Z - boundingBox.Min.Z) * voxelSpace_offset; //height

            

            voxelSpace = new Voxel[w, l, h];

            double offset_spacer = 1 / voxelSpace_offset;

            #region Initialize the voxel space element-wise
            Parallel.For(0, w, i =>
            {
                for (int j = 0; j < l; j++)
                {
                    double baseX = i + boundingBox.Min.X;
                    double baseY = j + boundingBox.Min.Y;


                    Point3d basePoint = new Point3d(baseX, baseY, boundingBox.Min.Z - 1);
                    Point3d topPoint = new Point3d(baseX, baseY, boundingBox.Max.Z + 1);
                    Curve intersector = (new Line(basePoint, topPoint)).ToNurbsCurve();
                    int num_region = 0;
                    if(Intersection.CurveBrep(intersector, currModel, myDoc.ModelAbsoluteTolerance, out Curve[] overlapCurves, out Point3d[] intersectionPoints))
                    {
                        num_region = intersectionPoints.Length % 2;
                    }
                    
                    //Fix cases where the intersector intersect the brep at an edge
                    if (intersectionPoints != null && intersectionPoints.Length > 1)
                    {
                        Point3dList hit_points = new Point3dList(intersectionPoints);
                        hit_points.Sort();
                        Point3d hit = hit_points.Last();
                        List<Point3d> toRemoved_hit_points = new List<Point3d>();
                        for(int p = hit_points.Count - 2; p >= 0; p--)
                        {
                            if (hit_points[p].DistanceTo(hit) < myDoc.ModelAbsoluteTolerance)
                            {
                                toRemoved_hit_points.Add(hit);
                                toRemoved_hit_points.Add(hit_points[p]);
                            }
                            else
                                hit = hit_points[p];
                        }
                        foreach(var replicate in toRemoved_hit_points)
                            hit_points.Remove(replicate);
                        intersectionPoints = hit_points.ToArray();
                        Quicksort(intersectionPoints, 0, intersectionPoints.Length - 1);
                    }

                    for (int k = 0; k < h; k++)
                    {
                        double currentZ = offset_spacer * k + boundingBox.Min.Z;
                        Point3d currentPt = new Point3d(baseX, baseY, currentZ);
                        voxelSpace[i, j, k] = new Voxel
                        {
                            X = baseX,
                            Y = baseY,
                            Z = currentZ,
                            isTaken = true,
                            Index = new Index(i, j, k),
                            vector = new Vector3d(baseX, baseY, currentZ)
                        };

                        //Check if the current point is in the current model
                        if(num_region == 0)
                        {
                            for (int r = 0; r < intersectionPoints.Length; r += 2)
                            {
                                if (currentZ < intersectionPoints[r + 1].Z && currentZ > intersectionPoints[r].Z)
                                {
                                    voxelSpace[i, j, k].isTaken = false;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            voxelSpace[i, j, k].isTaken = !currModel.IsPointInside(currentPt, myDoc.ModelAbsoluteTolerance, true);
                        }

                       

                        //Traverse all objects in the Rhino View to check for intersection. 
                        Boolean intersected = false;
                        Double maximumDistance = 4;//TODO: Try different distance metric to get better result. Euclidean: Math.Sqrt(Math.Pow((voxelSpace_offset + 1), 2) * 3)

                        if (mode == 1)
                            maximumDistance = 3;

                        if(voxelSpace[i, j, k].isTaken == false)
                        {
                            foreach (var item in allObjects)
                            {
                                Guid guid = item.Id;
                                ObjRef currObj = new ObjRef(guid);
                                Brep brep = currObj.Brep();



                                //See if the point is strictly inside of the brep
                                if (brep != null)
                                {
                                    //Check if the current brep is the 3D model main body
                                    if (guid == customized_part_Guid || guid == currModel_Guid)
                                    {
                                        continue;
                                    }

                                    if (brep.IsPointInside(currentPt, myDoc.ModelAbsoluteTolerance, true))
                                    {
                                        voxelSpace[i, j, k].isTaken = true;
                                        break;
                                    }

                                    //See if the point is too close to the brep and will cause intersection after creating the pipe
                                    if (brep.ClosestPoint(currentPt).DistanceTo(currentPt) <= maximumDistance)
                                    {
                                        voxelSpace[i, j, k].isTaken = true;
                                        break;
                                    }
                                }
                            }
                        }
                        
                    }
                }
            });
            #endregion
        }




        /// <summary>
        /// This method finds the estimated index in the 3D grid of the current model that has the closest location to the given point
        /// </summary>
        /// <param name="point">A point that needs to be estimated</param>
        /// <param name="currModel">current model that the user wants to add pipe into</param>
        /// <returns>the estimated index in the 3D grid of the current model</returns>
        private Index FindClosestPointIndex(Point3d point, Brep currModel)
        {
            Index index = new Index();

            //Calculate the approximate index of Point3d. Then obtain the precise index that has the smallest distance within the 2*2*2 bounding box of the Point3d
            BoundingBox boundingBox = currModel.GetBoundingBox(true);

            #region Calculate an estimated index
            double w = boundingBox.Max.X - boundingBox.Min.X; //width
            double h = boundingBox.Max.Y - boundingBox.Min.Y; //length
            double l = boundingBox.Max.Z - boundingBox.Min.Z; //height
            
            int estimated_i = (int)((point.X - boundingBox.Min.X) * voxelSpace_offset);
            int estimated_j = (int)((point.Y - boundingBox.Min.Y) * voxelSpace_offset);
            int estimated_k = (int)((point.Z - boundingBox.Min.Z) * voxelSpace_offset);

            if (estimated_i >= voxelSpace.GetLength(0))
                estimated_i = voxelSpace.GetLength(0) - 1;
            if (estimated_j >= voxelSpace.GetLength(1))
                estimated_j = voxelSpace.GetLength(1) - 1;
            if (estimated_k >= voxelSpace.GetLength(2))
                estimated_k = voxelSpace.GetLength(2) - 1;
            if (estimated_i < 0)
                estimated_i = 0;
            if (estimated_j < 0)
                estimated_j = 0;
            if (estimated_k < 0)
                estimated_k = 0;
            #endregion

            #region Traverse the 5*5*5 bounding box of the estimated index to see if there is a better one
            double smallestDistance = voxelSpace[estimated_i, estimated_j, estimated_k].GetDistance(point.X, point.Y, point.Z);
            index.i = estimated_i;
            index.j = estimated_j;
            index.k = estimated_k;

            for (int i = estimated_i - 5; i < estimated_i + 6; i++)
            {
                for (int j = estimated_j - 5; j < estimated_j + 6; j++)
                {
                    for (int k = estimated_k - 5; k < estimated_k + 6; k++)
                    {
                        if (i < voxelSpace.GetLength(0) && j < voxelSpace.GetLength(1) && k < voxelSpace.GetLength(2) && i >= 0 && j >= 0 && k >= 0)
                        {
                            double distance = voxelSpace[i, j, k].GetDistance(point.X, point.Y, point.Z);

                            if(distance < smallestDistance)
                            {
                                smallestDistance = distance;
                                index.i = i;
                                index.j = j;
                                index.k = k;
                            }
                        }
                    }
                }
            }
            #endregion

            return index;
        }

        /// <summary>
        /// Finds surrounding neighbors in 5*5*5 region
        /// </summary>
        /// <param name="current">current index in the 3D grid of the current model</param>
        /// <param name="goal">the goal voxel of the A* algorithm</param>
        /// <param name="voxelSpace"> the 3D grid of the current model</param>
        /// <returns></returns>
        private Queue<Voxel> GetNeighbors(Index current, ref Voxel goal,ref Voxel[,,] voxelSpace)
        {
            #region Get all neighbors
            Queue<Voxel> neighbors = new Queue<Voxel>();

            //up,down,left,right,front,back voxels of the current
            for (int i = current.i - 2; i < current.i + 3; i++)
            {
                for (int j = current.j - 2; j < current.j + 3; j++)
                {
                    for (int k = current.k - 2; k < current.k + 3; k++)
                    {
                        if (i < voxelSpace.GetLength(0) && j < voxelSpace.GetLength(1) && k < voxelSpace.GetLength(2) && i >= 0 && j >= 0 && k >= 0)
                        {
                            if(goal.Equal(voxelSpace[i, j, k]))
                                neighbors.Enqueue(voxelSpace[i, j, k]);
                            if(voxelSpace[i, j, k].isTaken == false)
                                neighbors.Enqueue(voxelSpace[i, j, k]);
                        }
                    }
                }
            }
            #endregion

            #region Get direct neighbors only
            ////Left neighbor
            //if (current.i - 1 < voxelSpace.GetLength(0) && current.j < voxelSpace.GetLength(1) && current.k < voxelSpace.GetLength(2) && current.i - 1 >= 0 && current.j >= 0 && current.k >= 0)
            //{
            //    neighbors.Enqueue(voxelSpace[current.i - 1, current.j, current.k]);
            //}
            ////Right neighbor
            //if (current.i + 1 < voxelSpace.GetLength(0) && current.j < voxelSpace.GetLength(1) && current.k < voxelSpace.GetLength(2) && current.i + 1 >= 0 && current.j >= 0 && current.k >= 0)
            //{
            //    neighbors.Enqueue(voxelSpace[current.i + 1, current.j, current.k]);
            //}
            ////Front neighbor
            //if (current.i < voxelSpace.GetLength(0) && current.j - 1 < voxelSpace.GetLength(1) && current.k < voxelSpace.GetLength(2) && current.i >= 0 && current.j - 1 >= 0 && current.k >= 0)
            //{
            //    neighbors.Enqueue(voxelSpace[current.i, current.j - 1, current.k]);
            //}
            ////Back neighbor
            //if (current.i < voxelSpace.GetLength(0) && current.j + 1 < voxelSpace.GetLength(1) && current.k < voxelSpace.GetLength(2) && current.i >= 0 && current.j + 1 >= 0 && current.k >= 0)
            //{
            //    neighbors.Enqueue(voxelSpace[current.i, current.j + 1, current.k]);
            //}
            ////Up neighbor
            //if (current.i < voxelSpace.GetLength(0) && current.j < voxelSpace.GetLength(1) && current.k - 1 < voxelSpace.GetLength(2) && current.i >= 0 && current.j >= 0 && current.k - 1 >= 0)
            //{
            //    neighbors.Enqueue(voxelSpace[current.i, current.j, current.k - 1]);
            //}
            ////Down neighbor
            //if (current.i < voxelSpace.GetLength(0) && current.j < voxelSpace.GetLength(1) && current.k + 1 < voxelSpace.GetLength(2) && current.i >= 0 && current.j >= 0 && current.k + 1 >= 0)
            //{
            //    neighbors.Enqueue(voxelSpace[current.i, current.j, current.k + 1]);
            //}
            #endregion

            return neighbors;
        }
        

        private List<Point3d> FindShortestPath(Point3d customized_part_center, Point3d base_part_center, Brep customized_part, Brep currModel)
        {
            Line temp = new Line();
            Curve pipepath = temp.ToNurbsCurve();

            
            Guid customized_guid = myDoc.Objects.AddPoint(customized_part_center);
            //Guid guid2 = myDoc.Objects.AddPoint(base_part_center);
            //myDoc.Views.Redraw();


            #region Preprocessing before A* algorithm
            Index customized_part_center_index = FindClosestPointIndex(customized_part_center, currModel);
            Index base_part_center_index = FindClosestPointIndex(base_part_center, currModel);

            Voxel start = voxelSpace[customized_part_center_index.i, customized_part_center_index.j, customized_part_center_index.k];
            Voxel goal = voxelSpace[base_part_center_index.i, base_part_center_index.j, base_part_center_index.k];

            start.Cost = 0; //TODO: Start is null, please FIX
            start.Distance = 0;
            start.SetDistance(goal.X, goal.Y, goal.Z);

            Queue<Voxel> voxelRoute = new Queue<Voxel>();

            #endregion

            int count = 0;
            foreach(var item in voxelSpace)
            {
                if (item.isTaken == false)
                    count++;
            }


            #region Perform A* algorithm
            SimplePriorityQueue<Voxel, double> frontier = new SimplePriorityQueue<Voxel, double>();
            List<Voxel> searchedVoxels = new List<Voxel>();

            frontier.Enqueue(start, 0);
            Voxel current;

            Line straight_line = new Line(start.X,start.Y,start.Z,goal.X,goal.Y,goal.Z);

            while (frontier.Count != 0)
            {
                current = frontier.Dequeue();

                if (current.Equal(goal))
                {
                    break;
                }

                foreach(var next in GetNeighbors(current.Index, ref goal, ref voxelSpace))
                {
                    double new_cost = current.Cost + 1;
                    if (new_cost < next.Cost || !searchedVoxels.Contains(next))
                    {
                        next.Cost = new_cost;
                        double distance = straight_line.DistanceTo(new Point3d(next.X, next.Y, next.Z), false);
                        double priority = new_cost + next.GetDistance(goal.X, goal.Y, goal.Z) + distance;
                        frontier.Enqueue(next, priority);
                        searchedVoxels.Add(next);
                        next.Parent = current;
                    }
                }
            }
            #endregion

            #region Retrieve to get the searched best route
            Stack<Voxel> bestRoute_Voxel = new Stack<Voxel>();
            List<Point3d> bestRoute_Point3d = new List<Point3d>();
            

            current = goal;
            while (current != start)
            {
                Point3d currentPoint = new Point3d(current.X, current.Y, current.Z);
                bestRoute_Voxel.Push(current);
                bestRoute_Point3d.Add(currentPoint);
                current = current.Parent;
                current.isTaken = true;
            }
            bestRoute_Voxel.Push(current);
            bestRoute_Point3d.Add(new Point3d(current.X, current.Y, current.Z));
            #endregion

            //TODO: Set the accurate location of the start and end
            //bestRoute_Point3d[0] = base_part_center;
            bestRoute_Point3d.Add(customized_part_center);

            Parallel.For(0, bestRoute_Point3d.Count, i =>{
                myDoc.Objects.AddPoint(bestRoute_Point3d[i]);
            });

            #region Use the result of A* to generate routes that are less curvy

            #region Method 1: Retrive route from the start to the end, it checks if the pipe generated by the straight line from current point to end point is not causing intersection
            List<Point3d> interpolatedRoute_Point3d = new List<Point3d>();
            interpolatedRoute_Point3d.Add(bestRoute_Point3d[0]);

            Boolean isIntersected = false;
            int index = 1;
            while(isIntersected == true || !interpolatedRoute_Point3d[interpolatedRoute_Point3d.Count - 1].Equals(bestRoute_Point3d[bestRoute_Point3d.Count - 1]))
            {
                Point3d endPoint = bestRoute_Point3d[bestRoute_Point3d.Count - index];
                //Create a pipe for the current section of the line
                Curve betterRoute = (new Line(interpolatedRoute_Point3d[interpolatedRoute_Point3d.Count - 1], endPoint)).ToNurbsCurve();

                if(betterRoute == null)
                {
                    interpolatedRoute_Point3d.Add(bestRoute_Point3d[bestRoute_Point3d.Count - index + 1]);
                    index = 1;
                    isIntersected = false;
                    continue;
                }
                Brep[] pipe = Brep.CreatePipe(betterRoute, 2, true, PipeCapMode.Flat, true, myDoc.ModelAbsoluteTolerance, myDoc.ModelAngleToleranceRadians);


                //Check if the Pipe is intersecting with other breps
                var allObjects = new List<RhinoObject>(myDoc.Objects.GetObjectList(ObjectType.Brep));
                foreach (var item in allObjects)
                {
                    Guid guid = item.Id;
                    ObjRef currObj = new ObjRef(guid);
                    Brep brep = currObj.Brep();

                    if (brep != null)
                    {
                        //Ignore the current model and customized part
                        if (brep.IsDuplicate(currModel, myDoc.ModelAbsoluteTolerance) || brep.IsDuplicate(customized_part, myDoc.ModelAbsoluteTolerance))
                        {
                            continue;
                        }

                        //Check for intersection, go to the next brep if no intersection is founded, else, break and report intersection found
                        if (Intersection.BrepBrep(brep, pipe[0], myDoc.ModelAbsoluteTolerance, out Curve[] intersectionCurves, out Point3d[] intersectionPoints))
                        {
                            if (intersectionCurves.Length == 0 && intersectionPoints.Length == 0)
                            {
                                isIntersected = false;
                                continue;
                            }
                            else
                            {
                                isIntersected = true;
                                index += 1;
                                break;
                            }
                        }
                    }
                }

                if(isIntersected)
                {
                    continue;
                }

                interpolatedRoute_Point3d.Add(endPoint);
                index = 1;
            }
            #endregion
            return interpolatedRoute_Point3d;

            #endregion




            //#region Method 2: Test curve with less control points, if it's not causing intersection, then return the curve with less control point
            ////for (int i = -1; i < 10; i++) //Let's try it 10 times
            ////{
            ////    List<Point3d> Route = new List<Point3d>();
            ////    Route.Add(base_part_center);

            ////    //Add control points in between the start and end point, need to add at least one point
            ////    if (i >= 0)
            ////    {
            ////        for (int j = 1; j < i + 2; j++)
            ////        {
            ////            int subCurveNumbers = bestRoute_Point3d.Count / (i + 2);
            ////            int index = j * subCurveNumbers;

            ////            if (index >= bestRoute_Point3d.Count)
            ////                index = bestRoute_Point3d.Count - 1;

            ////            Route.Add(bestRoute_Point3d[index]);
            ////        }
            ////    }

            ////    Route.Add(customized_part_center);

            ////    //Get the surface normal of customized_part_center and base_part_center

            ////    Curve betterRoute = Curve.CreateControlPointCurve(Route, 3);

            ////    Brep[] pipe = Brep.CreatePipe(betterRoute, 3.5, true, PipeCapMode.Flat, true, myDoc.ModelAbsoluteTolerance, myDoc.ModelAngleToleranceRadians);

            ////    //Check if the Pipe is intersecting with other breps
            ////    Boolean isIntersected = false;
            ////    var allObjects = new List<RhinoObject>(myDoc.Objects.GetObjectList(ObjectType.Brep));
            ////    foreach (var item in allObjects)
            ////    {
            ////        Guid guid = item.Id;
            ////        ObjRef currObj = new ObjRef(guid);
            ////        Brep brep = currObj.Brep();

            ////        if (brep != null)
            ////        {
            ////            //Ignore the current model and customized part
            ////            if (brep.IsDuplicate(currModel, myDoc.ModelAbsoluteTolerance) || brep.IsDuplicate(customized_part, myDoc.ModelAbsoluteTolerance))
            ////            {
            ////                continue;
            ////            }

            ////            //Check for intersection, go to the next brep if no intersection is founded, else, break and report intersection found
            ////            if (Intersection.BrepBrep(brep, pipe[0], myDoc.ModelAbsoluteTolerance, out Curve[] intersectionCurves, out Point3d[] intersectionPoints))
            ////            {
            ////                if (intersectionCurves.Length == 0 && intersectionPoints.Length == 0)
            ////                    continue;
            ////                else
            ////                {
            ////                    isIntersected = true;
            ////                    break;
            ////                }
            ////            }
            ////        }
            ////    }

            ////    if (isIntersected)
            ////        continue;

            ////    return betterRoute;
            ////}
            //#endregion
            //#endregion





            //Curve bestRoute = Curve.CreateControlPointCurve(bestRoute_Point3d, 3);

            ////TODO: To Adjust the start and end tangent to the surface normal of the start and end point
            ////Brep.ClosestPoint(out surface)
            ////Surface.NormalAt()
            ////Reset the end tangent of the curve


            //bestRoute = bestRoute.Smooth(1, true, true, true, true, 0);

            //return bestRoute;
        }

        /// <summary>
        /// Test if the curve is completely  inside a brep in Rhino
        /// </summary>
        /// <param name="crv">the target curve</param>
        /// <param name="body">the target brep</param>
        /// <returns></returns>
        public bool CurveInModel(Curve crv, Brep body, double diameter)
        {
            bool result = false;
            double radius = diameter / 2;

            var pipes = Brep.CreatePipe(crv, radius, false, PipeCapMode.Round, false, myDoc.ModelAbsoluteTolerance, myDoc.ModelAngleToleranceRadians);

            //myDoc.Objects.AddBrep(pipes[0], orangeAttribute);
            //myDoc.Views.Redraw();

            var intersections = Brep.CreateBooleanDifference(pipes[0], body, myDoc.ModelAbsoluteTolerance);

            if (intersections.Length > 0)
                result = false;
            else
                result = true;

            return result;

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
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("F35ED144-E27C-403B-95FF-B8BD8A720F17"); }
        }
    }
}