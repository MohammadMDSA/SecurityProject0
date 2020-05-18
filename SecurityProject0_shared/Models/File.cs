using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Text;

namespace SecurityProject0_shared.Models
{
    public class File : Message
    {
        public string Name { get; private set; }
        public string Path { get; private set; }
        public bool IsSaved => !string.IsNullOrEmpty(Path) && System.IO.File.Exists(Path);

        public File(string name) : base(true)
        {
            this.Name = name;
        }

        public bool Save()
        {
            if (IsSaved)
                return true;
            try
            {
                this.Path = System.IO.Path.GetTempPath() + Name;
                System.IO.File.WriteAllText(this.Path, this.RawMessage, Encoding.Unicode);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
