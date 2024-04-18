using System;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;

namespace MyUI
{
    public class MyUIInfo : GH_AssemblyInfo
    {
        public override string Name => "MyUI";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("ddeeab5b-f1e5-4360-95cf-74ff93437833");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";
    }
}