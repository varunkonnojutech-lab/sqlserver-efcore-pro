# Database scripts

The application schema is owned by EF Core migrations under
`src/SqlServerEfCorePro.Data/Migrations`. These scripts are the DBA/learning
view of the same database:

| Script | Purpose |
|---|---|
| `01-create-schema.sql` | Tables, keys, and the core indexes in readable T-SQL |
| `02-stored-procedures.sql` | The two procedures called by EF Core |
| `03-query-tuning-lab.sql` | Before/after queries, covering-index experiment, and execution-plan reading guide |

Use migrations for a real deployment. Use the scripts in SSMS or Azure Data
Studio when you want to inspect exactly what SQL Server is being asked to do.
