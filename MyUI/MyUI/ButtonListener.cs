using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;

namespace MyUI
{
    public class ButtonListener : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public ButtonListener()
          : base("Button Listener", "B_Listener",
              "This listener listens to the button, and open and close mutual exclusive windows",
              "MyUI", "Main")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("parent window button", "p_win", "The button of parent window of the set", GH_ParamAccess.item);
            pManager.AddBooleanParameter("child window button", "c_win", "The button of child window of the set", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("new value of parent window button", "p_win", "New value for the button of parent window of the set", GH_ParamAccess.item);
            pManager.AddBooleanParameter("new value of child window button", "c_win", "New value for the button of child window of the set", GH_ParamAccess.item);
            pManager.AddBooleanParameter("new value for parent window", "val_p_win", "New value for the parent window of the set", GH_ParamAccess.item);
            pManager.AddBooleanParameter("new value for child window", "val_c_win", "New value for the child window of the set", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Boolean parent_window_button = false; //True when user clicks 'modify' or something similar that need to navigate to the child window
            Boolean child_window_button = false; //True when user clicks 'done' or something similar that need to navigate back to the parent window
            Boolean new_parent_window_button = false;
            Boolean new_child_window_button = false;
            Boolean new_parent_window = false;
            Boolean new_child_window = false;


            if (!DA.GetData(0, ref parent_window_button))
                return;
            if (!DA.GetData(1, ref child_window_button))
                return;
           
            if(child_window_button == true)
            {
                new_parent_window = true;
                new_child_window = false;
                new_parent_window_button = false;
                new_child_window_button = false;
            }
            else if(parent_window_button == true)
            {
                new_parent_window = false;
                new_child_window = true;
                new_parent_window_button = false;
                new_child_window_button = false;
            }
            else
            {
                new_parent_window = true;
                new_child_window = false;
                new_parent_window_button = false;
                new_child_window_button = false;
            }



            DA.SetData(0, new_parent_window_button);
            DA.SetData(1, new_child_window_button);
            DA.SetData(2, new_parent_window);
            DA.SetData(3, new_child_window);

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
            get { return new Guid("22FC3A8C-CFD8-4ECE-9D61-86A403484DAF"); }
        }
    }
}