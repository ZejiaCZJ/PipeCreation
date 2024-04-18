using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUI
{
    public class Param
    {
        private static Param instance = null;

        //Variables that must have
        private string param_name;
        private string input_param;
        private string input_type;
        private string output_param;

        //Placeholder for area selection
        private double x;
        private double y;
        private double z;
        private double radius;

        //--Placeholder for all possible output options--

        //Speaker
        private string sound_type;

        //LED Light
        private int brightness;
        private string light_mode;

        //Rotational & Translational Motion
        private string rotate_mode;
        private string translational_mode;
        private double degree;
        private double speed;
        private double direction;
        private double repeatability;
        private double start_point;
        private double end_point;
        private bool is_recycling;

        //Vibration
        private string vibrate_mode;
        private string pattern;
        private string time;
        private string strength;

        //Constructors
        public Param() 
        {
            this.param_name = "";
            this.input_param = "";
            this.input_type = "";
            this.output_param = "";
        }

        public Param(string param_name, string input_param, string input_type, string output_param)
        {
            if (param_name == "Create New Parameter")
            {
                this.param_name = input_param + " " + input_type + " " + output_param;
                this.input_param = input_param;
                this.input_type = input_type;
                this.output_param = output_param;
            }
            else
            {
                this.param_name = param_name;
                this.input_param = input_param;
                this.input_type = input_type;
                this.output_param = output_param;
            }
        }

        public Param(Param original)
        {
            //Variables that must have
            this.param_name = original.Param_name;
            this.input_param = original.Input_param;
            this.input_type = original.Input_type;
            this.output_param = original.Output_param;

            //Placeholder for area selection
            this.x = original.X;
            this.y = original.Y;
            this.z = original.Z;
            this.radius = original.Radius;

            //--Placeholder for all possible output options--

            //Speaker
            this.sound_type = original.Sound_type;

            //LED Light
            this.brightness = original.Brightness;
            this.light_mode = original.Light_mode;

            //Rotational & Translational Motion
            this.rotate_mode = original.Rotate_mode;
            this.translational_mode = original.Translational_mode;
            this.degree = original.Degree;
            this.speed = original.Speed;
            this.direction = original.Direction;
            this.repeatability = original.Repeatability ;
            this.start_point = original.Start_point;
            this.end_point = original.End_point;
            this.is_recycling = original.Is_recycling;

            //Vibration
            this.vibrate_mode = original.Vibrate_mode;
            this.pattern = original.Pattern;
            this.time = original.Time;
            this.strength = original.Strength;
    }

        public static Param Instance
        {
            get
            {
                if(instance == null)
                {
                    instance = new Param();
                }
                return instance;
            }
        }

        //Getters & Setters
        public string Param_name
        {
            get { return this.param_name; }
            set { this.param_name = value; }
        }

        public string Input_param
        {
            get { return this.input_param; }
            set { this.input_param = value;}
        }

        public string Input_type
        {   get { return this.input_type;}
            set { this.input_type = value;}
        }

        public string Output_param
        { get { return this.output_param;}
          set { this.output_param = value;}
        }

        public double X { get { return this.x; } set { this.x = value;} }

        public double Y { get { return this.y;} set { this.y = value;} }

        public double Z { get { return this.z; } set { this.z = value; } }  

        public double Radius { get { return this.radius; } set { this.radius = value;} }

        public string Sound_type { get { return this.sound_type;} set { this.sound_type = value;} }

        public int Brightness { get { return this.brightness; } set { this.brightness = value; } }

        public string Light_mode { get { return this.light_mode;} set { this.light_mode = value;} }

        public string Rotate_mode { get { return this.rotate_mode;} set { this.rotate_mode = value;} }

        public string Translational_mode {  get { return this.translational_mode;} set { this.translational_mode = value;} }

        public double Degree { get { return this.degree; } set { this.degree = value;} }

        public double Speed { get { return this.speed; } set { this.speed = value; } }

        public double Direction { get { return this.direction; } set { this.direction = value; } }

        public double Repeatability { get { return this.repeatability;} set {  this.repeatability = value;} }

        public double Start_point { get { return this.start_point;} set { this.start_point = value; } }

        public double End_point { get { return this.end_point;} set { this.end_point = value; } }

        public bool Is_recycling { get {  return this.is_recycling; } set { this.is_recycling = value;} }

        public string Vibrate_mode { get { return this.vibrate_mode; } set { this.vibrate_mode = value; } }

        public string Pattern {  get { return this.pattern; } set { this.pattern = value; } }

        public string Time {  get { return this.time; } set {  this.time = value; } }

        public string Strength { get { return this.strength; } set { this.strength = value; } }

    }

    public class Param_Goo : GH_Goo<Param>
    {
        public Param_Goo()
        {
            
        }

        public override bool IsValid => throw new NotImplementedException();

        public override string TypeName => throw new NotImplementedException();

        public override string TypeDescription => throw new NotImplementedException();

        public override IGH_Goo Duplicate()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }
    }

    public class Parameters : GH_Param<Param_Goo>
    {
        public Parameters(IGH_InstanceDescription tag) : base(tag)
        {
        }

        public Parameters(IGH_InstanceDescription tag, GH_ParamAccess access) : base(tag, access)
        {
        }

        public Parameters(string name, string nickname, string description, string category, string subcategory, GH_ParamAccess access) : base(name, nickname, description, category, subcategory, access)
        {
        }

        public override Guid ComponentGuid => throw new NotImplementedException();
    }
}



