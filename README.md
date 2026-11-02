# Linkloom

Linkloom is a single-project .NET 10 Blazor Web App for keeping a focused personal library of training resources. It uses interactive server rendering, a vertical-slice-friendly feature layout, and JSON files instead of a database.

## Run locally

```powershell
dotnet run
```

The app creates `App_Data/training.json` and `App_Data/categories.json` on first run with a small set of sample data. Links and categories are intentionally stored separately outside `wwwroot`; both files are updated through an in-process lock and atomic temporary-file replacement so a write cannot leave a partial JSON document behind. Existing combined `training.json` files are migrated automatically the next time the app starts.

## Included

- Web page and YouTube training links with title, URL, notes, category, subcategory, and priority.
- Seen/unseen and completed/incomplete states, search, and filters.
- Category and subcategory management with validation.
- A dedicated Categories screen for adding, editing, and removing categories. Categories with assigned links are protected from accidental deletion until those links are moved or removed.
- Responsive material-inspired UI with clear empty, success, and error states.
