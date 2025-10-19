using System.Globalization;
using System.Text;
using Dapper;
using Npgsql;

public record DeedDetails(Guid Id, Guid ChildId, Guid ParentId);
public record DeedTypeDetails(Guid Id, Guid ParentId);

public static class Data
{
  public static NpgsqlConnection Conn(string cs) => new NpgsqlConnection(cs);

  public static async Task EnsureSchema(string cs)
  {
    const string sql = @"
create table if not exists parents(
  id uuid primary key,
  email text unique not null,
  created_at timestamptz not null default now()
);

create table if not exists children(
  id uuid primary key,
  parent_id uuid not null references parents(id) on delete cascade,
  name text not null,
  dollar_per_point numeric(10,2) not null default 1.00,
  created_at timestamptz not null default now()
);

create table if not exists deed_types(
  id uuid primary key,
  parent_id uuid not null references parents(id) on delete cascade,
  name text not null,
  points integer not null,
  active boolean not null default true,
  created_at timestamptz not null default now(),
  unique(parent_id, name)
);

create table if not exists deeds(
  id uuid primary key,
  child_id uuid not null references children(id) on delete cascade,
  deed_type_id uuid not null references deed_types(id),
  points integer not null,
  note text,
  occurred_at timestamptz not null default now(),
  created_by uuid not null references parents(id)
);

create table if not exists redemptions(
  id uuid primary key,
  child_id uuid not null references children(id) on delete cascade,
  points integer not null check(points > 0),
  description text,
  created_at timestamptz not null default now(),
  created_by uuid not null references parents(id)
);";
    await using var db = Conn(cs);
    await db.ExecuteAsync(sql);
  }

  public static async Task<ParentDto?> GetParentById(string cs, Guid id)
  {
    const string sql = "select id, email from parents where id = @Id";
    await using var db = Conn(cs);
    return await db.QuerySingleOrDefaultAsync<ParentDto>(sql, new { Id = id });
  }

  public static async Task<ParentDto?> GetParentByEmail(string cs, string email)
  {
    const string sql = "select id, email from parents where email = @Email";
    await using var db = Conn(cs);
    return await db.QuerySingleOrDefaultAsync<ParentDto>(sql, new { Email = email });
  }

  public static async Task<ParentDto> CreateParent(string cs, string email)
  {
    const string sql = @"
insert into parents(id, email)
values(@Id, @Email)
returning id, email;";

    await using var db = Conn(cs);
    var id = Guid.NewGuid();
    return await db.QuerySingleAsync<ParentDto>(sql, new { Id = id, Email = email });
  }

  public static async Task<ChildDto?> GetChildById(string cs, Guid id)
  {
    const string sql = "select id, parent_id, name, dollar_per_point from children where id = @Id";
    await using var db = Conn(cs);
    return await db.QuerySingleOrDefaultAsync<ChildDto>(sql, new { Id = id });
  }

  public static async Task<bool> DeleteChild(string cs, Guid childId)
  {
    const string sql = "delete from children where id = @Id";
    await using var db = Conn(cs);
    var affected = await db.ExecuteAsync(sql, new { Id = childId });
    return affected > 0;
  }

  public static async Task<IEnumerable<ChildDto>> GetChildrenForParent(string cs, Guid parentId)
  {
    const string sql = "select id, parent_id, name, dollar_per_point from children where parent_id = @ParentId order by created_at";
    await using var db = Conn(cs);
    return await db.QueryAsync<ChildDto>(sql, new { ParentId = parentId });
  }

  public static async Task<ChildDto> CreateChild(string cs, Guid parentId, string name, decimal dollarPerPoint)
  {
    const string sql = @"
insert into children(id, parent_id, name, dollar_per_point)
values(@Id, @ParentId, @Name, @DollarPerPoint)
returning id, parent_id, name, dollar_per_point;";

    await using var db = Conn(cs);
    var id = Guid.NewGuid();
    return await db.QuerySingleAsync<ChildDto>(sql, new
    {
      Id = id,
      ParentId = parentId,
      Name = name,
      DollarPerPoint = dollarPerPoint
    });
  }

  public static async Task<ChildDto?> UpdateChild(string cs, Guid id, string name, decimal dollarPerPoint)
  {
    const string sql = @"
update children
set name = @Name,
  dollar_per_point = @DollarPerPoint
where id = @Id
returning id, parent_id, name, dollar_per_point;";

    await using var db = Conn(cs);
    return await db.QuerySingleOrDefaultAsync<ChildDto>(sql, new
    {
      Id = id,
      Name = name,
      DollarPerPoint = dollarPerPoint
    });
  }

