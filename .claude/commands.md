# Commandes

```bash
# Backend
dotnet test                              # tous les tests
dotnet test --filter "Category=Unit"     # tests unitaires seuls
dotnet stryker                           # mutation testing (depuis tests/)
dotnet ef migrations add <Nom>           # nouvelle migration EF Core
dotnet run --project src/Reservation.Api # lancer l'API

# Frontend
ng serve                                 # dev server (http://localhost:4200)
ng test --watch=false                    # tests Angular
ng build --configuration=production      # build prod
```
