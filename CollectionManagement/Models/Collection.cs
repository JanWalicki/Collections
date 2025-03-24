using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollectionManagement.Models
{
    public partial class Collection: ObservableObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        
        [ObservableProperty]
        public ObservableCollection<Item> items = new();

        public Collection(string name)
        {
            Name = name;
        }
    }
}
