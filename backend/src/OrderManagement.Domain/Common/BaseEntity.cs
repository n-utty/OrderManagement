namespace OrderManagement.Domain.Common;

public abstract class BaseEntity
{
    // Version 7 GUIDs are time-ordered, so inserts stay sequential in the primary key index.
    public Guid Id { get; set; } = Guid.CreateVersion7();

    // Both timestamps are UTC and are stamped by the persistence layer on save,
    // so every write path records them the same way.
    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
