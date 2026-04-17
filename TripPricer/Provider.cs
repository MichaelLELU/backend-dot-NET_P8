using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TripPricer;

public class Provider
{
    public string Name { get; }
    public double Price { get; }
    public Guid TripId { get; }

    public Provider(Guid tripId, string name, double price)
    {
        Name = name;
        TripId = tripId;
        Price = price;
    }
}
