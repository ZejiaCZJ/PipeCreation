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
using MyUI.Geometry;

namespace MyUI
{
    public class AddGear : GH_Component
    {
        private Point3d customized_part_location;
        private Point3d center_point = new Point3d(0, 0, 0);
        private Vector3d gear_direction = new Vector3d(0, 0, 0);
        private Vector3d gear_x_dir = new Vector3d(0, 0, 0);
        private int teethNum = 20;
        private double mod = 1.5;
        private double pressure_angle = 20;
        private double Thickness = 1;
        private double selfRotAngle = 0;
        private double coneAngle = 90;
        private bool movable = true;

        private Brep currModel;
        private List<Point3d> surfacePts;
        private List<Point3d> selectedPts;
        private Guid currModelObjId;
        private RhinoDoc myDoc;
        ObjectAttributes solidAttribute, orangeAttribute, redAttribute, yellowAttribute;

        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public AddGear()
          : base("AddGear", "AddGear",
              "This component adds a gear",
              "MyUI", "Main")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Button", "B", "Button to create gear", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Boolean isPressed = false;
            if (!DA.GetData(0, ref isPressed))
            {
                return;
            }

            if (isPressed)
            {
                selectGearExit();

                if (customized_part_location.Z == currModel.GetBoundingBox(true).Max.Z)
                    center_point.Z = customized_part_location.Z - 1;
                else
                    center_point.Z = customized_part_location.Z + 1;

                center_point.X = customized_part_location.X;
                center_point.Y = customized_part_location.Y;

                SpurGear gear = new SpurGear(center_point, gear_direction, gear_x_dir, teethNum, mod, pressure_angle, Thickness, selfRotAngle, movable);

                //在gear旁边弄个box，看ipad里面的数据

                //在gear中间加两个pipe， 连通到customized part location外面，大的pipe比小的pipe大0.7mm



                RhinoDoc myDoc = RhinoDoc.ActiveDoc;

                myDoc.Objects.Add(gear.Model);
            }
        }


        private void selectGearExit()
        {
            ObjRef objSel_ref1;



            var rc = RhinoGet.GetOneObject("Select a model (geometry): ", false, ObjectType.AnyObject, out objSel_ref1);
            if (rc == Rhino.Commands.Result.Success)
            {
                currModelObjId = objSel_ref1.ObjectId;
                ObjRef currObj = new ObjRef(currModelObjId);
                currModel = currObj.Brep(); //The model body

                if (currObj.Geometry().ObjectType == ObjectType.Mesh)
                {
                    //Mesh
                    Mesh currModel_Mesh = currObj.Mesh();

                    //TODO: Convert Mesh into Brep; or just throw an error to user saying that only breps are allowed 
                    currModel = Brep.CreateFromMesh(currModel_Mesh, false);
                    currModelObjId = myDoc.Objects.AddBrep(currModel);
                    myDoc.Objects.Delete(currObj.ObjectId, false);

                    myDoc.Views.Redraw();
                }


                #region Generate the base PCB part
                BoundingBox boundingBox = currModel.GetBoundingBox(true);
                double base_z = boundingBox.Min.Z;
                double base_x = (boundingBox.Max.X - boundingBox.Min.X) / 2 + boundingBox.Min.X;
                double base_y = (boundingBox.Max.Y - boundingBox.Min.Y) / 2 + boundingBox.Min.Y;
                Point3d base_center = new Point3d(base_x, base_y, base_z);
                #endregion


                #region Create interactive dots around the model body for users to select

                if (currModel == null)
                    return;


                // Find the candidate positions to place dots
                BoundingBox meshBox = currModel.GetBoundingBox(true);
                double w = meshBox.Max.X - meshBox.Min.X;
                double l = meshBox.Max.Y - meshBox.Min.Y;
                double h = meshBox.Max.Z - meshBox.Min.Z;
                double offset = 5;

                //For each point in x-y plane
                for (int i = 31; i < w-30; i++)
                {
                    for (int j = 31; j < l-30; j++)
                    {
                        // Create a nurbs curve of a straight line in respect to z axis with same x,y coordinate
                        Point3d ptInSpaceOri = new Point3d(i + meshBox.Min.X, j + meshBox.Min.Y, meshBox.Min.Z - offset);
                        Point3d ptInSpaceEnd = new Point3d(i + meshBox.Min.X, j + meshBox.Min.Y, meshBox.Max.Z + offset);

                        Line ray_ln = new Line(ptInSpaceOri, ptInSpaceEnd);
                        Curve ray_crv = ray_ln.ToNurbsCurve();

                        Curve[] overlapCrvs;
                        Point3d[] overlapPts;



                        // Get the 3D points of the intersection between the line and the surface of the current model
                        Intersection.CurveBrep(ray_crv, currModel, myDoc.ModelAbsoluteTolerance, out overlapCrvs, out overlapPts);

                        // Store the candidate positions
                        if (overlapPts != null)
                        {
                            if (overlapPts.Count() != 0)
                            {
                                foreach (Point3d p in overlapPts)
                                {
                                    surfacePts.Add(p);
                                }
                            }
                        }
                    }
                }

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

                #region ask the user to select a point


                var getSelectedPts = RhinoGet.GetPoint("Please select points for parameter area", false, out customized_part_location);



                #endregion

                //Kill all dots and duplicate brep, Show the original brep
                myDoc.Objects.Delete(dupObjID, true);
                myDoc.Objects.Show(currModelObjId, true);
                foreach (var ptsID in pts_normals)
                {
                    myDoc.Objects.Delete(ptsID, true);
                }



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
            get { return new Guid("EE617B3A-813E-4935-B1AB-F236095FEE74"); }
        }
    }
}