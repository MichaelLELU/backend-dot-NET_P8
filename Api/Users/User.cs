using GpsUtil.Location;
using System.Collections.Concurrent;
using TripPricer;

namespace TourGuide.Users;

public class User
{
    public Guid UserId { get; }
    public string UserName { get; }
    public string PhoneNumber { get; set; }
    public string EmailAddress { get; set; }
    public DateTime LatestLocationTimestamp { get; set; }
    public ConcurrentStack<VisitedLocation> VisitedLocations { get; } = new();
    public List<UserReward> UserRewards { get; } = new List<UserReward>();
    public object UserRewardsLock { get; } = new();
    public UserPreferences UserPreferences { get; set; } = new UserPreferences();
    public List<Provider> TripDeals { get; set; } = new List<Provider>();

    public User(Guid userId, string userName, string phoneNumber, string emailAddress)
    {
        UserId = userId;
        UserName = userName;
        PhoneNumber = phoneNumber;
        EmailAddress = emailAddress;
    }

    public void AddToVisitedLocations(VisitedLocation visitedLocation)
    {
        VisitedLocations.Push(visitedLocation);
    }

    public void ClearVisitedLocations()
    {
        VisitedLocations.Clear();
    }

    public void AddUserReward(UserReward userReward)
    {
        if (!UserRewards.Exists(r => r.Attraction.AttractionName == userReward.Attraction.AttractionName))
        {
            UserRewards.Add(userReward);
        }
    }

    public VisitedLocation GetLastVisitedLocation()
    {
        return VisitedLocations.First();
    }
}
