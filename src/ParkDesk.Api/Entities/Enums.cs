namespace ParkDesk.Api.Entities;

public enum UserRole
{
    Admin,
    Operator,
    Customer
}

public enum SpotType
{
    Regular,
    Compact,
    Electric,
    Disabled
}

public enum SpotStatus
{
    Available,
    Occupied,
    OutOfService
}

public enum VehicleType
{
    Car,
    Motorcycle,
    Electric
}

public enum SessionStatus
{
    Active,
    Completed
}

public enum ReservationStatus
{
    Pending,
    CheckedIn,
    Completed,
    Cancelled,
    Expired
}
