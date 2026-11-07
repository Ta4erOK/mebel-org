using System;
using System.Collections.Generic;

namespace MebelOrg.Models;

public partial class Unit
{
    public int Id { get; set; }

    public string Name { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
