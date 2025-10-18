public record ParentDto(Guid Id, string Email);
public record CreateParent(string Email);

public record ChildDto(Guid Id, Guid ParentId, string Name, decimal DollarPerPoint);
public record CreateChild(Guid ParentId, string Name, decimal? DollarPerPoint);
public record UpdateChild(Guid ParentId, string Name, decimal? DollarPerPoint);

public record DeedTypeDto(Guid Id, Guid ParentId, string Name, int Points, bool Active);
public record CreateDeedType(Guid ParentId, string Name, int Points);
public record UpdateDeedType(Guid ParentId, string Name, int Points, bool Active);

public record CreateDeed(Guid ChildId, Guid DeedTypeId, int Points, string? Note, Guid CreatedBy);
public record DeedDto(Guid Id, Guid ChildId, Guid DeedTypeId, int Points, string? Note, DateTimeOffset OccurredAt, Guid CreatedBy);
public record BalanceDto(Guid ChildId, int Points, decimal Dollars);
public record CreateRedemption(Guid ChildId, int Points, string? Description, Guid CreatedBy);
public record RedemptionDto(Guid Id, Guid ChildId, int Points, string? Description, DateTimeOffset CreatedAt, Guid CreatedBy);
public record ChildHistoryRow(string EntryType, int Points, decimal DollarValue, string? Note, DateTimeOffset OccurredAt, Guid RecordedBy);
