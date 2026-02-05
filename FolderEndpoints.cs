using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using tyss.Services;

namespace tyss
{
    public static class FolderEndpoints
    {
        public static IEndpointRouteBuilder MapFoldersEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/folders").RequireAuthorization();

            group.MapGet("/", GetAllFolders);
            group.MapGet("/{id}", GetFolderById);
            group.MapPost("/", CreateFolder);
            group.MapPut("/{id}", UpdateFolder).RequireAuthorization("Admin");
            group.MapDelete("/{id}", DeleteFolder).RequireAuthorization("Admin");

            app.MapGet("/api/export", ExportFolders);
#if DEBUG
            app.MapGet("/", () => Results.Redirect("./swagger/index.html"))
                .ExcludeFromDescription(); 
#endif

            return app;
        }

        private static async Task<IResult> GetAllFolders(AppDbContext db)
        {
            var folders = await db.Folders
                .Select(f => new { f.Id, f.Name, f.ParentId })
                .ToListAsync();
            return Results.Ok(folders);
        }

        private static async Task<IResult> GetFolderById(int id, AppDbContext db)
        {
            var folder = await db.Folders
                .Where(f => f.Id == id)
                .Select(f => new { f.Id, f.Name, f.ParentId })
                .FirstOrDefaultAsync();
            return folder == null ? Results.NotFound() : Results.Ok(folder);
        }

        private static async Task<IResult> CreateFolder(Folder folder, AppDbContext db)
        {
            if (folder.ParentId == 0)
                folder.ParentId = null;

            db.Folders.Add(folder);
            await db.SaveChangesAsync();

            return Results.Created($"/api/folders/{folder.Id}", folder);
        }

        private static async Task<IResult> UpdateFolder(int id, Folder input, FolderService service)
        {
            await service.UpdateAsync(id, input.Name, input.ParentId);
            return Results.NoContent();
        }

        private static async Task<IResult> DeleteFolder(int id, AppDbContext db)
        {
            var folder = await db.Folders.FindAsync(id);
            if (folder == null)
                return Results.NotFound();

            db.Folders.Remove(folder);
            await db.SaveChangesAsync();

            return Results.NoContent();
        }

        private static async Task<IResult> ExportFolders(AppDbContext db)
        {
            await db.Database.EnsureCreatedAsync();

            var folders = db.Folders.Include(f => f.Children);
            var roots = folders.Where(f => f.ParentId == null);

            var toExport =  await roots.ToListAsync();

            Folder root = new Folder
            {
                Children = toExport, 
                Id = 0, 
                Name = "Root", 
                ParentId = null, 
                Parent = null 
            };
;
            var exportedText = JsonConvert.SerializeObject(root,
                Formatting.Indented,
            new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new FilteredPropertiesResolver("ParentId")
            });

            var res = Results.Text(exportedText);

            return res;
        }

        public class FilteredPropertiesResolver : DefaultContractResolver
        {
            private readonly string _propertyName;
            public FilteredPropertiesResolver(string propertyName) => _propertyName = propertyName;

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                return base.CreateProperties(type, memberSerialization)
                    .Where(p => p.PropertyName != _propertyName)
                    .ToList();
            }
        }




    }
}
