using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Text;

namespace SecurityProject0_shared.Models
{
    public class File : Message
    {
        public string Name { get; private set; }
        public string Path { get; set; }
        public bool IsSaved => !string.IsNullOrEmpty(Path) && System.IO.File.Exists(Path);

        public File(string name) : base(true)
        {
            this.Name = name;
        }

    }
}
