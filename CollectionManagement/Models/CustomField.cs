using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollectionManagement.Models
{
    public class CustomField
    {
        public int Id { get; set; }
        public string Name { get; set; } 
        public FieldType Type { get; set; } 
        public object Value { get; set; } 

        public CustomField(string name, FieldType type, object value)
        {
            Name = name;
            Type = type;
            Value = value;
        }

        public override string ToString()
        {
            return Value?.ToString() ?? string.Empty;
        }
    }
}
