using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace MyUI
{
    public class SharedData
    {
        public Voxel[,,] voxelSpace { get; set; }


        public SharedData() => voxelSpace = null;

    }


    public class SharedDataComponent : GH_Component
    {
        public SharedData sharedData;


        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public SharedDataComponent()
          : base("SharedData", "Data",
              "This component stores data that should be shared among parameters",
              "MyUI", "Main")
        {
             sharedData = new SharedData();
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("New Voxel Space", "voxels", "The new voxel space of the current model", GH_ParamAccess.item);
            pManager[0].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Old Voxel Space", "voxels", "The old voxel space of the current model", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Voxel[,,] new_voxelSpace= null;

            if (!DA.GetData(0, ref new_voxelSpace))
                return;

            sharedData.voxelSpace = new_voxelSpace;

            DA.SetData(0, sharedData);
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
            get { return new Guid("57542D7E-19DB-4E8D-827B-FB1657E91A9E"); }
        }
    }
}