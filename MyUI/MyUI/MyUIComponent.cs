using System;
using System.Collections.Generic;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;

namespace MyUI
{
    public class MyUIComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public MyUIComponent()
          : base("List of Parameter", "Params_list",
            "The list container that stores all user-created parameter",
            "MyUI", "Main")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddScriptVariableParameter("param_name","name","The name of the parameter", GH_ParamAccess.item);
            pManager.AddScriptVariableParameter("input_param","input","The input parameter", GH_ParamAccess.item);
            pManager.AddScriptVariableParameter("input_type","type","The type of the input parameter", GH_ParamAccess.item);
            pManager.AddScriptVariableParameter("output_param","output","The output parameter", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Selected Parameter", "param", "The user-selected parameter", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string param_name = "";
            string input_param = "";
            string input_type = "";
            string output_param = "";
            Param param = new Param();


            List<Param> param_list = new List<Param>();

            if (!DA.GetData(0, ref param_name))
                return;
            if (!DA.GetData(1, ref input_param))
                return;
            if (!DA.GetData(2, ref input_type))
                return;
            if (!DA.GetData(3, ref output_param))
                return;

            if (param_name == "Create New Parameter")
            {
                param = new Param(param_name, input_param, input_type, output_param);
                param_list.Add(param);
                DA.SetData(0, param);
            }
            else
            {
                foreach (var temp_param in param_list)
                {
                    if(temp_param.Param_name == param_name)
                        param = temp_param;
                }
                DA.SetData(0, param);
            }

        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => null;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("bef446d0-618e-424b-9ec4-267d6fbca2eb");
    }
}