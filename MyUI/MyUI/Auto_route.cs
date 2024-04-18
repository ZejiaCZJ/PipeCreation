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

        public Voxel()
        {
            X=0; Y=0; Z=0; Cost=0; Distance=0; isTaken = false;
        }
        
        //This function set the distance with Manhattan distance
        public void SetDistance(double targetX, double targetY, double targetZ)
        {
            this.Distance = Math.Abs(targetX - X) + Math.Abs(targetY - Y) + Math.Abs(targetZ - Z);
        }

        //This function get the distance with Manhattan distance
        public double GetDistance(double targetX, double targetY, double targetZ)
        {
            return Math.Abs(targetX - X) + Math.Abs(targetY - Y) + Math.Abs(targetZ - Z);//Manhattan
            //return Math.Sqrt(Math.Pow(targetX - X,2) + Math.Pow(targetY - Y,2) + Math.Pow(targetZ - Z,2)); //Euclidean
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

            //TODO: please adjust the size when the actual size is given
            pcbWidth = 20;
            pcbHeight = 20;
            pipeRadius = 3.5;
            voxelSpace_offset = 0; //Each voxel is now stands for (offset+1)*(offset+1)*(offset+1)


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


            
            Point3d customized_part_center = customized_part.GetBoundingBox(true).Center;

            GetVoxelSpace(customized_part, currModel);

            //Find the Pipe exit location
            if(pipeExitPts.Count == 0)
                GetPipeExits(base_part_center, currModel);

            Point3d pipeExit = new Point3d();
            foreach (var item in pipeExitPts)
            {
                if(item.isTaken == false)
                {
                    pipeExit = item.location;
                    item.isTaken = true;
                    break;
                }
            }
            


            Point3d point = new Point3d(voxelSpace[10, 10, 10].X, voxelSpace[10, 10, 10].Y, voxelSpace[10, 10, 10].Z);
            //myDoc.Objects.AddPoint(point);
            //myDoc.Views.Redraw();
            
            Curve bestRoute = FindShortestPath(customized_part_center, pipeExit, customized_part,currModel);
            
            #region Create Pipes
            //Cut the first 5mm of the bestRoute to generate the inner pipe
            //Double[] divisionParameters = bestRoute.DivideByLength(5, true, out Point3d[] points);
            Curve lightGuidePipeRoute = bestRoute.Trim(CurveEnd.Start, bestRoute.GetLength() - 7);
            Curve soluablePipeRoute = bestRoute.Trim(CurveEnd.End, 7);

            List<GeometryBase> geometryBases = new List<GeometryBase>();
            geometryBases.Add(customized_part);

            lightGuidePipeRoute = lightGuidePipeRoute.Extend(CurveEnd.End, 100, CurveExtensionStyle.Line);
            soluablePipeRoute = soluablePipeRoute.Extend(CurveEnd.Start, 100, CurveExtensionStyle.Line);

            Brep[] soluablePipe = Brep.CreatePipe(soluablePipeRoute, 2, true, PipeCapMode.Flat, true, myDoc.ModelAbsoluteTolerance, myDoc.ModelAngleToleranceRadians);
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
            foreach(var exit in pipeExitPts)
            {
                if(exit.isTaken == false)
                {
                    finish = false; break;
                }
            }

            if(finish)
            {
                List<Brep> firstSet = new List<Brep>();
                firstSet.Add(currModel);

                Brep[] currModel_new = Brep.CreateBooleanDifference(firstSet, allPipes, myDoc.ModelAbsoluteTolerance, false);
                myDoc.Objects.AddBrep(currModel_new[0]);
            }
            #endregion

            #region Delete current model and customized part bounding box
            var allObjects = new List<RhinoObject>(myDoc.Objects.GetObjectList(ObjectType.AnyObject));
            foreach (var item in allObjects)
            {
                Guid guid = item.Id;
                ObjRef currObj = new ObjRef(guid);
                Brep brep = currObj.Brep();

                if (brep != null)
                {
                    if (brep.IsDuplicate(customized_part, myDoc.ModelAbsoluteTolerance) || brep.IsDuplicate(currModel, myDoc.ModelAbsoluteTolerance))
                    {
                        myDoc.Objects.Delete(guid, true);
                        break;
                    }
                }
            }

            myDoc.Views.Redraw();
            #endregion
            
        }

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

        private void GetVoxelSpace(Brep customized_part, Brep currModel)
        {
            var allObjects = new List<RhinoObject>(myDoc.Objects.GetObjectList(ObjectType.AnyObject));
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

            int w = (int)Math.Abs(boundingBox.Max.X - boundingBox.Min.X) / (voxelSpace_offset + 1); //width
            int l = (int)Math.Abs(boundingBox.Max.Y - boundingBox.Min.Y) / (voxelSpace_offset + 1); //length
            int h = (int)Math.Abs(boundingBox.Max.Z - boundingBox.Min.Z) / (voxelSpace_offset + 1); //height

            

            voxelSpace = new Voxel[w, l, h];

            #region Initialize the voxel space element-wise
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < l; j++)
                {
                    for (int k = 0; k < h; k++)
                    {
                        Point3d currentPt = new Point3d((voxelSpace_offset + 1) * i + boundingBox.Min.X, (voxelSpace_offset + 1) * j + boundingBox.Min.Y, (voxelSpace_offset + 1) * k + boundingBox.Min.Z);
                        voxelSpace[i, j, k] = new Voxel
                        {
                            X = (voxelSpace_offset + 1) * i + boundingBox.Min.X,
                            Y = (voxelSpace_offset + 1) * j + boundingBox.Min.Y,
                            Z = (voxelSpace_offset + 1) * k + boundingBox.Min.Z,
                            isTaken = !currModel.IsPointInside(currentPt, myDoc.ModelAbsoluteTolerance, true),
                            Index = new Index(i, j, k)
                        };

                        //Traverse all objects in the Rhino View to check for intersection. 
                        Boolean intersected = false;
                        Double maximumDistance = 4;//TODO: Try different distance metric to get better result. Euclidean: Math.Sqrt(Math.Pow((voxelSpace_offset + 1), 2) * 3)

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
            #endregion
        }



        private Index FindClosestPointIndex(Point3d point, Brep currModel)
        {
            Index index = new Index();

            //Calculate the approximate index of Point3d. Then obtain the precise index that has the smallest distance within the 2*2*2 bounding box of the Point3d
            BoundingBox boundingBox = currModel.GetBoundingBox(true);

            #region Calculate an estimated index
            double w = boundingBox.Max.X - boundingBox.Min.X; //width
            double h = boundingBox.Max.Y - boundingBox.Min.Y; //length
            double l = boundingBox.Max.Z - boundingBox.Min.Z; //height
            
            int estimated_i = (int)((point.X - boundingBox.Min.X) / (voxelSpace_offset + 1));
            int estimated_j = (int)((point.Y - boundingBox.Min.Y) / (voxelSpace_offset + 1));
            int estimated_k = (int)((point.Z - boundingBox.Min.Z) / (voxelSpace_offset + 1));

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

        private Point3d FindClosestPointIndex_isNotTaken(Point3d point, Brep currModel)
        {
            Index index = new Index();

            //Calculate the approximate index of Point3d. Then obtain the precise index that has the smallest distance within the 2*2*2 bounding box of the Point3d
            BoundingBox boundingBox = currModel.GetBoundingBox(true);

            #region Calculate an estimated index
            double w = boundingBox.Max.X - boundingBox.Min.X; //width
            double h = boundingBox.Max.Y - boundingBox.Min.Y; //length
            double l = boundingBox.Max.Z - boundingBox.Min.Z; //height

            int estimated_i = (int)(point.X - boundingBox.Min.X);
            int estimated_j = (int)(point.Y - boundingBox.Min.Y);
            int estimated_k = (int)(point.Z - boundingBox.Min.Z);

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
            double smallestDistance = 0;

            for (int i = estimated_i - 5; i < estimated_i + 6; i++)
            {
                for (int j = estimated_j - 5; j < estimated_j + 6; j++)
                {
                    for (int k = estimated_k - 5; k < estimated_k + 6; k++)
                    {
                        if (i < voxelSpace.GetLength(0) && j < voxelSpace.GetLength(1) && k < voxelSpace.GetLength(2) && i >= 0 && j >= 0 && k >= 0 && voxelSpace[i,j,k].isTaken == false)
                        {
                            //Potential error: when the input point is the goal, it may create a problem
                            if(smallestDistance == 0)
                            {
                                index.i = i;
                                index.j = j;
                                index.k = k;
                                smallestDistance = voxelSpace[i, j, k].GetDistance(point.X, point.Y, point.Z);
                            }
                            
                            double distance = voxelSpace[i, j, k].GetDistance(point.X, point.Y, point.Z);

                            if (distance < smallestDistance)
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

            Point3d bestPoint = new Point3d(voxelSpace[index.i, index.j, index.k].X, voxelSpace[index.i, index.j, index.k].Y, voxelSpace[index.i, index.j, index.k].Z);

            return bestPoint;
        }

        private Queue<Voxel> GetNeighbors(Index current, ref Voxel goal,ref Voxel[,,] voxelSpace)
        {
            #region Get all neighbors
            Queue<Voxel> neighbors = new Queue<Voxel>();

            //up,down,left,right,front,back voxels of the current
            for (int i = current.i - 1; i < current.i + 2; i++)
            {
                for (int j = current.j - 1; j < current.j + 2; j++)
                {
                    for (int k = current.k - 1; k < current.k + 2; k++)
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
            // && voxelSpace[i,j,k].isTaken == false
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

        private Curve FindShortestPath(Point3d customized_part_center, Point3d base_part_center, Brep customized_part, Brep currModel)
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

            //myDoc.Objects.AddPoint(new Point3d(start.X, start.Y, start.Z));

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

            while (frontier.Count != 0)
            {
                current = frontier.Dequeue();

                if (current.Equal(goal))
                {
                    break;
                }
                foreach (Voxel next in GetNeighbors(current.Index, ref goal, ref voxelSpace))
                {
                    double new_cost = current.Cost + 1;
                    if (new_cost < next.Cost || !searchedVoxels.Contains(next))
                    {
                        next.Cost = new_cost;
                        double priority = new_cost + next.GetDistance(goal.X, goal.Y, goal.Z);
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
                bestRoute_Voxel.Push(current);
                bestRoute_Point3d.Add(new Point3d(current.X, current.Y, current.Z));
                current = current.Parent;
                current.isTaken = true;
            }
            bestRoute_Voxel.Push(current);
            bestRoute_Point3d.Add(new Point3d(current.X, current.Y, current.Z));
            #endregion

            //TODO: Set the accurate location of the start and end
            //bestRoute_Point3d[0] = base_part_center;
            bestRoute_Point3d.Add(customized_part_center);

            //Test curve with less control points, if it's not causing intersection, then return the curve with less control point
            for (int i = 0; i < 10; i++) //Let's try it 10 times
            {
                List<Point3d> Route = new List<Point3d>();
                Route.Add(base_part_center);

                //Add control points in between the start and end point, need to add at least one point
                for (int j = 1; j < i + 2; j++)
                {
                    int subCurveNumbers = bestRoute_Point3d.Count / (i + 2);
                    int index = j * subCurveNumbers;

                    if (index >= bestRoute_Point3d.Count)
                        index = bestRoute_Point3d.Count - 1;

                    Route.Add(bestRoute_Point3d[index]);
                }
                Route.Add(customized_part_center);

                //Get the surface normal of customized_part_center and base_part_center

                Curve betterRoute = Curve.CreateControlPointCurve(Route, 3);

                Brep[] pipe = Brep.CreatePipe(betterRoute, 3.5, true, PipeCapMode.Flat, true, myDoc.ModelAbsoluteTolerance, myDoc.ModelAngleToleranceRadians);

                //Check if the Pipe is intersecting with other breps
                Boolean isIntersected = false;
                var allObjects = new List<RhinoObject>(myDoc.Objects.GetObjectList(ObjectType.AnyObject));
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
                                continue;
                            else
                            {
                                isIntersected = true;
                                break;
                            }
                        }
                    }
                }

                if (isIntersected)
                    continue;


                return betterRoute;
            }


            Curve bestRoute = Curve.CreateControlPointCurve(bestRoute_Point3d, 3);

            //TODO: To Adjust the start and end tangent to the surface normal of the start and end point
            //Brep.ClosestPoint(out surface)
            //Surface.NormalAt()
            //Reset the end tangent of the curve
            

            bestRoute = bestRoute.Smooth(1, true, true, true, true, 0);
             
            return bestRoute;
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