  public static async Task<DeedTypeDto?> GetDeedTypeById(string cs, Guid id)
  {
    const string sql = "select id, parent_id, name, points, active from deed_types where id = @Id";
    await using var db = Conn(cs);
    return await db.QuerySingleOrDefaultAsync<DeedTypeDto>(sql, new { Id = id });
  }

  public static async Task<IEnumerable<DeedTypeDto>> GetDeedTypesForParent(string cs, Guid parentId)
  {
    const string sql = "select id, parent_id, name, points, active from deed_types where parent_id = @ParentId order by created_at";
    await using var db = Conn(cs);
    return await db.QueryAsync<DeedTypeDto>(sql, new { ParentId = parentId });
  }

  public static async Task<DeedTypeDto?> GetDeedTypeByName(string cs, Guid parentId, string name)
  {
    const string sql = "select id, parent_id, name, points, active from deed_types where parent_id = @ParentId and lower(name) = lower(@Name)";
    await using var db = Conn(cs);
    return await db.QuerySingleOrDefaultAsync<DeedTypeDto>(sql, new { ParentId = parentId, Name = name });
  }

  public static async Task<DeedTypeDto> CreateDeedType(string cs, Guid parentId, string name, int points)
  {
    const string sql = @"
insert into deed_types(id, parent_id, name, points, active)
values(@Id, @ParentId, @Name, @Points, true)
returning id, parent_id, name, points, active;";

    await using var db = Conn(cs);
    var id = Guid.NewGuid();
    return await db.QuerySingleAsync<DeedTypeDto>(sql, new
    {
      Id = id,
      ParentId = parentId,
      Name = name,
      Points = points
    });
  }

  public static async Task<DeedTypeDto?> UpdateDeedType(string cs, Guid id, string name, int points, bool active)
  {
    const string sql = @"
update deed_types
set name = @Name,
  points = @Points,
  active = @Active
where id = @Id
returning id, parent_id, name, points, active;";

    await using var db = Conn(cs);
    return await db.QuerySingleOrDefaultAsync<DeedTypeDto>(sql, new
    {
      Id = id,
      Name = name,
      Points = points,
      Active = active
    });
  }

  public static async Task<bool> DeleteDeedType(string cs, Guid deedTypeId)
  {
    const string sql = "delete from deed_types where id = @Id";
    await using var db = Conn(cs);
    var affected = await db.ExecuteAsync(sql, new { Id = deedTypeId });
    return affected > 0;
  }

  public static async Task<DeedDto> CreateDeed(string cs, Guid childId, Guid deedTypeId, int points, string? note, Guid createdBy, DateTimeOffset occurredAt)
  {
    const string sql = @"
insert into deeds(id, child_id, deed_type_id, points, note, occurred_at, created_by)
values(@Id, @ChildId, @DeedTypeId, @Points, @Note, @OccurredAt, @CreatedBy)
returning id, child_id, deed_type_id, points, note, occurred_at, created_by;";

    await using var db = Conn(cs);
    var id = Guid.NewGuid();
    return await db.QuerySingleAsync<DeedDto>(sql, new
    {
      Id = id,
      ChildId = childId,
      DeedTypeId = deedTypeId,
      Points = points,
      Note = note,
      OccurredAt = occurredAt,
      CreatedBy = createdBy
    });
  }

  public static async Task<IEnumerable<DeedDto>> GetDeedsForChild(string cs, Guid childId)
  {
    const string sql = @"
select id, child_id, deed_type_id, points, note, occurred_at, created_by
from deeds
where child_id = @ChildId
order by occurred_at desc;";

    await using var db = Conn(cs);
    return await db.QueryAsync<DeedDto>(sql, new { ChildId = childId });
  }

  public static async Task<DeedDto?> GetDeedById(string cs, Guid deedId)
  {
    const string sql = @"
select id, child_id, deed_type_id, points, note, occurred_at, created_by
from deeds where id = @Id";

    await using var db = Conn(cs);
    return await db.QuerySingleOrDefaultAsync<DeedDto>(sql, new { Id = deedId });
  }

  public static async Task<DeedDetails?> GetDeedDetails(string cs, Guid deedId)
  {
    const string sql = @"
select d.id, d.child_id, c.parent_id
from deeds d
join children c on c.id = d.child_id
where d.id = @Id";

    await using var db = Conn(cs);
    return await db.QuerySingleOrDefaultAsync<DeedDetails>(sql, new { Id = deedId });
  }

