namespace tyss.Services;

public class FolderService(AppDbContext db)
{
    public async Task UpdateAsync(int id, string name, int? parentId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            await ValidateNoCyclicReference(id, parentId);

            var folder = await db.Folders.FindAsync(id) ?? throw new Exception("Folder not found");
            folder.Name = name;
            folder.ParentId = parentId == 0 ? null : parentId;

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task ValidateNoCyclicReference(int folderId, int? parentId)
    {
        if (folderId == parentId && parentId != 0)
            throw new Exception("Self-referential cycle detected.");

        if (!parentId.HasValue) return;

        var current = await db.Folders.FindAsync(parentId);
        while (current != null)
        {
            if (current.Id == folderId)
                throw new Exception("Cyclic dependency detected.");

            db.Entry(current).Reference(f => f.Parent).Load();
            current = current.Parent;
        }
    }
}