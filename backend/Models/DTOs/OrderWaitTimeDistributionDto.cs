namespace InnriGreifi.API.Models.DTOs;

/// <summary>
/// Distribution of wait times as histogram buckets.
/// </summary>
public class OrderWaitTimeDistributionDto
{
    public int BucketSize { get; set; } = 5; // minutes
    public List<OrderWaitTimeBucketDto> Buckets { get; set; } = new();
}

public class OrderWaitTimeBucketDto
{
    /// <summary>
    /// Lower bound of the bucket (inclusive), in minutes.
    /// </summary>
    public int MinMinutes { get; set; }
    
    /// <summary>
    /// Upper bound of the bucket (exclusive), in minutes. Null for the last bucket (unbounded).
    /// </summary>
    public int? MaxMinutes { get; set; }
    
    public int Count { get; set; }
    
    public string Label => MaxMinutes.HasValue 
        ? $"{MinMinutes}-{MaxMinutes}" 
        : $"{MinMinutes}+";
}

