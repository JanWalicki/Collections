using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollectionManagement.Models
{
    public class Item
    {
        public int Id { get; set; }
        public int CollectionId { get; set; }
        public string Title { get; set; } 
        public decimal Price { get; set; } 
        public ItemStatus Status { get; set; }
        public string StringStatus 
        {
            get
            {
                switch (Status) 
                { 
                    case ItemStatus.New:
                        return "Nowy";
                    case ItemStatus.Used:
                        return "Używany";
                    case ItemStatus.ForSale:
                        return "Na sprzedaż";
                    case ItemStatus.WantToBuy:
                        return "Chcę kupić";
                    case ItemStatus.Sold:
                        return "Sprzedane";
                    default:
                        return "Unknown";
                }
            }
        }
        public int Rating { get; set; } 
        public string Comment { get; set; }
        public string ImagePath { get; set; } 
        public Dictionary<string, CustomField> CustomFields { get; set; } = new();

        public bool IsSold => Status == ItemStatus.Sold;

        public Item(string title, decimal price, ItemStatus status, int rating, string comment, string imagePath)
        {
            Title = title;
            Price = price;
            Status = status;
            Rating = rating;
            Comment = comment;
            ImagePath = imagePath;
        }
    }
}
