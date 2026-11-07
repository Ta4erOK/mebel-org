using System;
using System.Collections.Generic;

namespace MebelOrg.Models;

public partial class Product
{
    public int Id { get; set; }

    public string Article { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public int CategoryId { get; set; }

    public int ManufacturerId { get; set; }

    public int SupplierId { get; set; }

    public int UnitId { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public decimal Discount { get; set; }

    public string ImagePath { get; set; }

    public virtual Category Category { get; set; }

    public virtual Manufacturer Manufacturer { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Supplier Supplier { get; set; }

    public virtual Unit Unit { get; set; }
}
