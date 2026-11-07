using System;
using System.Collections.Generic;

namespace MebelOrg.Models;

public partial class Order
{
    public int Id { get; set; }

    public int Number { get; set; }

    public int UserId { get; set; }

    public int PickupPointId { get; set; }

    public int StatusId { get; set; }

    public DateOnly OrderDate { get; set; }

    public DateOnly? DeliveryDate { get; set; }

    public int Code { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual PickupPoint PickupPoint { get; set; }

    public virtual Status Status { get; set; }

    public virtual User User { get; set; }
}