  public static async Task<bool> DeleteDeed(string cs, Guid deedId)
  {
    const string sql = "delete from deeds where id = @Id";
    await using var db = Conn(cs);
    var affected = await db.ExecuteAsync(sql, new { Id = deedId });
    return affected > 0;
  }

  public static async Task<DeedTypeDetails?> GetDeedTypeDetails(string cs, Guid deedTypeId)
  {
    const string sql = "select id, parent_id from deed_types where id = @Id";
    await using var db = Conn(cs);
    return await db.QuerySingleOrDefaultAsync<DeedTypeDetails>(sql, new { Id = deedTypeId });
  }

  public static async Task<RedemptionDto> CreateRedemption(string cs, Guid childId, int points, string? description, Guid createdBy, DateTimeOffset createdAt)
  {
    const string sql = @"
insert into redemptions(id, child_id, points, description, created_at, created_by)
values(@Id, @ChildId, @Points, @Description, @CreatedAt, @CreatedBy)
returning id, child_id, points, description, created_at, created_by;";

    await using var db = Conn(cs);
    var id = Guid.NewGuid();
    return await db.QuerySingleAsync<RedemptionDto>(sql, new
    {
      Id = id,
      ChildId = childId,
      Points = points,
      Description = description,
      CreatedAt = createdAt,
      CreatedBy = createdBy
    });
  }

  public static async Task<IEnumerable<RedemptionDto>> GetRedemptionsForChild(string cs, Guid childId)
  {
    const string sql = @"
select id, child_id, points, description, created_at, created_by
from redemptions
where child_id = @ChildId
order by created_at desc;";

    await using var db = Conn(cs);
    return await db.QueryAsync<RedemptionDto>(sql, new { ChildId = childId });
  }

  public static async Task<BalanceDto?> GetBalance(string cs, Guid childId)
  {
    const string sql = @"
select
  c.id as ChildId,
  coalesce(sum(d.points), 0) - coalesce(sum(r.points), 0) as Points,
  (coalesce(sum(d.points), 0) - coalesce(sum(r.points), 0)) * c.dollar_per_point as Dollars
from children c
left join deeds d on d.child_id = c.id
left join redemptions r on r.child_id = c.id
where c.id = @ChildId
group by c.id, c.dollar_per_point;";

    await using var db = Conn(cs);
    return await db.QuerySingleOrDefaultAsync<BalanceDto>(sql, new { ChildId = childId });
  }

  public static async Task<IEnumerable<ChildHistoryRow>> GetChildHistory(string cs, Guid childId)
  {
    const string sql = @"
select entry_type as EntryType,
     points as Points,
     dollar_value as DollarValue,
     note as Note,
     occurred_at as OccurredAt,
     recorded_by as RecordedBy
from (
  select 'deed' as entry_type,
       d.points,
       (d.points * c.dollar_per_point) as dollar_value,
       d.note,
       d.occurred_at,
       d.created_by as recorded_by
  from deeds d
  join children c on c.id = d.child_id
  where d.child_id = @ChildId

  union all

  select 'redemption' as entry_type,
       -r.points,
       (-r.points * c.dollar_per_point) as dollar_value,
       r.description,
       r.created_at,
       r.created_by
  from redemptions r
  join children c on c.id = r.child_id
  where r.child_id = @ChildId
) history
order by occurred_at;";

    await using var db = Conn(cs);
    return await db.QueryAsync<ChildHistoryRow>(sql, new { ChildId = childId });
  }

  public static string ToCsv(IEnumerable<ChildHistoryRow> rows)
  {
    var sb = new StringBuilder();
    sb.AppendLine("entry_type,points,dollar_value,note,occurred_at,recorded_by");

    foreach (var row in rows)
    {
      var line = string.Join(',', new[]
      {
        EscapeCsv(row.EntryType),
        row.Points.ToString(CultureInfo.InvariantCulture),
        row.DollarValue.ToString(CultureInfo.InvariantCulture),
        EscapeCsv(row.Note),
        row.OccurredAt.ToString("O", CultureInfo.InvariantCulture),
        row.RecordedBy.ToString()
      });
      sb.AppendLine(line);
    }

    return sb.ToString();
  }

  private static string EscapeCsv(string? value)
  {
    if (string.IsNullOrEmpty(value))
    {
      return string.Empty;
    }

    var sanitized = value.Replace("\"", "\"\"");
    return (sanitized.Contains(',') || sanitized.Contains('\n') || sanitized.Contains('\r'))
      ? $"\"{sanitized}\""
      : sanitized;
  }
}
