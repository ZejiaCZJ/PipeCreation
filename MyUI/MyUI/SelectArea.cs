using System;
using System.Collections.Generic;
using System.Windows.Forms;

using Grasshopper.GUI;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino;
using Rhino.UI;
using Rhino.Input.Custom;
using Rhino.Commands;
using Rhino.Geometry.Intersect;
using System.Linq;
using System.Drawing;
using System.Threading;
using Rhino.Collections;

namespace MyUI
{

    public class SelectArea : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        /// 
        private Brep currModel;
        private List<Point3d> surfacePts;
        private List<Point3d> selectedPts;
        private Guid currModelObjId;
        private RhinoDoc myDoc;
        private bool end_button_clicked;
        ObjectAttributes solidAttribute, orangeAttribute, redAttribute, yellowAttribute;


        //private Brep sphere_body;
        //private Brep[] differenceBreps;

        public SelectArea()
          : base("SelectArea", "Area",
              "modify the area of the parameter to the selected model",
              "MyUI", "Main")
        {
            myDoc = RhinoDoc.ActiveDoc;
            currModelObjId = Guid.Empty;
            selectedPts = new List<Point3d>();
            surfacePts = new List<Point3d>();

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

            int orangeIndex = myDoc.Materials.Add();
            Rhino.DocObjects.Material orangeMat = myDoc.Materials[orangeIndex];
            orangeMat.DiffuseColor = System.Drawing.Color.Orange;
            orangeMat.Transparency = 0.3;
            orangeMat.SpecularColor = System.Drawing.Color.Orange;
            orangeMat.CommitChanges();
            orangeAttribute = new ObjectAttributes();
            //orangeAttribute.LayerIndex = 3;
            orangeAttribute.MaterialIndex = orangeIndex;
            orangeAttribute.MaterialSource = Rhino.DocObjects.ObjectMaterialSource.MaterialFromObject;
            orangeAttribute.ObjectColor = Color.Orange;
            orangeAttribute.ColorSource = ObjectColorSource.ColorFromObject;

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
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Start Button", "SB", "The button that users click if they need to modify the parameter in Rhino", GH_ParamAccess.item);
            //pManager.AddGenericParameter("Shared Data", "Data", "The data that shared among all parameters", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Customized Part", "Part", "The brep of the customized part", GH_ParamAccess.item);
            pManager.AddGenericParameter("Base Part Center", "Base", "The center point of the Base part", GH_ParamAccess.item);
            pManager.AddGenericParameter("The Model brep", "Model", "The brep of the Model", GH_ParamAccess.item);
            pManager.AddGenericParameter("Selected Area box", "box", "The selected Area box Guid", GH_ParamAccess.item);
            //pManager.AddTextParameter("Successful", "Successful", "Text to check if succesful", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool start_button_clicked = false;
            end_button_clicked = false;
            SharedData sharedData = new SharedData();


            if (!DA.GetData(0, ref start_button_clicked)) 
                return;
            //if (!DA.GetData(1, ref sharedData))
            //    return;
            //DA.SetData(3, sharedData.voxelSpace);
            

            if (start_button_clicked)
            {
                ObjRef objSel_ref1;
            


                var rc = RhinoGet.GetOneObject("Select a model (geometry): ", false, ObjectType.AnyObject, out objSel_ref1);
                if (rc == Rhino.Commands.Result.Success)
                {
                    currModelObjId = objSel_ref1.ObjectId;
                    ObjRef currObj = new ObjRef(currModelObjId);
                    currModel = currObj.Brep(); //The model body

                    if(currObj.Geometry().ObjectType == ObjectType.Mesh)
                    {
                        //Mesh
                        Mesh currModel_Mesh = currObj.Mesh();

                        //TODO: Convert Mesh into Brep; or just throw an error to user saying that only breps are allowed 
                        currModel = Brep.CreateFromMesh(currModel_Mesh, false);
                        currModelObjId = myDoc.Objects.AddBrep(currModel);
                        myDoc.Objects.Delete(currObj.ObjectId, false);

                        myDoc.Views.Redraw();
                    }

                    #region Make sure the brep is closed and manifold

                    //if (!currModel.IsManifold)
                    //{
                    //    Mesh[] currModel_MeshList = Mesh.CreateFromBrep(currModel, new MeshingParameters());
                    //    Mesh currModel_Mesh = new Mesh();
                    //    currModel_Mesh.Append(currModel_MeshList);

                    //    currModel_Mesh = currModel_Mesh.ExtractNonManifoldEdges(false);

                    //    currModel = Brep.CreateFromMesh(currModel_Mesh, false);
                    //    //Delete the old brep and add the new brep
                    //    myDoc.Objects.Delete(currModelObjId, false);
                    //    currModelObjId = myDoc.Objects.AddBrep(currModel);
                    //}

                    #endregion


                   
                    #region Generate the base PCB part
                    BoundingBox boundingBox = currModel.GetBoundingBox(true);
                    double base_z = boundingBox.Min.Z;
                    double base_x = (boundingBox.Max.X - boundingBox.Min.X) / 2 + boundingBox.Min.X;
                    double base_y = (boundingBox.Max.Y - boundingBox.Min.Y) / 2 + boundingBox.Min.Y;
                    Point3d base_center = new Point3d(base_x, base_y, base_z);

                    DA.SetData(1, base_center);
                    #endregion


                    #region new point creating method
                    if (currModel == null)
                        return;

                    double w = boundingBox.Max.X - boundingBox.Min.X;
                    double l = boundingBox.Max.Y - boundingBox.Min.Y;
                    double h = boundingBox.Max.Z - boundingBox.Min.Z;
                    double offset = 5;


                    // Create a x-y plane to intersect with the current model from top to bottom
                    for (int i = 0; i < h; i += 5)
                    {
                        Point3d Origin = new Point3d(w / 2, l / 2, i);
                        Point3d xPoint = new Point3d(boundingBox.Max.X + offset, l / 2, i);
                        Point3d yPoint = new Point3d(w / 2, boundingBox.Max.Y + offset, i);

                        Plane plane = new Plane(Origin, xPoint, yPoint);
                        PlaneSurface planeSurface = PlaneSurface.CreateThroughBox(plane, boundingBox);

                        Intersection.BrepSurface(currModel, planeSurface, myDoc.ModelAbsoluteTolerance, out Curve[] intersectionCurves, out Point3d[] intersectionPoints);

                        //Create Points on the Curve
                        if (intersectionCurves != null)
                        {
                            if (intersectionCurves.Length != 0)
                            {
                                foreach (Curve curve in intersectionCurves)
                                {
                                    Double[] curveParams = curve.DivideByLength(2, true, out Point3d[] points);
                                    surfacePts.AddRange(points);
                                }
                            }
                        }
                    }

                    #endregion


                    #region Create interactive dots around the model body for users to select

                    //if (currModel == null)
                    //    return;

                    ////// Convert the current body to a mesh
                    ////var default_mesh_params = MeshingParameters.Default;
                    ////var minimal = MeshingParameters.Minimal;

                    ////var meshes = Mesh.CreateFromBrep(currModel, default_mesh_params);
                    ////if (meshes == null || meshes.Length == 0)
                    ////    return;

                    ////// Convert all quads in the mesh to triangles
                    ////var brep_mesh = new Mesh();
                    ////foreach (var mesh in meshes)
                    ////    brep_mesh.Append(mesh);
                    ////brep_mesh.Faces.ConvertQuadsToTriangles();


                    //// Find the candidate positions to place dots
                    //BoundingBox meshBox = currModel.GetBoundingBox(true);
                    //double w = meshBox.Max.X - meshBox.Min.X;
                    //double l = meshBox.Max.Y - meshBox.Min.Y;
                    //double h = meshBox.Max.Z - meshBox.Min.Z;
                    //double offset = 5;

                    ////For each point in x-y plane
                    //for (int i = 0; i < w; i++)
                    //{
                    //    for (int j = 0; j < l; j++)
                    //    {
                    //        // Create a nurbs curve of a straight line in respect to z axis with same x,y coordinate
                    //        Point3d ptInSpaceOri = new Point3d(i + meshBox.Min.X, j + meshBox.Min.Y, meshBox.Min.Z - offset);
                    //        Point3d ptInSpaceEnd = new Point3d(i + meshBox.Min.X, j + meshBox.Min.Y, meshBox.Max.Z + offset);

                    //        Line ray_ln = new Line(ptInSpaceOri, ptInSpaceEnd);
                    //        Curve ray_crv = ray_ln.ToNurbsCurve();

                    //        Curve[] overlapCrvs;
                    //        Point3d[] overlapPts;



                    //        // Get the 3D points of the intersection between the line and the surface of the current model
                    //        Intersection.CurveBrep(ray_crv, currModel, myDoc.ModelAbsoluteTolerance, out overlapCrvs, out overlapPts);

                    //        // Store the candidate positions
                    //        if (overlapPts != null)
                    //        {
                    //            if (overlapPts.Count() != 0)
                    //            {
                    //                foreach (Point3d p in overlapPts)
                    //                {
                    //                    surfacePts.Add(p);
                    //                }
                    //            }
                    //        }
                    //    }
                    //}

                    //// For each point in x-z plane
                    //for (int i = 0; i < w; i++)
                    //{
                    //    for (int t = 0; t < h; t++)
                    //    {
                    //        // Create a nurbs curve of a straight line in respect to z axis with same x,y coordinate
                    //        Point3d ptInSpaceOri = new Point3d(i + meshBox.Min.X, meshBox.Min.Y - offset, meshBox.Min.Z + t);
                    //        Point3d ptInSpaceEnd = new Point3d(i + meshBox.Min.X, meshBox.Max.Y + offset, meshBox.Min.Z + t);

                    //        Line ray_ln = new Line(ptInSpaceOri, ptInSpaceEnd);
                    //        Curve ray_crv = ray_ln.ToNurbsCurve();

                    //        Curve[] overlapCrvs;
                    //        Point3d[] overlapPts;

                    //        Intersection.CurveBrep(ray_crv, currModel, myDoc.ModelAbsoluteTolerance, out overlapCrvs, out overlapPts);

                    //        if (overlapPts != null)
                    //        {
                    //            if (overlapPts.Count() != 0)
                    //            {
                    //                foreach (Point3d p in overlapPts)
                    //                {
                    //                    if (surfacePts.IndexOf(p) == -1)
                    //                        surfacePts.Add(p);
                    //                }
                    //            }
                    //        }
                    //    }
                    //}

                    //// For each point in y-z plane
                    //for (int j = 0; j < l; j++)
                    //{
                    //    for (int t = 0; t < h; t++)
                    //    {

                    //        Point3d ptInSpaceOri = new Point3d(meshBox.Min.X - offset, j + meshBox.Min.Y, meshBox.Min.Z + t);
                    //        Point3d ptInSpaceEnd = new Point3d(meshBox.Max.X + offset, j + meshBox.Min.Y, meshBox.Min.Z + t);

                    //        Line ray_ln = new Line(ptInSpaceOri, ptInSpaceEnd);
                    //        Curve ray_crv = ray_ln.ToNurbsCurve();

                    //        Curve[] overlapCrvs;
                    //        Point3d[] overlapPts;

                    //        Intersection.CurveBrep(ray_crv, currModel, myDoc.ModelAbsoluteTolerance, out overlapCrvs, out overlapPts);

                    //        if (overlapPts != null)
                    //        {
                    //            if (overlapPts.Count() != 0)
                    //            {
                    //                foreach (Point3d p in overlapPts)
                    //                {
                    //                    if (surfacePts.IndexOf(p) == -1)
                    //                        surfacePts.Add(p);
                    //                }
                    //            }
                    //        }

                    //        //myDoc.Objects.AddCurve(ray_crv, redAttribute);

                    //    }
                    //}



                    // Create a copy of the current model to put on dots
                    Brep solidDupBrep = currModel.DuplicateBrep();
                    Guid dupObjID = myDoc.Objects.AddBrep(solidDupBrep, yellowAttribute);
                    myDoc.Objects.Hide(currModelObjId, true);

                    // Put dots on the copy
                    List<Guid> pts_normals = new List<Guid>();
                    foreach (Point3d point in surfacePts)
                    {
                        Guid pointID = myDoc.Objects.AddPoint(point);
                        pts_normals.Add(pointID);
                    }
                    myDoc.Views.Redraw();

                    #endregion

                    CurveList selected_area = new CurveList();
                    List<Brep> selected_box = new List<Brep>();

                    #region ask the user to select points to generate the area of the parameter
                    List<Guid> rectanglesGuid = new List<Guid>();
                    while (!end_button_clicked)
                    {
                        ObjRef pointRef;

                        var getSelectedPts = RhinoGet.GetOneObject("Please select points for a pipe exit, press ENTER when finished: ", false, ObjectType.Point, out pointRef);
                        

                        if(pointRef != null)
                        {
                            Point3d tempPt = new Point3d(pointRef.Point().Location.X, pointRef.Point().Location.Y, pointRef.Point().Location.Z);
                            double x = pointRef.Point().Location.X;
                            double y = pointRef.Point().Location.Y;
                            double z = pointRef.Point().Location.Z;

                            //Check if the selected point is in selectedPts
                            //1. If so, Get rid of the bounding box
                            //2. If not, store the selected point and display bounding box

                            selectedPts.Add(tempPt);

                            Guid tempPt_ID = pointRef.ObjectId;


                            #region Display the selected area with red 2D bounding box
                            // Create a 1*1 solid box with the center coordinate with the dot's xyz position
                            BoundingBox box = new BoundingBox(x - 0.5, y - 0.5, z - 0.5, x + 0.5, y + 0.5, z + 0.5);
                            Curve[] curve;
                            Point3d[] point;
                            Intersection.BrepBrep(box.ToBrep(), currModel, myDoc.ModelAbsoluteTolerance, out curve, out point);
                            foreach (var c in curve)
                                selected_area.Add(c);

                            //Add boundary rectangle into the Rhino view with red color
                            foreach (var c in curve)
                            {
                                Guid temp = myDoc.Objects.AddCurve(c, redAttribute);
                                rectanglesGuid.Add(temp);
                            }

                            selected_box.Add(box.ToBrep());

                            myDoc.Views.Redraw();

                            #endregion

                            RhinoApp.KeyboardEvent += OnKeyboardEvent;
                        }
                        
                    }
                    //DA.SetData(1, !end_button_clicked);
                    #endregion

                    //Kill all dots and duplicate brep, Show the original brep
                    myDoc.Objects.Delete(dupObjID, true);
                    myDoc.Objects.Show(currModelObjId, true);
                    foreach (var ptsID in pts_normals)
                    {
                        myDoc.Objects.Delete(ptsID, true);
                    }

                    //Kill all rectangle
                    foreach (var ptsID in rectanglesGuid)
                    {
                        myDoc.Objects.Delete(ptsID, true);
                    }

                    //Show the User interact parameter
                    Brep new_part = Brep.MergeBreps(selected_box.ToArray(), myDoc.ModelAbsoluteTolerance);
                    Guid newPartGuid = myDoc.Objects.AddBrep(new_part, redAttribute);
                    myDoc.Views.Redraw();

                    DA.SetData(0, new_part);
                    DA.SetData(2, currModel);

                    //DA.SetData(4, "This selection is successful");

                    //Pass selected_box to output to auto-generate route

                    
                }

            }
                    
        }

        public void OnKeyboardEvent(int key)
        {
            if(key == 13)
            {
                end_button_clicked = true;
            }
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
            get { return new Guid("B9F30653-B0A5-4ADB-AF06-B1970F70C93B"); }
        }
    }
}