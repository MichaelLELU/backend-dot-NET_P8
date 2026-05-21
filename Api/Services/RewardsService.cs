using GpsUtil;
using GpsUtil.Location;
using System.Collections.Concurrent;
using System.Diagnostics;
using TourGuide.LibrairiesWrappers.Interfaces;
using TourGuide.Services.Interfaces;
using TourGuide.Users;

namespace TourGuide.Services;

public class RewardsService : IRewardsService
{
    private const double StatuteMilesPerNauticalMile = 1.15077945;
    private readonly int _defaultProximityBuffer = 10;
    private int _proximityBuffer;
    private readonly int _attractionProximityRange = 200;
    private readonly IGpsUtil _gpsUtil;
    private readonly IRewardCentral _rewardsCentral;

    public RewardsService(IGpsUtil gpsUtil, IRewardCentral rewardCentral)
    {
        _gpsUtil = gpsUtil;
        _rewardsCentral =rewardCentral;
        _proximityBuffer = _defaultProximityBuffer;
    }

    public void SetProximityBuffer(int proximityBuffer)
    {
        _proximityBuffer = proximityBuffer;
    }

    public void SetDefaultProximityBuffer()
    {
        _proximityBuffer = _defaultProximityBuffer;
    }



    public void CalculateRewards(User user)
    {
        var attractions = _gpsUtil.GetAttractions();
        var rewardedAttractions = new ConcurrentDictionary<string, byte>();

        List<UserReward> existingRewards;

        lock (user.UserRewardsLock)
        {
            existingRewards = user.UserRewards.ToList();
        }

        foreach (var reward in existingRewards)
        {
            rewardedAttractions.TryAdd(
                reward.Attraction.AttractionName,
                0);
        }

        var visitedLocations = user.VisitedLocations.ToList();

        foreach (var visitedLocation in visitedLocations)
        {
            Parallel.ForEach(
                attractions,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                },
                attraction =>
                {
                if (!NearAttraction(visitedLocation, attraction))
                {
                    return;
                }

                if (!rewardedAttractions.TryAdd(
                        attraction.AttractionName,
                        0))
                {
                    return;
                }

                //Stopwatch swRewardPoints = Stopwatch.StartNew();

                var rewardPoints = GetRewardPoints(attraction, user);

                //Console.WriteLine(
                //    $"RewardCentral: {swRewardPoints.ElapsedMilliseconds} ms");

                var reward = new UserReward(
                    visitedLocation,
                    attraction,
                    rewardPoints
                );

                    lock (user.UserRewardsLock)
                    {
                    user.AddUserReward(reward);
                }
            });
        }
    }

    public async Task CalculateRewardsAsync(User user)
    {
        await Task.Run(() =>
        {
            CalculateRewards(user);
        });
    }


    public bool IsWithinAttractionProximity(Attraction attraction, Locations location)
    {
        //Console.WriteLine(GetDistance(attraction, location));
        return GetDistance(attraction, location) <= _attractionProximityRange;
    }

    private bool NearAttraction(VisitedLocation visitedLocation, Attraction attraction)
    {
        return GetDistance(attraction, visitedLocation.Location) <= _proximityBuffer;
    }

    public int GetRewardPoints(Attraction attraction, User user)
    {
        return _rewardsCentral.GetAttractionRewardPoints(attraction.AttractionId, user.UserId);
    }

    public double GetDistance(Locations loc1, Locations loc2)
    {
        double lat1 = Math.PI * loc1.Latitude / 180.0;
        double lon1 = Math.PI * loc1.Longitude / 180.0;
        double lat2 = Math.PI * loc2.Latitude / 180.0;
        double lon2 = Math.PI * loc2.Longitude / 180.0;

        double angle = Math.Acos(Math.Sin(lat1) * Math.Sin(lat2)
                                + Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lon1 - lon2));

        double nauticalMiles = 60.0 * angle * 180.0 / Math.PI;
        return StatuteMilesPerNauticalMile * nauticalMiles;
    }
}
